using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluid;
using Fluid.Values;
using Markdig;
using Microsoft.Extensions.FileProviders;
using Serilog;
using SuCoS.Models;
using SuCoS.Parser;

namespace SuCoS;

/// <summary>
/// Base class for build and serve commands.
/// </summary>
public abstract class BaseGeneratorCommand
{
    /// <summary>
    /// Command line options
    /// </summary>
    readonly IGenerateOptions options;

    /// <summary>
    /// The configuration file name.
    /// </summary>
    protected const string configFile = "sucos.yaml";

    /// <summary>
    /// The Fluid parser instance.
    /// </summary>
    protected static readonly FluidParser fluidParser = new();

    /// <summary>
    /// The site configuration.
    /// </summary>
    protected Site site;

    /// <summary>
    /// The frontmatter parser instance. The default is YAML.
    /// </summary>
    protected readonly IFrontmatterParser frontmatterParser = new YAMLParser();

    /// <summary>
    /// Cache for content templates.
    /// </summary>
    protected readonly Dictionary<(string, Kind, string), string> contentTemplateCache = new();

    /// <summary>
    /// Cache for base templates.
    /// </summary>
    protected readonly Dictionary<(string, Kind, string), string> baseTemplateCache = new();

    /// <summary>
    /// Cache for tag frontmatter.
    /// </summary>
    protected readonly Dictionary<string, Frontmatter> automaticContentCache = new();

    /// <summary>
    /// The synchronization lock object.
    /// </summary>
    protected readonly object syncLock = new();

    /// <summary>
    /// The synchronization lock object during ProstProcess.
    /// </summary>
    protected readonly object syncLockPostProcess = new();

    /// <summary>
    /// The Fluid/Liquid template options.
    /// </summary>
    protected readonly TemplateOptions templateOptions = new();

    /// <summary>
    /// The stopwatch reporter.
    /// </summary>
    protected readonly StopwatchReporter stopwatch = new();

    /// <summary>
    /// Markdig 20+ built-in extensions
    /// </summary>
    /// https://github.com/xoofx/markdig
    protected MarkdownPipeline markdownPipeline;

    /// <summary>
    /// The time that the older cache should be ignored.
    /// </summary>
    public DateTime IgnoreCacheBefore { get; set; }

    /// <summary>
    /// Create a page not from the content folder, but as part of the process.
    /// It's used to create tag pages, section list pages, etc.
    /// </summary>
    public Frontmatter CreateAutomaticFrontmatter(BasicContent baseContent, Frontmatter originalFrontmatter)
    {
        if (baseContent is null)
        {
            throw new ArgumentNullException(nameof(baseContent));
        }
        if (originalFrontmatter is null)
        {
            throw new ArgumentNullException(nameof(originalFrontmatter));
        }

        var id = baseContent.URL ?? baseContent.Section;
        Frontmatter? frontmatter = null;
        lock (syncLock)
        {
            if (!automaticContentCache.TryGetValue(id, out frontmatter))
            {
                frontmatter = new(
                    baseGeneratorCommand: this,
                    site: site,
                    title: baseContent.Title,
                    sourcePath: string.Empty,
                    sourceFileNameWithoutExtension: string.Empty,
                    sourcePathDirectory: null
                )
                {
                    Section = baseContent.Section,
                    Kind = baseContent.Kind,
                    Type = baseContent.Type,
                    URL = baseContent.URL,
                    Pages = new()
                };

                automaticContentCache.Add(id, frontmatter);
                PostProcessFrontMatter(frontmatter);
            }
        }

        if (frontmatter.Kind != Kind.index)
        {
            frontmatter.Pages!.Add(originalFrontmatter);
        }

        // TODO: still too hardcoded
        if (frontmatter.Type == "tags" && originalFrontmatter is not null)
        {
            lock (originalFrontmatter!)
            {
                if (frontmatter.Type == "tags")
                {
                    originalFrontmatter.Tags ??= new();
                    originalFrontmatter.Tags!.Add(frontmatter);
                }
            }
        }

        return frontmatter;
    }

    /// <summary>
    /// Create the page content, with converted Markdown and themed.
    /// </summary>
    /// <param name="frontmatter">the page</param>
    /// <returns></returns>
    public string CreateContent(Frontmatter frontmatter)
    {
        var fileContents = GetTemplate(site.SourceThemePath, frontmatter);
        // Theme content
        if (string.IsNullOrEmpty(value: fileContents))
        {
            return frontmatter.Content;
        }
        else if (fluidParser.TryParse(fileContents, out var template, out var error))
        {
            var context = new TemplateContext(templateOptions)
                .SetValue("page", frontmatter);
            try
            {
                var rendered = template.Render(context);
                return rendered;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error rendering theme template: {Error}", error);
                return string.Empty;
            }
        }
        else
        {
            Log.Error("Error parsing theme template: {Error}", error);
            return string.Empty;
        }
    }

    /// <summary>
    /// Create the page content, with converted Markdown and but no theme.
    /// </summary>
    /// <param name="frontmatter">the page</param>
    /// <returns></returns>
    public string CreateContentPreRendered(Frontmatter frontmatter)
    {
        if (frontmatter is null)
        {
            throw new ArgumentNullException(nameof(frontmatter));
        }

        return Markdown.ToHtml(frontmatter!.RawContent, markdownPipeline);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseGeneratorCommand"/> class.
    /// </summary>
    /// <param name="options">The generate options.</param>
    protected BaseGeneratorCommand(IGenerateOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }
        this.options = options;

        Log.Information("Source path: {source}", propertyValue: options.Source);

        try
        {
            site = ParseSiteSettings(options: options, frontmatterParser);
            if (site is null)
            {
                throw new FormatException("Error reading app config");
            }
        }
        catch
        {
            throw new FormatException("Error reading app config");
        }

        templateOptions.MemberAccessStrategy.Register<Frontmatter>();
        templateOptions.MemberAccessStrategy.Register<Site>();
        templateOptions.MemberAccessStrategy.Register<BaseGeneratorCommand>();
        templateOptions.FileProvider = new PhysicalFileProvider(Path.GetFullPath(site.SourceThemePath));
        templateOptions.Filters.AddFilter("whereParams", WhereParamsFilter);

        // Configure Markdig with the Bibliography extension
        markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        ScanAllMarkdownFiles();

        ParseSourceFiles();
    }

    /// <summary>
    /// Scans all markdown files in the source directory.
    /// </summary>
    protected void ScanAllMarkdownFiles()
    {
        // Scan content files
        var markdownFiles = GetAllMarkdownFiles(site.SourceContentPath);

        foreach (var fileAbsolutePath in markdownFiles)
        {
            var content = File.ReadAllText(fileAbsolutePath);
            var relativePath = Path.GetRelativePath(site.SourceContentPath, fileAbsolutePath);
            site.RawPages.Add((relativePath, content));
        }
    }

    /// <summary>
    /// Parses the source files and extracts the frontmatter.
    /// </summary>
    protected void ParseSourceFiles()
    {
        stopwatch.Start("Parse");

        // Process the source files, extracting the frontmatter
        var filesParsed = 0; // counter to keep track of the number of files processed
        _ = Parallel.ForEach(site.RawPages, file =>
        {
            try
            {
                var frontmatter = ReadSourceFrontmatter(file.filePath, file.content, site, frontmatterParser);

                if (IsValidDate(frontmatter))
                {
                    PostProcessFrontMatter(frontmatter, true);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error parsing file {file}", file.filePath);
            }

            // Use interlocked to safely increment the counter in a multi-threaded environment
            _ = Interlocked.Increment(ref filesParsed);
        });

        // If the home page is not yet created, create it!
        if (site.HomePage is null)
        {
            var home = CreateIndexPage(string.Empty);
            site.HomePage = home;
        }

        stopwatch.Stop("Parse", filesParsed);
    }

    /// <summary>
    /// Extra calculation and automatic data for each frontmatter.
    /// </summary>
    /// <param name="frontmatter"></param>
    /// <param name="overwrite"></param>
    private void PostProcessFrontMatter(Frontmatter frontmatter, bool overwrite = false)
    {
        frontmatter.Permalink = CreatePermalink(site, frontmatter);
        lock (syncLockPostProcess)
        {
            if (!site.Pages.TryGetValue(frontmatter.Permalink, out var old) || overwrite)
            {
                if (old is not null)
                {
                    if (old?.Pages is not null)
                    {
                        frontmatter.Pages ??= new();
                        foreach (var page in old.Pages)
                        {
                            frontmatter.Pages.Add(page);
                        }
                    }
                }

                // Register the page for all urls
                foreach (var url in frontmatter.Urls)
                {
                    site.Pages[url] = frontmatter;
                }

                if (frontmatter.Kind == Kind.single)
                {
                    site.RegularPages.Add(frontmatter.Permalink, frontmatter);
                }

                if (site.HomePage is null && frontmatter.SourcePath == "index.md")
                {
                    site.HomePage = frontmatter;
                    frontmatter.Kind = Kind.index;
                }

                if (frontmatter.Aliases is not null)
                {
                    for (var i = 0; i < frontmatter.Aliases.Count; i++)
                    {
                        frontmatter.Aliases[i] = "/" + CreatePermalink(site, frontmatter, frontmatter.Aliases[i]);
                    }
                }
            }
        }

        // Create a section page when due
        if (frontmatter.Type != "section")
        {
            var contentTemplate = new BasicContent(
                title: frontmatter.Section,
                section: frontmatter.Section,
                type: "section",
                url: frontmatter.Section
            );
            CreateAutomaticFrontmatter(contentTemplate, frontmatter);
        }
    }

    /// <summary>
    /// Reads the application configuration.
    /// </summary>
    /// <param name="options">The generate options.</param>
    /// <param name="frontmatterParser">The frontmatter parser.</param>
    /// <returns>The site configuration.</returns>
    protected static Site ParseSiteSettings(IGenerateOptions options, IFrontmatterParser frontmatterParser)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }
        if (frontmatterParser is null)
        {
            throw new ArgumentNullException(nameof(frontmatterParser));
        }

        // Read the main configation
        var filePath = Path.Combine(options.Source, configFile);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The {configFile} file was not found in the specified source directory: {options.Source}");
        }

        var fileContent = File.ReadAllText(filePath);
        var settings = frontmatterParser.ParseSiteSettings(fileContent);
        settings.SourcePath = options.Source;
        settings.OutputPath = options.Output;

        return settings;
    }

    /// <summary>
    /// Creates the frontmatter for the index page.
    /// </summary>
    /// <param name="relativePath">The relative path of the page.</param>
    /// <returns>The created frontmatter for the index page.</returns>
    protected Frontmatter CreateIndexPage(string relativePath)
    {
        Frontmatter frontmatter = new(
            baseGeneratorCommand: this,
            title: site.Title,
            site: site,
            sourcePath: Path.Combine(relativePath, "/index.md"),
            sourceFileNameWithoutExtension: "index",
            sourcePathDirectory: null
        )
        {
            Kind = string.IsNullOrEmpty(relativePath) ? Kind.index : Kind.list,
            Section = (string.IsNullOrEmpty(relativePath) ? Kind.index : Kind.list).ToString()
        };

        PostProcessFrontMatter(frontmatter);
        return frontmatter;
    }

    /// <summary>
    /// Gets all Markdown files in the specified directory.
    /// </summary>
    /// <param name="directory">The directory path.</param>
    /// <returns>The list of Markdown file paths.</returns>
    protected static List<string> GetAllMarkdownFiles(string directory)
    {
        var markdownFiles = new List<string>();
        var files = Directory.GetFiles(directory, "*.md");
        markdownFiles.AddRange(files);

        var subdirectories = Directory.GetDirectories(directory);
        foreach (var subdirectory in subdirectories)
        {
            markdownFiles.AddRange(GetAllMarkdownFiles(subdirectory));
        }

        return markdownFiles;
    }

    /// <summary>
    /// Reads the frontmatter from the source file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="content">The file content.</param>
    /// <param name="site">The site instance.</param>
    /// <param name="frontmatterParser">The frontmatter parser.</param>
    /// <returns>The parsed frontmatter.</returns>
    protected Frontmatter ReadSourceFrontmatter(string filePath, string content, Site site, IFrontmatterParser frontmatterParser)
    {
        // test if filePath or config is null
        if (filePath is null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }
        if (site is null)
        {
            throw new ArgumentNullException(nameof(site));
        }
        if (frontmatterParser is null)
        {
            throw new ArgumentNullException(nameof(frontmatterParser));
        }

        // Separate the YAML frontmatter from the file content
        var frontmatter = frontmatterParser.ParseFrontmatter(site, filePath, ref content, this)
            ?? throw new FormatException($"Error parsing frontmatter for {filePath}");

        return frontmatter;
    }

    /// <summary>
    /// Check if the page have a publishing date from the past.
    /// </summary>
    /// <param name="frontmatter">The given page</param>
    /// <returns></returns>
    bool IsValidDate(Frontmatter frontmatter)
    {
        return (frontmatter.PublishDate is null && frontmatter.Date is null)
            || options.Future
            || (frontmatter.PublishDate != null && (frontmatter.PublishDate <= DateTime.Now))
            || (frontmatter.Date != null && (frontmatter.Date <= DateTime.Now));
    }

    /// <summary>
    /// Gets the Permalink path for the file.
    /// </summary>
    /// <param name="site">The site instance.</param>
    /// <param name="frontmatter">The frontmatter.</param>
    /// <param name="URL">The URL to consider. If null, we get frontmatter.URL</param>
    /// <returns>The output path.</returns>
    public string CreatePermalink(Site site, Frontmatter frontmatter, string? URL = null)
    {
        if (frontmatter is null)
        {
            throw new ArgumentNullException(nameof(frontmatter));
        }
        if (site is null)
        {
            throw new ArgumentNullException(nameof(site));
        }

        var isIndex = frontmatter.SourceFileNameWithoutExtension == "index";
        string outputRelativePath;

        URL ??= frontmatter.URL
            ?? (isIndex ? "{{ page.SourcePathDirectory }}" : "{{ page.SourcePathDirectory }}/{{ page.Title }}");

        outputRelativePath = URL;

        if (fluidParser.TryParse(URL, out var template, out var error))
        {
            var context = new TemplateContext(templateOptions)
                .SetValue("page", frontmatter);
            try
            {
                outputRelativePath = template.Render(context);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error converting URL: {Error}", error);
            }
        }

        outputRelativePath = Urlizer.UrlizePath(outputRelativePath);

        if (!string.IsNullOrEmpty(outputRelativePath) && !Path.IsPathRooted(outputRelativePath) && !outputRelativePath.StartsWith("/"))
        {
            outputRelativePath = "/" + outputRelativePath;
        }

        return outputRelativePath;
    }

    /// <summary>
    /// Creates the output file by applying the theme templates to the frontmatter content.
    /// </summary>
    /// <param name="frontmatter">The frontmatter containing the content to process.</param>
    /// <returns>The processed output file content.</returns>
    protected string CreateOutputFile(Frontmatter frontmatter)
    {
        // Theme content
        string result;

        // Process the theme base template
        // If the theme base template file is available, parse and render the template using the frontmatter data
        // Otherwise, use the processed content as the final result
        // Any error during parsing is logged, and an empty string is returned
        // The final result is stored in the 'result' variable and returned
        var fileContents = GetTemplate(site.SourceThemePath, frontmatter, true);
        if (string.IsNullOrEmpty(fileContents))
        {
            result = frontmatter.Content;
        }
        else if (fluidParser.TryParse(fileContents, out var template, out var error))
        {
            var context = new TemplateContext(templateOptions);
            _ = context.SetValue("page", frontmatter);
            result = template.Render(context);
        }
        else
        {
            Log.Error("Error parsing theme template: {Error}", error);
            return string.Empty;
        }

        return result;
    }

    /// <summary>
    /// Gets the content of a template file from the specified list of template paths.
    /// </summary>
    /// <param name="templatePaths">The list of template paths to search.</param>
    /// <returns>The content of the template file, or an empty string if not found.</returns>
    protected static string GetTemplate(List<string> templatePaths)
    {
        if (templatePaths is null)
        {
            throw new ArgumentNullException(nameof(templatePaths));
        }

        // Iterate through the template paths and return the content of the first existing file
        foreach (var templatePath in templatePaths)
        {
            if (File.Exists(templatePath))
            {
                return File.ReadAllText(templatePath);
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the content of a template file based on the frontmatter and the theme path.
    /// </summary>
    /// <param name="themePath">The theme path.</param>
    /// <param name="frontmatter">The frontmatter to determine the template index.</param>
    /// <param name="isBaseTemplate">Indicates whether the template is a base template.</param>
    /// <returns>The content of the template file.</returns>
    protected string GetTemplate(string themePath, Frontmatter frontmatter, bool isBaseTemplate = false)
    {
        if (frontmatter is null)
        {
            throw new ArgumentNullException(nameof(frontmatter));
        }

        var index = (frontmatter.Section, frontmatter.Kind, frontmatter.Type);

        var cache = isBaseTemplate ? baseTemplateCache : contentTemplateCache;

        // Check if the template content is already cached
        if (!cache.TryGetValue(index, out var content))
        {
            var templatePaths = GetTemplateLookupOrder(themePath, frontmatter, isBaseTemplate);
            content = GetTemplate(templatePaths);

            // Cache the template content for future use
            lock (cache)
            {
                _ = cache.TryAdd(index, content);
            }
        }

        return content;
    }

    /// <summary>
    /// Gets the lookup order for template files based on the theme path, frontmatter, and template type.
    /// </summary>
    /// <param name="themePath">The theme path.</param>
    /// <param name="frontmatter">The frontmatter to determine the template index.</param>
    /// <param name="isBaseTemplate">Indicates whether the template is a base template.</param>
    /// <returns>The list of template paths in the lookup order.</returns>
    protected static List<string> GetTemplateLookupOrder(string themePath, Frontmatter frontmatter, bool isBaseTemplate)
    {
        if (frontmatter is null)
        {
            throw new ArgumentNullException(nameof(frontmatter));
        }

        // Generate the lookup order for template files based on the theme path, frontmatter section, type, and kind
        var sections = new string[] { frontmatter.Section.ToString(), string.Empty };
        var types = new string[] { frontmatter.Type.ToString(), "_default" };
        var kinds = isBaseTemplate
            ? new string[] { frontmatter.Kind.ToString() + "-baseof", "baseof" }
            : new string[] { frontmatter.Kind.ToString() };
        var templatePaths = new List<string>();
        foreach (var section in sections)
        {
            foreach (var type in types)
            {
                foreach (var kind in kinds)
                {
                    var path = Path.Combine(themePath, section, type, kind) + ".liquid";
                    templatePaths.Add(path);
                }
            }
        }
        return templatePaths;
    }

    /// <summary>
    /// Resets the template cache to force a reload of all templates.
    /// </summary>
    protected void ResetCache()
    {
        baseTemplateCache.Clear();
        contentTemplateCache.Clear();
        automaticContentCache.Clear();
        site.Pages.Clear();
        IgnoreCacheBefore = DateTime.Now;
    }

    /// <summary>
    /// Fluid/Liquid filter to navigate Params dictionary
    /// </summary>
    /// <param name="input"></param>
    /// <param name="arguments"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static ValueTask<FluidValue> WhereParamsFilter(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }
        if (arguments is null)
        {
            throw new ArgumentNullException(nameof(arguments));
        }

        List<FluidValue> result = new();
        var list = (input as ArrayValue)!.Values;

        var keys = arguments.At(0).ToStringValue().Split('.');
        foreach (var item in list)
        {
            if (item.ToObjectValue() is IParams param && CheckValueInDictionary(keys, param.Params, arguments.At(1).ToStringValue()))
            {
                result.Add(item);
            }
        }

        return new ValueTask<FluidValue>(new ArrayValue((IEnumerable<FluidValue>)result));
    }

    static bool CheckValueInDictionary(string[] array, Dictionary<string, object> dictionary, string value)
    {
        var key = array[0];

        // Check if the key exists in the dictionary
        if (dictionary.TryGetValue(key, out var dictionaryValue))
        {
            // If it's the last element in the array, check if the dictionary value matches the value parameter
            if (array.Length == 1)
            {
                return dictionaryValue.Equals(value);
            }

            // Check if the value is another dictionary
            else if (dictionaryValue is Dictionary<string, object> nestedDictionary)
            {
                // Create a new array without the current key
                var newArray = new string[array.Length - 1];
                Array.Copy(array, 1, newArray, 0, newArray.Length);

                // Recursively call the method with the nested dictionary and the new array
                return CheckValueInDictionary(newArray, nestedDictionary, value);
            }
        }

        // If the key doesn't exist or the value is not a dictionary, return false
        return false;
    }
}

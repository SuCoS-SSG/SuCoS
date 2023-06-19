using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluid;
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
    protected static readonly FluidParser parser = new();

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
    protected readonly Dictionary<string, Frontmatter> tagFrontmatterCache = new();

    /// <summary>
    /// The synchronization lock object.
    /// </summary>
    protected readonly object syncLock = new();

    /// <summary>
    /// The template options.
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

    /// <inheritdoc/>
    public Frontmatter CreateTagFrontmatter(Site site, string tagName, Frontmatter originalFrontmatter)
    {
        if (originalFrontmatter is null)
        {
            throw new ArgumentNullException(nameof(originalFrontmatter));
        }

        Frontmatter? frontmatter = null;
        lock (syncLock)
        {
            if (!tagFrontmatterCache.TryGetValue(tagName, out frontmatter))
            {
                if (site is null)
                {
                    throw new ArgumentNullException(nameof(site));
                }

                frontmatter = new(
                    BaseGeneratorCommand: this,
                    Site: site,
                    Title: tagName,
                    SourcePath: "tags",
                    SourceFileNameWithoutExtension: string.Empty,
                    SourcePathDirectory: null
                )
                {
                    Section = "tags",
                    Kind = Kind.list,
                    Type = "tags",
                    URL = "tags/" + Urlizer.Urlize(tagName),
                    ContentRaw = $"# {tagName}",
                    Pages = new()
                };
                frontmatter.Permalink = "/" + GetOutputPath(frontmatter.URL, site, frontmatter);
                site.Pages.Add(frontmatter);
                tagFrontmatterCache.Add(tagName, frontmatter);
            }
        }
        lock (frontmatter?.Pages!)
        {
            frontmatter.Pages!.Add(originalFrontmatter);
            originalFrontmatter.Tags ??= new();
            originalFrontmatter.Tags!.Add(frontmatter);
        }
        return frontmatter;
    }

    /// <inheritdoc/>
    public string CreateFrontmatterContent(Frontmatter frontmatter)
    {
        var fileContents = GetTemplate(site.SourceThemePath, frontmatter);
        // Theme content
        if (string.IsNullOrEmpty(value: fileContents))
        {
            return frontmatter.Content;
        }
        else if (parser.TryParse(fileContents, out var template, out var error))
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
            site = ReadAppConfig(options: options, frontmatterParser);
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
                    site.Pages.Add(frontmatter);
                    site.RegularPages.Add(frontmatter);

                    // Convert the Markdown content to HTML
                    frontmatter.ContentPreRendered = Markdown.ToHtml(frontmatter.ContentRaw);
                    frontmatter.Permalink = "/" + GetOutputPath(file.filePath, site, frontmatter);

                    if (site.HomePage is null && string.IsNullOrEmpty(frontmatter.SourcePath) && frontmatter.SourceFileNameWithoutExtension == "index")
                    {
                        site.HomePage = frontmatter;
                    }
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
            var home = CreateIndexPage(site, string.Empty);
            site.HomePage = home;
            site.Pages.Add(home);
        }

        stopwatch.Stop("Parse", filesParsed);
    }

    /// <summary>
    /// Reads the application configuration.
    /// </summary>
    /// <param name="options">The generate options.</param>
    /// <param name="frontmatterParser">The frontmatter parser.</param>
    /// <returns>The site configuration.</returns>
    protected static Site ReadAppConfig(IGenerateOptions options, IFrontmatterParser frontmatterParser)
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
        var configFilePath = Path.Combine(options.Source, configFile);
        if (!File.Exists(configFilePath))
        {
            throw new FileNotFoundException($"The {configFile} file was not found in the specified source directory: {options.Source}");
        }

        var configFileContent = File.ReadAllText(configFilePath);
        var config = frontmatterParser.ParseAppConfig(configFileContent);
        config.SourcePath = options.Source;
        config.OutputPath = options.Output;

        return config;
    }

    /// <summary>
    /// Creates the frontmatter for the index page.
    /// </summary>
    /// <param name="site">The site instance.</param>
    /// <param name="relativePath">The relative path of the page.</param>
    /// <returns>The created frontmatter for the index page.</returns>
    protected Frontmatter CreateIndexPage(Site site, string relativePath)
    {
        if (site is null)
        {
            throw new ArgumentNullException(nameof(site));
        }

        Frontmatter frontmatter = new(
            BaseGeneratorCommand: this,
            Title: site.Title,
            Site: site,
            SourcePath: Path.Combine(relativePath, "index"),
            SourceFileNameWithoutExtension: "index",
            SourcePathDirectory: null
        )
        {
            Kind = string.IsNullOrEmpty(relativePath) ? Kind.index : Kind.list,
            Permalink = "/index.html",
        };
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
    /// Gets the output path for the file.
    /// </summary>
    /// <param name="fileRelativePath">The file's relative path.</param>
    /// <param name="site">The site instance.</param>
    /// <param name="frontmatter">The frontmatter.</param>
    /// <returns>The output path.</returns>
    protected static string GetOutputPath(string fileRelativePath, Site site, Frontmatter frontmatter)
    {
        if (frontmatter is null)
        {
            throw new ArgumentNullException(nameof(frontmatter));
        }
        if (fileRelativePath is null)
        {
            throw new ArgumentNullException(nameof(fileRelativePath));
        }
        if (site is null)
        {
            throw new ArgumentNullException(nameof(site));
        }

        var isIndex = frontmatter.SourceFileNameWithoutExtension == "index";
        var outputRelativePath = isIndex ? ".html" : "/index.html";

        // TODO: Tokenize the URL instead of hardcoding the usage of the title
        if (!string.IsNullOrEmpty(frontmatter.URL))
        {
            outputRelativePath = $"{frontmatter.URL}{outputRelativePath}";
        }
        else
        {
            var folderRelativePath = Path.GetDirectoryName(fileRelativePath.Replace(site.SourceContentPath, string.Empty, StringComparison.InvariantCultureIgnoreCase));

            var documentTitle = !isIndex && string.IsNullOrEmpty(frontmatter.Title)
                ? frontmatter.Title
                : frontmatter.SourceFileNameWithoutExtension;

            outputRelativePath = Path.Combine(folderRelativePath ?? string.Empty, path2: $"{documentTitle}" + outputRelativePath);
        }

        outputRelativePath = Urlizer.UrlizePath(outputRelativePath);

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
        else if (parser.TryParse(fileContents, out var template, out var error))
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
        tagFrontmatterCache.Clear();
        IgnoreCacheBefore = DateTime.Now;
    }
}

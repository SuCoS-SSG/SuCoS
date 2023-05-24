using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluid;
using Markdig;
using Serilog;
using SuCoS.Models;
using SuCoS.Parser;

namespace SuCoS;

/// <summary>
/// Build Command will build the site based on the source files.
/// </summary>
public class BuildCommand : ITaxonomyCreator
{
    private const string configFile = "sucos.yaml";
    private static readonly FluidParser parser = new();
    private readonly Site site;
    private readonly IFrontmatterParser frontmatterParser = new YAMLParser();
    private readonly BuildOptions options;
    private readonly Dictionary<(string, Kind, string), string> contentTemplateCache = new();
    private readonly Dictionary<(string, Kind, string), string> baseTemplateCache = new();
    private readonly Dictionary<string, Frontmatter> tagFrontmatterCache = new();
    private readonly object syncLock = new();

    /// <summary>
    /// Entry point of the build command. It will be called by the main program
    /// in case the build command is invoked (which is by default).
    /// </summary>
    /// <param name="options"></param>
    public BuildCommand(BuildOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }
        this.options = options;

        Log.Information("Source path: {source}", propertyValue: options.Source);
        Log.Information("Output path: {output}", options.Output);

        try
        {
            site = ReadAppConfig(options, frontmatterParser);
            if (site is null)
            {
                throw new FormatException("Error reading app config");
            }
        }
        catch
        {
            throw new FormatException("Error reading app config");
        }

        // Scan conetnt files
        var markdownFiles = GetAllMarkdownFiles(site.SourceContentPath);

        foreach (var fileAbsolutePath in markdownFiles)
        {
            var content = File.ReadAllText(fileAbsolutePath);
            var relativePath = Path.GetRelativePath(site.SourceContentPath, fileAbsolutePath);
            site.RawPages.Add((relativePath, content));
        }

        // Create a stopwatch to measure the time taken
        var stopwatchParse = Stopwatch.StartNew();

        // Process the source files, extracting the frontmatter
        var filesParsed = 0; // counter to keep track of the number of files processed
        _ = Parallel.ForEach(site.RawPages, file =>
        {
            var frontmatter = ReadSourceFrontmatter(file.filePath, file.content, site, frontmatterParser);
            site.Pages.Add(frontmatter);
            site.RegularPages.Add(frontmatter);

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

        stopwatchParse.Stop();
        var stopwatchCreate = Stopwatch.StartNew();

        // Print each page
        var pagesCreated = 0; // counter to keep track of the number of pages created
        _ = Parallel.ForEach(site.Pages, frontmatter =>
        {
            PrintPage(frontmatter);

            // Use interlocked to safely increment the counter in a multi-threaded environment
            _ = Interlocked.Increment(ref pagesCreated);
        });

        // Stop the stopwatch
        stopwatchCreate.Stop();

        var reportData = new[]
         {
            new { Step = "Step", Status = "Status", Duration = "Duration" },
            new { Step = "Parse", Status = "{s1} files", Duration = "{d1} ms" },
            new { Step = "Create", Status = "{s2} pages", Duration = "{d2} ms" },
            new { Step = "Total", Status = "", Duration = "{dt} ms" }
        };
        var report = @"Site '{Title}' created!
═════════════════════════════════════════════";

        for (var i = 0; i < reportData.Length; i++)
        {
            if (i == 1 || i == reportData.Length - 1)
            {
                report += @"
─────────────────────────────────────────────";
            }
            report += $"\n{reportData[i].Step,-20} {reportData[i].Status,-15} {reportData[i].Duration,-10}";
        }
        report += @" 
═════════════════════════════════════════════";
        // Log the report
        Log.Information(report, site.Title,
            filesParsed, stopwatchParse.ElapsedMilliseconds,
            pagesCreated, stopwatchCreate.ElapsedMilliseconds,
            stopwatchParse.ElapsedMilliseconds + stopwatchCreate.ElapsedMilliseconds);
    }

    private static Site ReadAppConfig(BuildOptions options, IFrontmatterParser frontmatterParser)
    {
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

    private Frontmatter ReadSourceFrontmatter(string filePath, string content, Site site, IFrontmatterParser frontmatterParser)
    {
        // test if filePath or config is null
        if (filePath is null || site is null)
        {
            throw new ArgumentNullException(nameof(site));
        }

        // Separate the YAML frontmatter from the file content
        var frontmatter = frontmatterParser.ParseFrontmatter(site, filePath, ref content, this)
            ?? throw new FormatException($"Error parsing frontmatter for {filePath}");

        // Convert the Markdown content to HTML
        frontmatter.Content = Markdown.ToHtml(frontmatter.ContentRaw);
        frontmatter.Permalink = GetOutputPath(filePath, site, frontmatter);

        if (site.HomePage is null && string.IsNullOrEmpty(frontmatter.SourcePath) && frontmatter.SourceFileNameWithoutExtension == "index")
        {
            site.HomePage = frontmatter;
        }

        return frontmatter;
    }

    private static List<string> GetAllMarkdownFiles(string directory)
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

    private static string GetOutputPath(string fileRelativePath, Site site, Frontmatter frontmatter)
    {
        if (frontmatter is null)
        {
            throw new ArgumentNullException(nameof(frontmatter));
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

    private string GetTemplate(string themePath, Frontmatter frontmatter, bool isBaseTemplate = false)
    {
        var index = (frontmatter.Section, frontmatter.Kind, frontmatter.Type);

        var cache = isBaseTemplate ? baseTemplateCache : contentTemplateCache;

        if (!cache.TryGetValue(index, out var content))
        {
            var templatePaths = GetTemplateLookupOrder(themePath, frontmatter, isBaseTemplate);
            content = GetTemplate(templatePaths);
            lock (cache)
            {
                _ = cache.TryAdd(index, content);
            }
        }

        return content;
    }

    private static List<string> GetTemplateLookupOrder(string themePath, Frontmatter frontmatter, bool isBaseTemplate)
    {
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
                    var path = Path.Combine(themePath, section, type, kind) + ".html";
                    templatePaths.Add(path);
                }
            }
        }
        return templatePaths;
    }

    private static string GetTemplate(List<string> templatePaths)
    {
        for (var i = 0; i < templatePaths.Count; i++)
        {
            if (File.Exists(templatePaths[i]))
            {
                return File.ReadAllText(templatePaths[i]);
            }
        }
        return string.Empty;
    }

    private static Frontmatter CreateIndexPage(Site site, string relativePath)
    {
        Frontmatter frontmatter = new(

            Title: site.Title,
            Site: site,
            SourcePath: Path.Combine(relativePath, "index"),
            SourceFileNameWithoutExtension: "index",
            SourcePathDirectory: null
        )
        {
            Kind = string.IsNullOrEmpty(relativePath) ? Kind.index : Kind.list,
            Permalink = "index.html",
        };
        return frontmatter;
    }

    private void PrintPage(Frontmatter frontmatter)
    {
        // Generate the output path
        var fileAbsolutePath = Path.Combine(site.OutputPath, frontmatter.Permalink!);
        var outputDirectory = Path.GetDirectoryName(fileAbsolutePath);
        if (!Directory.Exists(outputDirectory))
        {
            _ = Directory.CreateDirectory(outputDirectory!);
        }

        string result;
        var themePath = Path.Combine(Path.GetFullPath(site.SourcePath), "theme");

        // Theme content
        IFluidTemplate? template;
        string? error;
        var fileContents = GetTemplate(themePath, frontmatter);
        if (string.IsNullOrEmpty(value: fileContents))
        {
            frontmatter.ContentPreRendered = frontmatter.Content;
        }
        else if (parser.TryParse(fileContents, out template, out error))
        {
            var context = new TemplateContext(frontmatter);
            frontmatter.ContentPreRendered = frontmatter.Content;
            frontmatter.Content = template.Render(context);
        }
        else
        {
            Log.Error("Error parsing theme template: {Error}", error);
            return;
        }

        // Theme base, which wraps the content into common HTML elements, like header, footer, etc.
        fileContents = GetTemplate(themePath, frontmatter, true);
        if (string.IsNullOrEmpty(fileContents))
        {
            result = frontmatter.Content;
        }
        else if (parser.TryParse(fileContents, out template, out error))
        {
            var context = new TemplateContext(frontmatter);
            result = template.Render(context);
        }
        else
        {
            Log.Error("Error parsing theme template: {Error}", error);
            return;
        }

        // Save the processed output to the final file
        File.WriteAllText(fileAbsolutePath, result);

        // Log
        if (options.Verbose)
        {
            Log.Information("Page created: {Permalink}", frontmatter.Permalink);
        }
    }

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
                frontmatter.Permalink = GetOutputPath(frontmatter.URL, site, frontmatter);
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
}

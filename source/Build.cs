using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluid;
using Markdig;
using Serilog;

namespace SuCoS;

/// <summary>
/// Build Command will build the site based on the source files.
/// </summary>
public class Build
{
    private const string configFile = "sucos.yaml";
    private AppConfig? config { get; set; }
    private static readonly FluidParser parser = new();
    private readonly IFrontmatterParser frontmatterParser = new YAMLParser();

    /// <summary>
    /// Entry point of the build command. It will be called by the main program
    /// in case the build command is invoked (which is by default).
    /// </summary>
    /// <param name="source"></param>
    /// <param name="output"></param>
    public void HandleCommand(string source, string output)
    {
        try
        {
            ReadAppConfig(source, output, frontmatterParser);
        }
        catch
        {
            throw new FormatException("Error reading app config");
        }

        var SourcePathAbsolute = Path.GetFullPath(config!.SourcePath);

        // Scan conetnt files
        var markdownFiles = GetAllMarkdownFiles(source);

        // New List for storing file content along with the file path
        var markdownFilesContent = new List<(string filePath, string content)>();

        foreach (var filePath in markdownFiles)
        {
            var content = File.ReadAllText(filePath);
            markdownFilesContent.Add((filePath, content));
        }

        // Create a stopwatch to measure the time taken
        var stopwatch = Stopwatch.StartNew();
        var filesProcessed = 0; // counter to keep track of the number of files processed

        _ = Parallel.ForEach(markdownFilesContent, file =>
        {
            ProcessMarkdownFile(file.filePath, file.content, config, frontmatterParser);

            // Use interlocked to safely increment the counter in a multi-threaded environment
            Interlocked.Increment(ref filesProcessed);
        });

        // Stop the stopwatch
        stopwatch.Stop();

        Log.Information("Site {Title} generation complete!", config.Title);
        Log.Information("Processed {filesProcessed} files in {elapsedTime} ms", filesProcessed, stopwatch.ElapsedMilliseconds);
    }

    private void ReadAppConfig(string source, string output, IFrontmatterParser frontmatterParser)
    {
        // Read the main configation
        var configFilePath = Path.Combine(source, configFile);
        if (!File.Exists(configFilePath))
        {
            Log.Error("The {configFile} file was not found in the specified source directory: {Source}", configFile, source);
            return;
        }

        var configFileContent = File.ReadAllText(configFilePath);
        config = frontmatterParser.ParseAppConfig(configFileContent);
        config.SourcePath = source;
        config.OutputPath = output;
    }

    private static void ProcessMarkdownFile(string filePath, string content, AppConfig config, IFrontmatterParser frontmatterParser)
    {
        // test if filePath or config is null
        if (filePath is null || config is null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        // Separate the YAML frontmatter from the file content
        var frontmatter = frontmatterParser.ParseFrontmatter(ref content);

        // Convert the Markdown content to HTML
        frontmatter.Content = Markdown.ToHtml(frontmatter.ContentRaw);

        // Generate the output path
        frontmatter.Permalink = GenerateOutputPath(filePath, config, frontmatter);
        var outputDirectory = Path.GetDirectoryName(frontmatter.Permalink);
        if (!Directory.Exists(outputDirectory))
        {
            _ = Directory.CreateDirectory(outputDirectory!);
        }

        var result = "";
        var templatePath = Path.Combine(Path.GetFullPath(config.SourcePath), "theme", "default/baseof.liquid");

        var fileContents = File.ReadAllText(templatePath);
        if (parser.TryParse(fileContents, out var template, out _))
        {
            var context = new TemplateContext(frontmatter);
            result = template.Render(context);
        }

        // Save the processed output to the final file
        File.WriteAllText(frontmatter.Permalink, result);

        // Log
        Log.Information("Page created: {Permalink}", frontmatter.Permalink);
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

    private static string GenerateOutputPath(string filePath, AppConfig config, Frontmatter frontmatter)
    {
        var folderPath = Path.GetDirectoryName(filePath.Replace(config.SourceContentPath, config.OutputPath, StringComparison.InvariantCultureIgnoreCase));

        var documentTitle = frontmatter?.Title ?? Path.GetFileNameWithoutExtension(filePath);
        var urlizedTitle = Urlizer.Urlize(documentTitle);

        // Check if the URL value is set in the frontmatter
        var urlValue = frontmatter?.URL ?? string.Empty;

        var outputPath = !string.IsNullOrEmpty(urlValue)
            ? Path.Combine(config.OutputPath ?? string.Empty, $"{urlValue}.html")
            : Path.Combine(folderPath ?? string.Empty, $"{urlizedTitle}.html");

        return outputPath;
    }
}

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Markdig;
using RazorLight;
using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SuCoS;

public class Program
{
    private const string configFile = "app.yaml";
    private AppConfig? config { get; set; }

    static int Main(string[] args)
    {
        var SuCoS = new Program();
        return SuCoS.Run(args);
    }

    private int Run(string[] args)
    {
        // use Serilog to log the program's output
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.CurrentCulture)
            .CreateLogger();

        // Print the name and version of the program.git remote add origin git@gitlab.com:brmassa/sucos.git
        var assembly = Assembly.GetEntryAssembly();
        var assemblyName = assembly?.GetName();
        var appName = assemblyName?.Name;
        var appVersion = assemblyName?.Version;
        Log.Information("{name} v{version}", appName, appVersion);

        // Print the logo of the program.
        OutputLogo();

        var buildSourceOption = new Option<string>(new[] { "--source", "-s" }, () => ".", "Source directory path");
        var buildOutputOption = new Option<string>(new[] { "--output", "-o" }, () => "./public", "Output directory path");
        var buildCommand = new RootCommand
            {
                buildSourceOption,
                buildOutputOption
            };
        buildCommand.Description = "Build commands";
        buildCommand.SetHandler((source, output) =>
            {
                Log.Information("Source path: {source}", source);
                Log.Information("Output path: {output}", output);
                HandleBuildCommand(source, output!);
            },
            buildSourceOption, buildOutputOption);

        return buildCommand.Invoke(args);
    }

    private void HandleBuildCommand(string source, string output)
    {
        // Read the main configation
        var configFilePath = Path.Combine(source, configFile);
        if (!File.Exists(configFilePath))
        {
            Log.Error("The {configFile} file was not found in the specified source directory: {Source}", configFile, source);
            return;
        }
        config = ReadAppConfig(configFilePath);
        config.SourcePath = source;
        config.OutputPath = output;
        var SourcePathAbsolute = Path.GetFullPath(config.SourcePath);
        config.RazorEngine = new RazorLightEngineBuilder()
            .UseFileSystemProject(root: Path.Combine(SourcePathAbsolute, "theme"))
            .UseMemoryCachingProvider()
            .Build();

        // Scan conetnt files
        var markdownFiles = GetAllMarkdownFiles(source);

        _ = Parallel.ForEach(markdownFiles, filePath =>
        {
            _ = ProcessMarkdownFile(filePath, config);
        });

        Log.Information("Site {Title} generation complete!", config.Title);
    }

    private static AppConfig ReadAppConfig(string configFilePath)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var configFileContent = File.ReadAllText(configFilePath);
        var config = deserializer.Deserialize<AppConfig>(configFileContent);
        return config;
    }

    public static string ProcessMarkdownFile(string filePath, AppConfig config)
    {
        // test if filePath or config is null
        if (filePath is null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }
        if (config is null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        // Read the Markdown file content
        var fileContent = File.ReadAllText(filePath);

        // Separate the YAML frontmatter from the file content
        var frontmatter = ExtractFrontmatter(ref fileContent);

        // Convert the Markdown content to HTML
        var htmlContent = Markdown.ToHtml(frontmatter.Content);

        // Generate the output path
        var outputPath = GenerateOutputPath(filePath, config, frontmatter);
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!Directory.Exists(outputDirectory))
        {
            _ = Directory.CreateDirectory(outputDirectory!);
        }

        // Use RazorLight to process the Razor page template
        var templateKey = "/default/baseof"; // Key or path to the Razor page template
        var model = new BaseofViewModel
        {
            Content = htmlContent
        };
        var result = config.RazorEngine?.CompileRenderAsync(templateKey, model).GetAwaiter().GetResult();

        // Save the processed output to the final file
        File.WriteAllText(outputPath, result);
        Log.Information("Page created: {outputPath}", outputPath);

        return outputPath;
    }

    private static Frontmatter ExtractFrontmatter(ref string fileContent)
    {
        Frontmatter frontmatter = new();
        var match = Regex.Match(fileContent, @"^---\s*\n(?<frontmatter>.*?)\n---\s*\n", RegexOptions.Singleline);
        if (match.Success)
        {
            var frontmatterString = match.Groups["frontmatter"].Value;
            fileContent = fileContent[match.Length..].TrimStart('\n');

            // Parse the front matter string into Frontmatter properties
            var yamlDeserializer = new DeserializerBuilder().Build();
            var yamlObject = yamlDeserializer.Deserialize(new StringReader(frontmatterString));

            if (yamlObject is Dictionary<object, object> frontmatterDictionary)
            {
                _ = (frontmatterDictionary.TryGetValue("Title", out var titleValue) && titleValue != null).ToString();
                _ = (frontmatterDictionary.TryGetValue("URL", out var urlValue) && urlValue != null).ToString();
                List<string> tags = new();
                if (frontmatterDictionary.TryGetValue("Tags", out var tagsValue) && tagsValue is List<object> tagsList)
                {
                    tags = tagsList.Select(tag => tag.ToString()!).ToList();
                }

                frontmatter = new Frontmatter
                {
                    Title = titleValue?.ToString(),
                    URL = urlValue?.ToString(),
                    Tags = tags,
                    Content = fileContent
                };
            }
        }
        else
        {
            frontmatter.Content = fileContent;
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

    private static string GenerateOutputPath(string filePath, AppConfig config, Frontmatter frontmatterObject)
    {
        var folderPath = Path.GetDirectoryName(filePath.Replace(config.SourceContentPath, config.OutputPath, StringComparison.InvariantCultureIgnoreCase));
        var urlizedTitle = Urlizer.Urlize(frontmatterObject?.Title ?? string.Empty);

        // Check if the URL value is set in the frontmatter
        var urlValue = frontmatterObject?.URL ?? string.Empty;

        var outputPath = !string.IsNullOrEmpty(urlValue)
            ? Path.Combine(config.OutputPath ?? string.Empty, $"{urlValue}.html")
            : Path.Combine(folderPath ?? string.Empty, $"{urlizedTitle}.html");

        return outputPath;
    }

    private static void OutputLogo()
    {
        Log.Information(@"
 ____             ____            ____       
/\  _`\          /\  _`\         /\  _`\     
\ \,\L\_\  __  __\ \ \/\_\    ___\ \,\L\_\   
 \/_\__ \ /\ \/\ \\ \ \/_/_  / __`\/_\__ \   
   /\ \L\ \ \ \_\ \\ \ \L\ \/\ \L\ \/\ \L\ \ 
   \ `\____\ \____/ \ \____/\ \____/\ `\____\
    \/_____/\/___/   \/___/  \/___/  \/_____/
                                             
");
    }
}

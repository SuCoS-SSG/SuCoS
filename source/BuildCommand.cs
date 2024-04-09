using Serilog;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;

namespace SuCoS;

/// <summary>
/// Build Command will build the site based on the source files.
/// </summary>
public class BuildCommand : BaseGeneratorCommand
{
    private readonly BuildOptions options;

    /// <summary>
    /// Entry point of the build command. It will be called by the main program
    /// in case the build command is invoked (which is by default).
    /// </summary>
    /// <param name="options">Command line options</param>
    /// <param name="logger">The logger instance. Injectable for testing</param>
    public BuildCommand(BuildOptions options, ILogger logger) : base(options, logger)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));

        logger.Information("Output path: {output}", options.Output);

        // Generate the site pages
        CreateOutputFiles();

        // Copy theme static folder files into the root of the output folder
        if (site.Theme is not null)
        {
            CopyFolder(site.Theme.StaticFolder, options.Output);
        }

        // Copy static folder files into the root of the output folder
        CopyFolder(site.SourceStaticPath, options.Output);

        // Generate the build report
        stopwatch.LogReport(site.Title);
    }

    private void CreateOutputFiles()
    {
        stopwatch.Start("Create");

        // Print each page
        var pagesCreated = 0; // counter to keep track of the number of pages created
        _ = Parallel.ForEach(site.OutputReferences, pair =>
        {
            var (url, output) = pair;

            if (output is IPage page)
            {
                var path = (url + (site.UglyURLs ? "" : "/index.html")).TrimStart('/');

                // Generate the output path
                var outputAbsolutePath = Path.Combine(options.Output, path);

                var outputDirectory = Path.GetDirectoryName(outputAbsolutePath);
                _ = Directory.CreateDirectory(outputDirectory!);

                // Save the processed output to the final file
                var result = page.CompleteContent;
                File.WriteAllText(outputAbsolutePath, result);

                // Log
                logger.Debug("Page created: {Permalink}", outputAbsolutePath);

                // Use interlocked to safely increment the counter in a multi-threaded environment
                _ = Interlocked.Increment(ref pagesCreated);
            }
            else if (output is IResource resource)
            {
                var outputAbsolutePath = Path.Combine(options.Output, resource.Permalink!.TrimStart('/'));

                var outputDirectory = Path.GetDirectoryName(outputAbsolutePath);
                _ = Directory.CreateDirectory(outputDirectory!);

                // Copy the file to the output folder
                File.Copy(resource.SourceFullPath, outputAbsolutePath, overwrite: true);
            }
        });

        // Stop the stopwatch
        stopwatch.Stop("Create", pagesCreated);
    }

    /// <summary>
    /// Copy a folder content from source into the output folder.
    /// </summary> 
    /// <param name="source">The source folder to copy from.</param>
    /// <param name="output">The output folder to copy to.</param>
    private static void CopyFolder(string source, string output)
    {
        // Check if the source folder even exists
        if (!Directory.Exists(source))
        {
            return;
        }

        // Create the output folder if it doesn't exist
        _ = Directory.CreateDirectory(output);

        // Get all files in the source folder
        var files = Directory.GetFiles(source);

        foreach (var fileFullPath in files)
        {
            // Get the filename from the full path
            var fileName = Path.GetFileName(fileFullPath);

            // Create the destination path by combining the output folder and the filename
            var destinationFullPath = Path.Combine(output, fileName);

            // Copy the file to the output folder
            File.Copy(fileFullPath, destinationFullPath, overwrite: true);
        }
    }
}

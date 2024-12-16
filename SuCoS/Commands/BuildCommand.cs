using Serilog;
using SuCoS.Helpers;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;

namespace SuCoS.Commands;

/// <summary>
/// Build Command will build the site based on the source files.
/// </summary>
public class BuildCommand : BaseGeneratorCommand
{
    private readonly BuildOptions _options;

    /// <summary>
    /// Entry point of the build command. It will be called by the main program
    /// in case the build command is invoked (which is by default).
    /// </summary>
    /// <param name="options">Command line options</param>
    /// <param name="logger">The logger instance. Injectable for testing</param>
    /// <param name="fs"></param>
    public BuildCommand(BuildOptions options, ILogger logger, IFileSystem fs)
        : base(options, logger, fs)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Run the command
    /// </summary>
    public int Run()
    {
        Logger.Information("Output path: {output}", _options.Output);

        // Generate the site pages
        CreateOutputFiles();

        // Copy theme static folder files into the root of the output folder
        if (Site.Theme is not null)
        {
            CopyFolder(Site.Theme.StaticFolder, _options.Output);
        }

        // Copy static folder files into the root of the output folder
        CopyFolder(Site.SourceStaticPath, _options.Output);

        // Generate the build report
        Stopwatch.LogReport(Site.Title);

        return 0;
    }

    private void CreateOutputFiles()
    {
        Stopwatch.Start("Create");

        // Print each page
        var pagesCreated = 0; // counter to keep track of the number of pages created
        _ = Parallel.ForEach(Site.OutputReferences, pair =>
        {
            var (path, output) = pair;

            if (output is IPage page)
            {
                // Generate the output path
                var outputAbsolutePath = Path.Combine(_options.Output, path.TrimStart('/'));

                var outputDirectory = Path.GetDirectoryName(outputAbsolutePath);
                Fs.DirectoryCreateDirectory(outputDirectory!);

                // Save the processed output to the final file
                var result = page.CompleteContent;
                Fs.FileWriteAllText(outputAbsolutePath, result);

                // Use interlocked to safely increment the counter in a multithreaded environment
                _ = Interlocked.Increment(ref pagesCreated);

                // Log
                Logger.Debug("Page created {pagesCreated}: {Permalink}", pagesCreated, outputAbsolutePath);
            }
            else if (output is IResource resource)
            {
                var inputAbsolutePath = Path.Combine(Site.SourceContentPath, resource.SourceRelativePath);
                var outputAbsolutePath = Path.Combine(_options.Output, resource.RelPermalink.TrimStart('/'));

                var outputDirectory = Path.GetDirectoryName(outputAbsolutePath);
                Fs.DirectoryCreateDirectory(outputDirectory!);

                // Copy the file to the output folder
                Fs.FileCopy(inputAbsolutePath, outputAbsolutePath, overwrite: true);
            }
        });

        // Stop the stopwatch
        Stopwatch.Stop("Create", pagesCreated);
    }

    /// <summary>
    /// Copy a folder content from source into the output folder.
    /// </summary>
    /// <param name="source">The source folder to copy from.</param>
    /// <param name="output">The output folder to copy to.</param>
    public void CopyFolder(string source, string output)
    {
        // Check if the source folder even exists
        if (!Fs.DirectoryExists(source))
        {
            return;
        }

        // Create the output folder if it doesn't exist
        Fs.DirectoryCreateDirectory(output);

        // Get all files in the source folder
        var files = Fs.DirectoryGetFiles(source);

        foreach (var fileFullPath in files)
        {
            // Get the filename from the full path
            var fileName = Path.GetFileName(fileFullPath);

            // Create the destination path by combining the output folder and the filename
            var destinationFullPath = Path.Combine(output, fileName);

            // Copy the file to the output folder
            Fs.FileCopy(fileFullPath, destinationFullPath, overwrite: true);
        }
    }
}

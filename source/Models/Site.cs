using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Serilog;

namespace SuCoS.Models;

/// <summary>
/// The main configuration of the program, primarily extracted from the app.yaml file.
/// </summary>
public class Site
{
    /// <summary>
    /// Site Title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The base URL that will be used to build internal links.
    /// </summary>
    public string BaseUrl { get; set; } = "./";

    /// <summary>
    /// The base path of the source site files.
    /// </summary>
    public string SourcePath { get; set; } = "./";

    /// <summary>
    /// The path where the generated site files will be saved.
    /// </summary>
    public string OutputPath { get; set; } = "./";

    /// <summary>
    /// The path of the content, based on the source path.
    /// </summary>
    public string SourceContentPath => Path.Combine(SourcePath, "content");

    /// <summary>
    /// List of all pages, including generated.
    /// </summary>
    public ConcurrentBag<Frontmatter> Pages { get; set; } = new();

    /// <summary>
    /// The frontmatter of the home page;
    /// </summary>
    public Frontmatter? HomePage { get; set; }

    /// <summary>
    /// List of pages from the content folder.
    /// </summary>
    public ConcurrentBag<Frontmatter> RegularPages { get; set; } = new();

    /// <summary>
    /// List of all content to be scanned and processed.
    /// </summary>
    public List<(string filePath, string content)> RawPages { get; set; } = new();
}

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SuCoS.Models;

/// <summary>
/// The main configuration of the program, primarily extracted from the app.yaml file.
/// </summary>
public class Site : IParams
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
    /// The appearance of a URL is either ugly or pretty.
    /// </summary>
    public bool UglyURLs { get; set; } = false;

    /// <summary>
    /// The path of the content, based on the source path.
    /// </summary>
    public string SourceContentPath => Path.Combine(SourcePath, "content");

    /// <summary>
    /// The path of the static content (that will be copied as is), based on the source path.
    /// </summary>
    public string SourceStaticPath => Path.Combine(SourcePath, "static");

    /// <summary>
    /// The path of the static content (that will be copied as is), based on the source path.
    /// </summary>
    public string SourceThemePath => Path.Combine(SourcePath, "theme");

    /// <summary>
    /// The path where the generated site files will be saved.
    /// </summary>
    public string OutputPath { get; set; } = "./";

    private List<Frontmatter>? pagesCache;

    /// <summary>
    /// List of all pages, including generated.
    /// </summary>
    public List<Frontmatter> Pages
    {
        get
        {
            pagesCache ??= PagesDict.Values.ToList();
            return pagesCache!;
        }
    }

    /// <summary>
    /// Expose a page getter to templates.
    /// </summary>
    /// <param name="permalink"></param>
    /// <returns></returns>
    public Frontmatter? GetPage(string permalink)
    {
        return PagesDict.TryGetValue(permalink, out var page) ? page : null;
    }

    /// <summary>
    /// List of all pages, including generated, by their permalink.
    /// </summary>
    public Dictionary<string, Frontmatter> PagesDict { get; } = new();

    private List<Frontmatter>? regularPagesCache;

    /// <summary>
    /// List of pages from the content folder.
    /// </summary>
    public List<Frontmatter> RegularPages
    {
        get
        {
            regularPagesCache ??= PagesDict
                    .Where(pair => pair.Value.Kind == Kind.single)
                    .Select(pair => pair.Value)
                    .ToList();
            return regularPagesCache;
        }
    }

    /// <summary>
    /// The frontmatter of the home page;
    /// </summary>
    public Frontmatter? HomePage { get; set; }

    /// <summary>
    /// List of all content to be scanned and processed.
    /// </summary>
    public List<(string filePath, string content)> RawPages { get; set; } = new();

    #region IParams

    /// <inheritdoc/>
    public Dictionary<string, object> Params { get; set; } = new();

    #endregion IParams
}

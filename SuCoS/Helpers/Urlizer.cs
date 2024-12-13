using System.Globalization;
using System.Text.RegularExpressions;

namespace SuCoS.Helpers;

/// <summary>
/// Helper class to convert a string to a URL-friendly string.
/// </summary>
public static partial class Urlizer
{
    [GeneratedRegex("[^a-zA-Z0-9]+")]
    private static partial Regex UrlizeRegexAlpha();

    [GeneratedRegex("[^a-zA-Z0-9.]+")]
    private static partial Regex UrlizeRegexAlphaDot();

    /// <summary>
    /// Converts a string to a URL-friendly string.
    /// It will remove all non-alphanumeric characters and replace spaces with the replacement character.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string Urlize(string? title, UrlizerOptions? options = null)
    {
        title ??= string.Empty;
        options ??= new UrlizerOptions(); // Use default options if not provided

        var cleanedTitle = !options.LowerCase ? title : title.ToLower(CultureInfo.CurrentCulture);

        var replacementChar = options.ReplacementChar ?? '\0';
        var replacementCharString = options.ReplacementChar.ToString() ?? string.Empty;

        // Remove non-alphanumeric characters and replace spaces with the replacement character
        cleanedTitle = (options.ReplaceDot ? UrlizeRegexAlpha() : UrlizeRegexAlphaDot())
            .Replace(cleanedTitle, replacementCharString)
            .Trim(replacementChar);

        return cleanedTitle;
    }

    /// <summary>
    /// Converts a path to a URL-friendly string.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static string UrlizePath(string? path, UrlizerOptions? options = null)
    {
        path ??= string.Empty;
        var items = path.Split("/");
        var result = new List<string>();
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item))
            {
                result.Add(Urlize(item, options));
            }
        }
        return (path.StartsWith('/') ? '/' : string.Empty) + string.Join('/', result);
    }

    /// <summary>
    /// Convert all paths to a unix path style
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string UnixPath(string? path) => (path ?? string.Empty).Replace('\\', '/');
}

/// <summary>
/// Options for the <see cref="Urlizer"/> class.
/// Basically to force lowercase and to change the replacement character.
/// </summary>
public class UrlizerOptions
{
    /// <summary>
    /// Force to generate lowercase URLs.
    /// </summary>
    public bool LowerCase { get; init; } = true;

    /// <summary>
    /// The character that will be used to replace spaces and other invalid characters.
    /// </summary>
    public char? ReplacementChar { get; init; } = '-';

    /// <summary>
    /// Replace dots with the replacement character.
    /// Note that it will break file paths and domain names.
    /// </summary>
    public bool ReplaceDot { get; init; }
}

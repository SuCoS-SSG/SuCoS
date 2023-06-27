using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SuCoS;

/// <summary>
/// Helper class to convert a string to a URL-friendly string.
/// </summary>
public static partial class Urlizer
{
    [GeneratedRegex(@"[^a-z0-9.]")]
    private static partial Regex UrlizeRegex();

    /// <summary>
    /// Converts a string to a URL-friendly string.
    /// It will remove all non-alphanumeric characters and replace spaces with the replacement character.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string Urlize(string title, UrlizerOptions? options = null)
    {
        if (title == null)
        {
            throw new ArgumentNullException(nameof(title));
        }

        options ??= new UrlizerOptions(); // Use default options if not provided

        var cleanedTitle = title;

        // Apply culture-specific case conversion if enabled
        if (options.LowerCase)
        {
            cleanedTitle = cleanedTitle.ToLower(CultureInfo.CurrentCulture);
        }

        // Remove non-alphanumeric characters and replace spaces with the replacement character
        cleanedTitle = UrlizeRegex()
            .Replace(cleanedTitle, options.ReplacementChar.ToString())
            .Trim(options.ReplacementChar);

        return cleanedTitle;
    }

    /// <summary>
    /// Converts a path to a URL-friendly string.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static string UrlizePath(string path, UrlizerOptions? options = null)
    {
        var items = (path ?? string.Empty).Split("/");
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = Urlize(items[i], options);
        }
        return string.Join("/", items);
    }
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
    public bool LowerCase { get; set; } = true;

    /// <summary>
    /// The character that will be used to replace spaces and other invalid characters.
    /// </summary>
    public char ReplacementChar { get; set; } = '-';
}

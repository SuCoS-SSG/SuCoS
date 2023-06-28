using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SuCoS.Helper;

/// <summary>
/// Helper class to convert a string to a URL-friendly string.
/// </summary>
public static partial class Urlizer
{
    [GeneratedRegex(@"[^a-zA-Z0-9]+")]
    private static partial Regex UrlizeRegexAlpha();
    [GeneratedRegex(@"[^a-zA-Z0-9.]+")]
    private static partial Regex UrlizeRegexAlphaDot();

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
        title ??= "";

        options ??= new UrlizerOptions(); // Use default options if not provided

        var cleanedTitle = !options.LowerCase ? title : title.ToLower(CultureInfo.CurrentCulture);

        var replacementChar = options.ReplacementChar ?? '\0';
        var replacementCharString = options.ReplacementChar.ToString() ?? "";

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
    public static string UrlizePath(string path, UrlizerOptions? options = null)
    {
        var pathString = (path ?? string.Empty);
        var items = pathString.Split("/");
        var result = new List<string>();
        for (var i = 0; i < items.Length; i++)
        {
            if (!string.IsNullOrEmpty(items[i]))
            {
                result.Add(Urlize(items[i], options));
            }
        }
        return (pathString.StartsWith('/') ? '/' : string.Empty) + string.Join('/', result);
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
    public char? ReplacementChar { get; set; } = '-';

    /// <summary>
    /// Replace dots with the replacement character.
    /// Note that it will break file paths and domain names.
    /// </summary>
    public bool ReplaceDot { get; set; }
}

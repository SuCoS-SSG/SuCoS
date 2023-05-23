using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SuCoS;

/// <summary>
/// Helper class to convert a string to a URL-friendly string.
/// </summary>
public static class Urlizer
{
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
            throw new ArgumentNullException(nameof(title));

        options ??= new UrlizerOptions(); // Use default options if not provided

        var cleanedTitle = title;

        // Apply culture-specific case conversion if enabled
        if (options.LowerCase)
            cleanedTitle = cleanedTitle.ToLower(CultureInfo.CurrentCulture);

        // Remove non-alphanumeric characters and replace spaces with the replacement character
        cleanedTitle = Regex.Replace(cleanedTitle, @"[^a-z0-9\s]", options.ReplacementChar.ToString());

        // Replace consecutive spaces and the replacement character with a single instance of the replacement character
        cleanedTitle = Regex.Replace(cleanedTitle, $@"[\s{Regex.Escape(options.ReplacementChar.ToString())}]+", options.ReplacementChar.ToString());

        // Trim leading and trailing replacement characters
        cleanedTitle = cleanedTitle.Trim(options.ReplacementChar);

        return cleanedTitle;
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

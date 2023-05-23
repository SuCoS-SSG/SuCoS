using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SuCoS;

public static class Urlizer
{
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

public class UrlizerOptions
{
    public bool LowerCase { get; set; } = true;
    public char ReplacementChar { get; set; } = '-';
}

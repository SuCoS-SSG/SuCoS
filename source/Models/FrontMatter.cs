using System.Collections.ObjectModel;
using YamlDotNet.Serialization;

namespace SuCoS.Models;

/// <summary>
/// A scafold structure to help creating system-generated content, like
/// tag, section or index pages
/// </summary>
public class FrontMatter : IFrontMatter
{
    #region IFrontMatter

    /// <inheritdoc/>
    public string? Title { get; init; } = string.Empty;

    /// <inheritdoc/>
    public string? Type { get; set; } = "page";

    /// <inheritdoc/>
    public string? URL { get; init; }

    /// <inheritdoc/>
    public bool? Draft { get; init; }

    /// <inheritdoc/>
    public Collection<string>? Aliases { get; init; }

    /// <inheritdoc/>
    public string? Section { get; set; } = string.Empty;

    /// <inheritdoc/>
    public DateTime? Date { get; init; }

    /// <inheritdoc/>
    public DateTime? LastMod { get; init; }

    /// <inheritdoc/>
    public DateTime? PublishDate { get; init; }

    /// <inheritdoc/>
    public DateTime? ExpiryDate { get; init; }

    /// <inheritdoc/>
    public int Weight { get; init; }

    /// <inheritdoc/>
    public Collection<string>? Tags { get; init; }

    /// <inheritdoc/>
    public Collection<FrontMatterResources>? ResourceDefinitions { get; set; }

    /// <inheritdoc/>
    [YamlIgnore]
    public string RawContent { get; set; } = string.Empty;

    /// <inheritdoc/>
    [YamlIgnore]
    public Kind Kind { get; set; } = Kind.single;

    /// <inheritdoc/>
    [YamlIgnore]
    public string? SourceRelativePath { get; set; }

    /// <inheritdoc/>
    [YamlIgnore]
    public string SourceFullPath { get; set; }

    /// <inheritdoc/>
    [YamlIgnore]
    public string? SourceRelativePathDirectory => (Path.GetDirectoryName(SourceRelativePath) ?? string.Empty)
        .Replace('\\', '/');

    /// <inheritdoc/>
    [YamlIgnore]
    public string? SourceFileNameWithoutExtension => (Path.GetFileNameWithoutExtension(SourceRelativePath) ?? string.Empty)
        .Replace('\\', '/');

    /// <inheritdoc/>
    [YamlIgnore]
    public DateTime? GetPublishDate => PublishDate ?? Date;

    /// <inheritdoc/>
    public Dictionary<string, object> Params { get; set; } = [];

    #endregion IFrontMatter

    /// <summary>
    /// Constructor
    /// </summary>
    public FrontMatter()
    {
        SourceFullPath = string.Empty;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="title"></param>
    /// <param name="sourcePath"></param>
    public FrontMatter(string title, string sourcePath)
    {
        Title = title;
        SourceRelativePath = sourcePath;
        SourceFullPath = sourcePath;
    }
}

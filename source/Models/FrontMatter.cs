using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace SuCoS.Models;

/// <summary>
/// A scafold structure to help creating system-generated content, like
/// tag, section or index pages
/// </summary>
internal class FrontMatter : IFrontMatter
{
    #region IFrontMatter

    /// <inheritdoc/>
    public string? Title { get; init; } = string.Empty;

    /// <inheritdoc/>
    public string? Type { get; set; } = "page";

    /// <inheritdoc/>
    public string? URL { get; init; }

    /// <inheritdoc/>
    public List<string>? Aliases { get; init; }

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
    public int Weight { get; init; } = 0;
    
    /// <inheritdoc/>
    public List<string>? Tags { get; init; }

    /// <inheritdoc/>
    [YamlIgnore]
    public string RawContent { get; set; } = string.Empty;

    /// <inheritdoc/>
    [YamlIgnore]
    public Kind Kind { get; set; } = Kind.single;

    /// <inheritdoc/>
    [YamlIgnore]
    public string? SourcePath { get; set; }

    /// <inheritdoc/>
    [YamlIgnore]
    public string? SourceFileNameWithoutExtension => Path.GetFileNameWithoutExtension(SourcePath);

    /// <inheritdoc/>
    [YamlIgnore]
    public string? SourcePathDirectory => Path.GetDirectoryName(SourcePath);

    /// <inheritdoc/>
    [YamlIgnore]
    public DateTime? GetPublishDate => PublishDate ?? Date;

    /// <inheritdoc/>
    public Dictionary<string, object> Params { get; set; } = new();

    #endregion IFrontMatter

    /// <summary>
    /// Constructor
    /// </summary>
    public FrontMatter() { }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="title"></param>
    /// <param name="sourcePath"></param>
    public FrontMatter(string title, string sourcePath)
    {
        Title = title;
        SourcePath = sourcePath;
    }
}

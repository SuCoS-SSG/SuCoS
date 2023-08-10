using System;
using System.Collections.Generic;
using SuCoS.Models.CommandLineOptions;

namespace SuCoS.Models;

/// <summary>
/// Basic structure needed to generate user content and system content
/// </summary>
public interface IFrontMatter : IParams, IFile
{
    /// <summary>
    /// The content Title.
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// The first directory where the content is located, inside content.
    /// </summary>
    /// 
    /// <example>
    /// If the content is located at <c>content/blog/2021-01-01-Hello-World.md</c>, 
    /// then the value of this property will be <c>blog</c>.
    /// </example>
    string? Section { get; }

    /// <summary>
    /// The type of content. It's the will be "page", if not specified.
    /// </summary>
    string? Type { get; }

    /// <summary>
    /// The URL pattern to be used to create the url.
    /// Liquid template can be used to use tokens.
    /// </summary>
    ///
    /// <example>
    /// <code>
    /// URL: my-page
    /// </code>
    /// will be converted to <code>/my-page</code>, independetly of the page title.
    /// </example>
    /// <example>
    /// <code>
    /// URL: "{{ page.Parent.Title }}/{{ page.Title }}"
    /// </code>
    /// will try to convert <code>page.Parent.Title</code> and <code>page.Title</code>.
    /// </example>
    string? URL { get; }

    /// <summary>
    /// True for draft content. It will not be rendered unless 
    /// a option <see cref="IGenerateOptions.Draft"/> is set to <c>true</c>.
    /// </summary>
    bool? Draft { get; }

    /// <summary>
    /// Date of the post. Will be used as the <see cref="PublishDate"/> if it's not set.
    /// Unless the option <see cref="IGenerateOptions.Future"/> is set to <c>true</c>,
    /// the dates set from the future will be ignored.
    /// </summary>
    DateTime? Date { get; }

    /// <summary>
    /// Last modification date of the page.
    /// Useful to notify users that the content was updated.
    /// </summary>
    DateTime? LastMod { get; }

    /// <summary>
    /// Publish date of the page. If not set, the <see cref="Date"/> will be used instead.
    /// Unless the option <see cref="IGenerateOptions.Future"/> is set to <c>true</c>,
    /// the dates set from the future will be ignored.
    /// </summary>
    DateTime? PublishDate { get; }

    /// <summary>
    /// Expiry date of the page.
    /// </summary>
    DateTime? ExpiryDate { get; }

    /// <summary>
    /// A List of secondary URL patterns to be used to create the url.
    /// List URL, it will be parsed as liquid templates, so you can use page variables.
    /// </summary>
    /// <see cref="URL"/>
    List<string>? Aliases { get; }

    /// <summary>
    /// Page weight. Used for sorting by default.
    /// </summary>
    int Weight { get; }

    /// <summary>
    /// A list of tags, if any.
    /// </summary>
    List<string>? Tags { get; }

    /// <summary>
    /// List of resource definitions.
    /// </summary>
    List<FrontMatterResources>? ResourceDefinitions { get; }

    /// <summary>
    /// Raw content from the Markdown file, bellow the front matter.
    /// </summary>
    string RawContent { get; }

    /// <summary>
    /// The kind of the page, if it's a single page, a list of pages or the home page.
    /// It's used to determine the proper theme file.
    /// </summary>
    Kind Kind { get; }

    /// <summary>
    /// The date to be considered as the publish date.
    /// </summary>
    DateTime? GetPublishDate => PublishDate ?? Date;
}

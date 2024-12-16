namespace SuCoS.Models;

/// <summary>
/// Page resources. All files that accompany a page.
/// </summary>
public interface IResource : IFile, IOutput, IParams
{
    /// <summary>
    /// Resource name.
    /// </summary>
    string? Title { get; set; }

    /// <summary>
    /// Resource file name.
    /// </summary>
    string FileName => Path.GetFileName(SourceRelativePath);
}

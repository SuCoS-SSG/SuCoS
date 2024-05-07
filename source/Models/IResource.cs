namespace SuCoS.Models;

/// <summary>
/// Page resources. All files that accompany a page.
/// </summary>
public interface IResource : IFile, IOutput, IParams
{
    /// <summary>
    /// Resource name.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Resource file name.
    /// </summary>
    public string? FileName { get; set; }
}

namespace SuCoS.Models;

/// <summary>
/// Page resources. All files that accompany a page.
/// </summary>
public interface IResource : IFile, IOutput, IParams
{
    /// <inheritdoc/>
    public string? Title { get; set; }

    /// <inheritdoc/>
    public string? FileName { get; set; }
}

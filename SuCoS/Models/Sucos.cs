using System.Reflection;

namespace SuCoS.Models;

/// <summary>
/// SuCoS internals
/// </summary>
public class Sucos
{
    /// <summary>
    /// Return true if the `SuCoS serve` is running.
    /// </summary>
    public bool IsServer { get; set; }

    /// <summary>
    /// The .NET version.
    /// </summary>
    public Version DotNetVersion => Environment.Version;

    /// <summary>
    /// The SuCoS version.
    /// </summary>
    public Version? Version =>
        Assembly.GetExecutingAssembly().GetName().Version;

    /// <summary>
    /// The date and time that the app was compiled.
    /// </summary>
    public DateTime BuildDate => SucosExt.BuildDate();
}

/// <summary>
/// (Code generated from SuCoSGenerator.cs) Build/compilation metadata.
/// </summary>
public static partial class SucosExt
{
    /// <summary>
    /// Date and time in UTC.
    /// </summary>
    public static partial DateTime BuildDate();

    /// <summary>
    /// Date and time (expressed as Ticks) in UTC.
    /// </summary>
    public static partial long BuildDateTicks();
}

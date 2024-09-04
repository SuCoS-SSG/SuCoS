using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SuCoS.Generator;

/// <summary>
/// Generate some data at the time of compilation
/// </summary>
[Generator]
public class SuCosGenerator : ISourceGenerator
{
    /// <inheritdoc/>
    public void Initialize(GeneratorInitializationContext context) { }

    /// <inheritdoc/>
    public void Execute(GeneratorExecutionContext context)
    {
        var source = $@"
namespace SuCoS.Models;

/// <summary>
/// (Code generated from SuCoSGenerator.cs) Build/compilation metadata.
/// </summary>
public static class SucosExt
{{
    /// <summary>
    /// Date and time in UTC.
    /// </summary>
    public static DateTime BuildDate => new DateTime({DateTime.UtcNow.Ticks}, DateTimeKind.Utc);

    /// <summary>
    /// Date and time (expressed as Ticks) in UTC.
    /// </summary>
    public static long BuildDateTicks => {DateTime.UtcNow.Ticks};
}}
";
        context.AddSource("SucosExt.g.cs", SourceText.From(source, Encoding.UTF8));
    }
}

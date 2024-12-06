using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SuCoS.Generator;

[Generator]
public class SuCosGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            var source = $@"
namespace SuCoS.Models;

/// <summary>
/// (Code generated from SuCoSGenerator.cs) Build/compilation metadata.
/// </summary>
public static partial class SucosExt
{{
    /// <summary>
    /// Date and time in UTC.
    /// </summary>
    public static partial DateTime BuildDate() => new DateTime({DateTime.UtcNow.Ticks}, DateTimeKind.Utc);

    /// <summary>
    /// Date and time (expressed as Ticks) in UTC.
    /// </summary>
    public static partial long BuildDateTicks() => {DateTime.UtcNow.Ticks};
}}
";
            ctx.AddSource("SucosExt.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }
}

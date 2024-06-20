using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SuCoS.Models;

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

public class SucosExt
{{
    public static DateTime BuildDate => new DateTime({DateTime.UtcNow.Ticks}, DateTimeKind.Utc);

    public static long BuildDateTicks => {DateTime.UtcNow.Ticks};
}}
";
        context.AddSource("SucosExt.g.cs", SourceText.From(source, Encoding.UTF8));
    }
}

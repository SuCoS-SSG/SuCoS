using Nuke.Common.Tooling;
using System.ComponentModel;

namespace SuCoS;

[TypeConverter(typeof(TypeConverter<Configuration>))]
public class Configuration : Enumeration
{
    public static Configuration Debug { get; set; } = new() { Value = nameof(Debug) };
    public static Configuration Release { get; set; } = new() { Value = nameof(Release) };

    public static implicit operator string(Configuration configuration) => configuration?.Value;
}

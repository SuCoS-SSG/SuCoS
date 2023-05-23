using System.IO;
using RazorLight;

namespace SuCoS;

public class AppConfig
{
    public string Title { get; set; } = "HOHOOH";
    public string BaseUrl { get; set; } = "./";
    public string SourcePath { get; set; } = "./";
    public string OutputPath { get; set; } = "./";
    public string SourceContentPath => Path.Combine(SourcePath, "content");
    public IRazorLightEngine? RazorEngine { get; set; }
}

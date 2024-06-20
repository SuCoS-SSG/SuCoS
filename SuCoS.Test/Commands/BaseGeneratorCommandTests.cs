using System.Reflection;
using NSubstitute;
using Serilog;
using SuCoS.Commands;
using SuCoS.Helpers;
using SuCoS.Models.CommandLineOptions;
using SuCoS.TemplateEngine;
using Xunit;

namespace SuCoS.Test.Commands;

public class BaseGeneratorCommandTests
{
    private static readonly IGenerateOptions TestOptions = new GenerateOptions
    {
        SourceArgument = "test_source"
    };

    private static readonly ILogger TestLogger = new LoggerConfiguration().CreateLogger();

    private class BaseGeneratorCommandStub(IGenerateOptions options, ILogger logger, IFileSystem fs)
        : BaseGeneratorCommand(options, logger, fs);

    private readonly IFileSystem _fs = Substitute.For<IFileSystem>();

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new BaseGeneratorCommandStub(null!, TestLogger, _fs));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new BaseGeneratorCommandStub(TestOptions, null!, _fs));
    }

    [Fact]
    public void CheckValueInDictionary_ShouldWorkCorrectly()
    {
        var type = typeof(FluidTemplateEngine);
        var method = type.GetMethod("CheckValueInDictionary", BindingFlags.NonPublic | BindingFlags.Static);
        var parameters = new object[] { new[] { "key" }, new Dictionary<string, object> { { "key", "value" } }, "value" };

        Assert.NotNull(method);
        var result = method.Invoke(null, parameters);

        Assert.NotNull(result);
        Assert.True((bool)result);
    }
}

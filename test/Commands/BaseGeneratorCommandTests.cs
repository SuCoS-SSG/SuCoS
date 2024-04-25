using NSubstitute;
using Serilog;
using SuCoS;
using SuCoS.Models.CommandLineOptions;
using SuCoS.TemplateEngine;
using System.Reflection;
using Xunit;

namespace Tests.Commands;

public class BaseGeneratorCommandTests
{
    private static readonly IGenerateOptions testOptions = new GenerateOptions
    {
        SourceArgument = "test_source"
    };

    private static readonly ILogger testLogger = new LoggerConfiguration().CreateLogger();

    private class BaseGeneratorCommandStub(IGenerateOptions options, ILogger logger, IFileSystem fs)
        : BaseGeneratorCommand(options, logger, fs);

    readonly IFileSystem fs;

    public BaseGeneratorCommandTests()
    {
        fs = Substitute.For<IFileSystem>();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new BaseGeneratorCommandStub(null!, testLogger, fs));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new BaseGeneratorCommandStub(testOptions, null!, fs));
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

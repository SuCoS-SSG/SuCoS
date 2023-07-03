using System.Reflection;
using Serilog;
using SuCoS;
using Xunit;

namespace Test;

public class BaseGeneratorCommandTests
{
    private static readonly IGenerateOptions testOptions = new BuildOptions
    {
        Source = "test_source"
    };

    private static readonly ILogger testLogger = new LoggerConfiguration().CreateLogger();

    private class BaseGeneratorCommandStub : BaseGeneratorCommand
    {
        public BaseGeneratorCommandStub(IGenerateOptions options, ILogger logger)
            : base(options, logger) { }
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new BaseGeneratorCommandStub(null!, testLogger));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new BaseGeneratorCommandStub(testOptions, null!));
    }

    [Fact]
    public void CheckValueInDictionary_ShouldWorkCorrectly()
    {
        var type = typeof(BaseGeneratorCommand);
        var method = type.GetMethod("CheckValueInDictionary", BindingFlags.NonPublic | BindingFlags.Static);
        var parameters = new object[] { new[] { "key" }, new Dictionary<string, object> { { "key", "value" } }, "value" };

        Assert.NotNull(method);
        var result = method.Invoke(null, parameters);

        Assert.NotNull(result);
        Assert.True((bool)result);
    }
}

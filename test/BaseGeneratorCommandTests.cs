using System.Reflection;
using Fluid;
using Fluid.Values;
using Serilog;
using Xunit;

namespace SuCoS.Tests;

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
    public async Task WhereParamsFilter_ShouldThrowArgumentNullException_WhenInputIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => BaseGeneratorCommand.WhereParamsFilter(null!, new FilterArguments(), new TemplateContext()).AsTask());
    }

    [Fact]
    public async Task WhereParamsFilter_ShouldThrowArgumentNullException_WhenArgumentsIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => BaseGeneratorCommand.WhereParamsFilter(new ArrayValue(new FluidValue[0]), null!, new TemplateContext()).AsTask());
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
        Assert.True((bool)result!);
    }
}

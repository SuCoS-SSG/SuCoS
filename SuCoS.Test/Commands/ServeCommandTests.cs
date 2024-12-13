using System.Net;
using NSubstitute;
using Serilog;
using SuCoS.Commands;
using SuCoS.Helpers;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;
using Xunit;

namespace test.Commands;

public class ServeCommandTests : TestSetup
{
    private readonly IFileSystem
        _mockFileSystem = Substitute.For<IFileSystem>();

    private readonly IFileWatcher _mockFileWatcher =
        Substitute.For<IFileWatcher>();

    private readonly IPortSelector _mockPortSelector =
        Substitute.For<IPortSelector>();

    private readonly ServeOptions _serverOptionsMock = new()
    {
        SourceArgument =
            Path.GetFullPath(Path.Combine(TestSitesPath, TestSitePathConst01))
    };

    public ServeCommandTests()
    {
        // Create a mock Site with minimal setup
        Site = new Site(
            GenerateOptionsMock,
            SiteSettingsMock,
            FrontMatterParserMock,
            LoggerMock,
            SystemClockMock);

        // Mock file system methods to prevent exceptions
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);
        _mockFileSystem.FileReadAllText(Arg.Any<string>())
            .Returns("Title: test");
        _mockFileSystem.DirectoryGetDirectories(Arg.Any<string>())
            .Returns([]);
        _mockFileSystem.DirectoryGetFiles(Arg.Any<string>())
            .Returns([]);

        FrontMatterParserMock.Parse<SiteSettings>(Arg.Any<string>())
            .Returns(SiteSettingsMock);

        _mockPortSelector.SelectAvailablePort(ServeCommand.BaseUrlDefault, ServeCommand.PortDefault, ServeCommand.MaxPortTries)
            .Returns(2441);
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        using var serveCommand = new ServeCommand(
            _serverOptionsMock,
            LoggerMock,
            _mockFileWatcher,
            _mockFileSystem,
            _mockPortSelector
        );

        // Assert
        _mockFileWatcher.Received(1).Start(
            Arg.Is<string>(path =>
                path == Path.GetFullPath(_serverOptionsMock.Source)),
            Arg.Any<Action<object, FileSystemEventArgs>>()
        );
    }

    [Fact]
    public void StartServer_ShouldUseSelectedPort()
    {
        // Arrange
        const int expectedPort = 1234;
        _mockPortSelector
            .SelectAvailablePort(Arg.Any<string>(), Arg.Any<int>(),
                Arg.Any<int>())
            .Returns(expectedPort);

        using var serveCommand = CreateServeCommand();

        // Act
        serveCommand.StartServer();

        // Assert
        _mockPortSelector.Received(1).SelectAvailablePort(
            Arg.Is(ServeCommand.BaseUrlDefault),
            Arg.Is(ServeCommand.PortDefault),
            Arg.Is(ServeCommand.MaxPortTries)
        );
        Assert.Equal(expectedPort, serveCommand.PortUsed);
    }

    [Fact]
    public void Dispose_ShouldCleanupResources()
    {
        // Arrange
        var serveCommand = CreateServeCommand();

        // Act
        serveCommand.StartServer();
        serveCommand.Dispose();

        // Assert
        _mockFileWatcher.Received(1).Stop();
    }

    private ServeCommand CreateServeCommand()
    {
        // Helper method to create a ServeCommand with mocked dependencies
        return new ServeCommand(
            _serverOptionsMock,
            LoggerMock,
            _mockFileWatcher,
            _mockFileSystem,
            _mockPortSelector
        );
    }
}

public class DefaultPortSelectorTests
{
    private readonly ILogger _mockLogger = Substitute.For<ILogger>();

    [Fact]
    public void SelectAvailablePort_WhenInitialPortAvailable_ReturnsSamePort()
    {
        // Arrange
        const int initialPort = 5000;
        var portSelector = new DefaultPortSelector(_mockLogger);

        // Act & Assert
        var selectedPort =
            portSelector.SelectAvailablePort("http://localhost", initialPort,
                10);
        Assert.Equal(initialPort, selectedPort);
    }

    [Fact]
    public void
        SelectAvailablePort_WhenInitialPortInUse_ReturnsNextAvailablePort()
    {
        // Arrange
        const int initialPort = ServeCommand.PortDefault;
        var portSelector = new DefaultPortSelector(_mockLogger);

        // Create a listener to block the initial port
        using var blockingListener = new HttpListener();
        blockingListener.Prefixes.Add($"http://localhost:{initialPort}/");
        blockingListener.Start();

        try
        {
            // Act
            var selectedPort =
                portSelector.SelectAvailablePort("http://localhost",
                    initialPort, 10);

            // Assert
            Assert.NotEqual(initialPort, selectedPort);
            Assert.True(selectedPort > initialPort);
        }
        finally
        {
            blockingListener.Stop();
            blockingListener.Close();
        }
    }

    [Fact]
    public void SelectAvailablePort_WhenNoPortAvailable_ThrowsException()
    {
        // Arrange
        const int initialPort = ServeCommand.PortDefault;
        var portSelector = new DefaultPortSelector(_mockLogger);

        // Block multiple sequential ports
        var blockingListeners = new List<HttpListener>();
        for (var i = 0; i < 10; i++)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{initialPort + i}/");
            listener.Start();
            blockingListeners.Add(listener);
        }

        try
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                portSelector.SelectAvailablePort("http://localhost",
                    initialPort, 10)
            );
        }
        finally
        {
            // Cleanup listeners
            foreach (var listener in blockingListeners)
            {
                listener.Stop();
                listener.Close();
            }
        }
    }
}

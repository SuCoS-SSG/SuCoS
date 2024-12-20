using NSubstitute;
using Serilog;
using SuCoS.Commands;
using SuCoS.Helpers;
using SuCoS.Models.CommandLineOptions;
using Xunit;

namespace test.Commands;

public class BuildCommandTests
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly BuildOptions _options;

    public BuildCommandTests()
    {
        _logger = Substitute.For<ILogger>();
        _fileSystem = Substitute.For<IFileSystem>();
        _fileSystem.FileExists("./sucos.yaml").Returns(true);
        _fileSystem.FileReadAllText("./sucos.yaml").Returns("""
Title: test
""");
        _options = new BuildOptions { Output = "test" };
    }

    [Fact]
    public void Constructor_ShouldNotThrowException_WhenParametersAreValid()
    {
        // Act
        var result = new BuildCommand(_options, _logger, _fileSystem);

        // Assert
        Assert.IsType<BuildCommand>(result);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => new BuildCommand(null!, _logger, _fileSystem));
    }

    [Fact]
    public void Run()
    {
        // Act
        var command = new BuildCommand(_options, _logger, _fileSystem);
        var result = command.Run();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CopyFolder_ShouldCallCreateDirectory_WhenSourceFolderExists()
    {
        // Arrange
        _fileSystem.DirectoryExists("sourceFolder").Returns(true);
        var buildCommand = new BuildCommand(_options, _logger, _fileSystem);

        // Act
        buildCommand.CopyFolder("sourceFolder", "outputFolder");

        // Assert
        _fileSystem.Received(1).DirectoryCreateDirectory("outputFolder");
    }

    [Fact]
    public void CopyFolder_ShouldNotCallCreateDirectory_WhenSourceFolderDoesNotExist()
    {
        // Arrange
        _fileSystem.DirectoryExists("sourceFolder").Returns(false);
        var buildCommand = new BuildCommand(_options, _logger, _fileSystem);

        // Act
        buildCommand.CopyFolder("sourceFolder", "outputFolder");

        // Assert
        _fileSystem.DidNotReceive().DirectoryCreateDirectory(Arg.Any<string>());
    }
}

using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;
using Xunit;
using NSubstitute;
using Serilog;
using SuCoS.Commands;
using SuCoS.Helpers;

namespace Tests.Commands;

public class NewSiteCommandTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly ISite _site = Substitute.For<ISite>();

    [Fact]
    public void Create_ShouldReturnNewSiteCommand_WhenParametersAreValid()
    {
        // Arrange
        var options = new NewSiteOptions { Output = "test", Title = "Test", Description = "Test", BaseUrl = "https://test.com" };

        // Act
        var result = NewSiteCommand.Create(options, _logger, _fileSystem);

        // Assert
        Assert.IsType<NewSiteCommand>(result);
    }

    [Fact]
    public void Create_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(testCode: () => NewSiteCommand.Create(null!, _logger, _fileSystem));
    }

    [Fact]
    public void Create_ShouldNotReturnNull_WhenParametersAreValid()
    {
        // Arrange
        var options = new NewSiteOptions { Output = "test", Title = "Test", Description = "Test", BaseUrl = "https://test.com" };

        // Act
        var result = NewSiteCommand.Create(options, _logger, _fileSystem);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Run_ShouldLogInformation_WhenCreatingNewSite()
    {
        // Arrange
        var options = new NewSiteOptions { Output = "test", Title = "Test", Description = "Test", BaseUrl = "https://test.com", Force = false };
        _fileSystem.FileExists(Arg.Any<string>()).Returns(false);

        var newSiteCommand = NewSiteCommand.Create(options, _logger, _fileSystem);

        // Act
        newSiteCommand.Run();

        // Assert
        _logger.Received(1).Information("Creating a new site: {title} at {outputPath}", options.Title, Arg.Any<string>());
    }

    [Fact]
    public void Run_ShouldCallCreateDirectoryWithCorrectPaths_ForEachFolder()
    {
        // Arrange
        var options = new NewSiteOptions { Output = "test", Title = "Test", Description = "Test", BaseUrl = "https://test.com", Force = false };
        _site.SourceFolders.Returns(["folder1", "folder2"]);
        _fileSystem.FileExists(Arg.Any<string>()).Returns(false);

        var newSiteCommand = new NewSiteCommand(options, _logger, _fileSystem, _site);

        // Act
        newSiteCommand.Run();

        // Assert
        _fileSystem.Received(1).DirectoryCreateDirectory("folder1");
        _fileSystem.Received(1).DirectoryCreateDirectory("folder2");
    }

    [Fact]
    public void Run_ShouldReturn1_WhenCreateDirectoryThrowsException()
    {
        // Arrange
        var options = new NewSiteOptions { Output = "test", Title = "Test", Description = "Test", BaseUrl = "https://test.com", Force = false };
        _site.SourceFolders.Returns(["folder1", "folder2"]);
        _fileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _fileSystem.When(x => x.DirectoryCreateDirectory(Arg.Any<string>()))
            .Do(_ => throw new ArgumentNullException());

        var newSiteCommand = new NewSiteCommand(options, _logger, _fileSystem, _site);

        // Act
        var result = newSiteCommand.Run();

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void Run_ShouldReturn0_WhenDirectoryDoesNotExist()
    {
        // Arrange
        var options = new NewSiteOptions { Output = "test", Force = false };
        _fileSystem.FileExists(Arg.Any<string>()).Returns(false);

        var newSiteCommand = new NewSiteCommand(options, _logger, _fileSystem, _site);

        // Act
        var result = newSiteCommand.Run();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Run_ShouldReturn1_WhenForceIsFalseAndDirectoryExists()
    {
        // Arrange
        var options = new NewSiteOptions { Output = "test", Force = false };
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);

        var newSiteCommand = new NewSiteCommand(options, _logger, _fileSystem, _site);

        // Act
        var result = newSiteCommand.Run();

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void Run_ShouldReturn0_WhenForceIsTrueAndDirectoryExists()
    {
        // Arrange
        var options = new NewSiteOptions { Output = "test", Force = true };
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);

        var newSiteCommand = new NewSiteCommand(options, _logger, _fileSystem, _site);

        // Act
        var result = newSiteCommand.Run();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Run_ShouldReturn1_WhenExportThrowsException()
    {
        // Arrange
        var options = new NewSiteOptions { Output = "test", Force = true };
        _fileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _site.Parser
            .When(x => x.Export(Arg.Any<SiteSettings>(), Arg.Any<string>()))
            .Do(_ => throw new ArgumentNullException());

        var newSiteCommand = new NewSiteCommand(options, _logger, _fileSystem, _site);

        // Act
        var result = newSiteCommand.Run();

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void Run_ShouldCallCreateDirectory_ForEachFolder()
    {
        // Arrange
        var options = new NewSiteOptions { Output = "test", Force = false };
        _site.SourceFolders.Returns(["folder1", "folder2"]);
        _fileSystem.FileExists(Arg.Any<string>()).Returns(false);

        var newSiteCommand = new NewSiteCommand(options, _logger, _fileSystem, _site);

        // Act
        newSiteCommand.Run();

        // Assert
        _fileSystem.Received(2).DirectoryCreateDirectory(Arg.Any<string>());
    }
}

using SuCoS;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;
using Xunit;
using NSubstitute;
using Serilog;

namespace Tests.Commands;

public class NewSiteCommandTests
{
    readonly ILogger logger;
    readonly IFileSystem fileSystem;
    readonly ISite site;

    public NewSiteCommandTests()
    {
        logger = Substitute.For<ILogger>();
        fileSystem = Substitute.For<IFileSystem>();
        site = Substitute.For<ISite>();
    }

    [Fact]
    public void Create_ShouldReturnNewSiteCommand_WhenParametersAreValid()
    {
        // Arrange
        var options = new NewSiteOptions { Output = "test", Title = "Test", Description = "Test", BaseURL = "http://test.com" };

        // Act
        var result = NewSiteCommand.Create(options, logger, fileSystem);

        // Assert
        Assert.IsType<NewSiteCommand>(result);
    }

    [Fact]
    public void Create_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(testCode: () => NewSiteCommand.Create(null!, logger, fileSystem));
    }

    [Fact]
    public void Create_ShouldNotReturnNull_WhenParametersAreValid()
    {
        // Arrange
        var options = new NewSiteOptions { Output = "test", Title = "Test", Description = "Test", BaseURL = "http://test.com" };

        // Act
        var result = NewSiteCommand.Create(options, logger, fileSystem);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Run_ShouldLogInformation_WhenCreatingNewSite()
    {
        // Arrange
        var options = new NewSiteOptions { Output = "test", Title = "Test", Description = "Test", BaseURL = "http://test.com", Force = false };
        fileSystem.FileExists(Arg.Any<string>()).Returns(false);

        var newSiteCommand = NewSiteCommand.Create(options, logger, fileSystem);

        // Act
        newSiteCommand.Run();

        // Assert
        logger.Received(1).Information("Creating a new site: {title} at {outputPath}", options.Title, Arg.Any<string>());
    }

    [Fact]
    public void Run_ShouldCallCreateDirectoryWithCorrectPaths_ForEachFolder()
    {
        // Arrange
        var options = new NewSiteOptions { Output = "test", Title = "Test", Description = "Test", BaseURL = "http://test.com", Force = false };
        var site = Substitute.For<ISite>();
        site.SourceFolders.Returns(["folder1", "folder2"]);
        fileSystem.FileExists(Arg.Any<string>()).Returns(false);

        var newSiteCommand = new NewSiteCommand(options, logger, fileSystem, site);

        // Act
        newSiteCommand.Run();

        // Assert
        fileSystem.Received(1).DirectoryCreateDirectory("folder1");
        fileSystem.Received(1).DirectoryCreateDirectory("folder2");
    }

    [Fact]
    public void Run_ShouldReturn1_WhenCreateDirectoryThrowsException()
    {
        // Arrange
        var options = new NewSiteOptions { Output = "test", Title = "Test", Description = "Test", BaseURL = "http://test.com", Force = false };
        var site = Substitute.For<ISite>();
        site.SourceFolders.Returns(["folder1", "folder2"]);
        fileSystem.FileExists(Arg.Any<string>()).Returns(false);
        fileSystem.When(x => x.DirectoryCreateDirectory(Arg.Any<string>()))
            .Do(x => { throw new ArgumentNullException(); });

        var newSiteCommand = new NewSiteCommand(options, logger, fileSystem, site);

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
        fileSystem.FileExists(Arg.Any<string>()).Returns(false);

        var newSiteCommand = new NewSiteCommand(options, logger, fileSystem, site);

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
        fileSystem.FileExists(Arg.Any<string>()).Returns(true);

        var newSiteCommand = new NewSiteCommand(options, logger, fileSystem, site);

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
        fileSystem.FileExists(Arg.Any<string>()).Returns(true);

        var newSiteCommand = new NewSiteCommand(options, logger, fileSystem, site);

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
        fileSystem.FileExists(Arg.Any<string>()).Returns(false);
        site.Parser
            .When(x => x.Export(Arg.Any<SiteSettings>(), Arg.Any<string>()))
            .Do(x => { throw new ArgumentNullException(); });

        var newSiteCommand = new NewSiteCommand(options, logger, fileSystem, site);

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
        site.SourceFolders.Returns(["folder1", "folder2"]);
        fileSystem.FileExists(Arg.Any<string>()).Returns(false);

        var newSiteCommand = new NewSiteCommand(options, logger, fileSystem, site);

        // Act
        newSiteCommand.Run();

        // Assert
        fileSystem.Received(2).DirectoryCreateDirectory(Arg.Any<string>());
    }
}

using SuCoS.Models.CommandLineOptions;
using Xunit;
using NSubstitute;
using Serilog;
using SuCoS;

namespace Tests.Commands;

public class BuildCommandTests
{
    readonly ILogger logger;
    readonly IFileSystem fileSystem;
    readonly BuildOptions options;

    public BuildCommandTests()
    {
        logger = Substitute.For<ILogger>();
        fileSystem = Substitute.For<IFileSystem>();
        fileSystem.FileExists("./sucos.yaml").Returns(true);
        fileSystem.FileReadAllText("./sucos.yaml").Returns("""
Ttile: test
""");
        options = new BuildOptions { Output = "test" };
    }

    [Fact]
    public void Constructor_ShouldNotThrowException_WhenParametersAreValid()
    {
        // Act
        var result = new BuildCommand(options, logger, fileSystem);

        // Assert
        Assert.IsType<BuildCommand>(result);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => new BuildCommand(null!, logger, fileSystem));
    }

    [Fact]
    public void Run()
    {
        // Act
        var command = new BuildCommand(options, logger, fileSystem);
        var result = command.Run();

        // Assert
        Assert.Equal(0, result);
    }

    // [Fact]
    // public void CreateOutputFiles_ShouldCallFileWriteAllText_WhenPageIsValid()
    // {
    //     // Arrange
    //     site.OutputReferences.Returns(new Dictionary<string, IOutput>
    //     {
    //         { "test", Substitute.For<IPage>() }
    //     });
    //     Path.Combine(options.Output, "test").Returns("testPath");

    //     var buildCommand = new BuildCommand(options, logger, fileSystem);

    //     // Act
    //     buildCommand.CreateOutputFiles();

    //     // Assert
    //     fileSystem.Received(1).FileWriteAllText("testPath", Arg.Any<string>());
    // }

    // [Fact]
    // public void CreateOutputFiles_ShouldCallFileCopy_WhenResourceIsValid()
    // {
    //     // Arrange
    //     var resource = Substitute.For<IResource>();
    //     resource.Permalink.Returns("testPermalink");
    //     resource.SourceFullPath.Returns("testSourcePath");
    //     site.OutputReferences.Returns(new Dictionary<string, IOutput>
    //     {
    //         { "test", resource }
    //     });
    //     Path.Combine(options.Output, "testPermalink").Returns("testDestinationPath");
    //     var buildCommand = new BuildCommand(options, logger, fileSystem);

    //     // Act
    //     buildCommand.CreateOutputFiles();

    //     // Assert
    //     fileSystem.Received(1).FileCopy("testSourcePath", "testDestinationPath", overwrite: true);
    // }

    [Fact]
    public void CopyFolder_ShouldCallCreateDirectory_WhenSourceFolderExists()
    {
        // Arrange
        fileSystem.DirectoryExists("sourceFolder").Returns(true);
        var buildCommand = new BuildCommand(options, logger, fileSystem);

        // Act
        buildCommand.CopyFolder("sourceFolder", "outputFolder");

        // Assert
        fileSystem.Received(1).DirectoryCreateDirectory("outputFolder");
    }

    [Fact]
    public void CopyFolder_ShouldNotCallCreateDirectory_WhenSourceFolderDoesNotExist()
    {
        // Arrange
        fileSystem.DirectoryExists("sourceFolder").Returns(false);
        var buildCommand = new BuildCommand(options, logger, fileSystem);

        // Act
        buildCommand.CopyFolder("sourceFolder", "outputFolder");

        // Assert
        fileSystem.DidNotReceive().DirectoryCreateDirectory(Arg.Any<string>());
    }
}

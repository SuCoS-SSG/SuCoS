using CommandLine;

namespace SuCoS.Models.CommandLineOptions;

/// <summary>
/// Command line options for the serve command.
/// </summary>
[Verb("serve", HelpText = "Starts the server")]
public class ServeOptions : GenerateOptions;


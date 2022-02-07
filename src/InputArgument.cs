using CommandLine;

namespace DuplicateFileFinder;

public class InputArgument
{
    [Option("dir", HelpText = "The target folders(can be specified multiple). Default to current folder.",
        Separator = ',')]
    public IEnumerable<string> Dirs { get; set; }

    [Option('r', "recursive", HelpText = "Include sub directories. Default to false.")]
    public bool Recursive { get; set; }

    [Option('v', "verbose", HelpText = "Display detailed logs. Default to false.")]
    public bool Verbose { get; set; }
}
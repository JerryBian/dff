using CommandLine;

namespace DuplicateFileFinder;

public class InputArgument
{
    [Option("dir", HelpText = "The target folder. Default to current folder.", Separator = ',')]
    public IEnumerable<string> Dirs { get; set; }

    [Option('r', "recursive", HelpText = "Include sub directories. Default to false.")]
    public bool Recursive { get; set; }
}
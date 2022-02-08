using CommandLine;

namespace DuplicateFileFinder;

public class InputArgument
{
    [Option('r', "recursive", HelpText = "Include sub directories. Default to false.")]
    public bool Recursive { get; set; }

    [Option('v', "verbose", HelpText = "Display detailed logs. Default to false.")]
    public bool Verbose { get; set; }

    [Option('e', "export", HelpText = "Export all duplicate paths. Default to false.")]
    public bool ExportDuplicatePath { get; set; }

    [Value(0, MetaName = "dir", HelpText = "The target folders(can be specified multiple). Default to current folder.")]
    public IEnumerable<string> Dirs { get; set; }
}
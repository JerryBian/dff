using CommandLine;

namespace DuplicateFileFinder.Model
{
    public class ArgOptions
    {
        [Option('i', "input", Required = false, HelpText = "Set input folder for processing.")]
        public string SourceFolder { get; set; }
    }
}
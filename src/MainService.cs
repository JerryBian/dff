using System.Diagnostics;
using ByteSizeLib;

namespace DuplicateFileFinder;

public class MainService
{
    private const int MaxBytesScan = 1024;
    private readonly AppOptions _options;
    private readonly IOutputHandler _outputHandler;

    public MainService(AppOptions options, IOutputHandler outputHandler)
    {
        _options = options;
        _outputHandler = outputHandler;
    }


    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        _outputHandler.Ingest(new OutputItem("Scanning following folders:"));
        foreach (var dir in _options.Dirs)
        {
            _outputHandler.Ingest(new OutputItem("\u2192 ", false, messageType: MessageType.DarkSuccess));
            _outputHandler.Ingest(new OutputItem($"{dir}", true, messageType: MessageType.Success));
        }

        _outputHandler.Ingest(new OutputItem(""));
        var filesMarkedAsDuplicate = new HashSet<string>();
        foreach (var dir in _options.Dirs)
        {
            // EnumerateFiles API will skip deleted one during iteration, but not for new added file.
            // Usually we don't have the new added case here, so it's safe.
            // However, if the input directory is root we may put duplicates directory to same too.
            // Anyway, we are good here.
            foreach (var file1 in Directory.EnumerateFiles(dir, "*",
                         _options.IncludeSubDirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                if (filesMarkedAsDuplicate.Contains(file1))
                {
                    continue; // This file already marked as duplicate in previous analysis
                }

                var sameFiles = new List<string> {file1}; // always feed file1 as one duplicate temporary
                foreach (var dir2 in _options.Dirs)
                {
                    foreach (var file2 in Directory.EnumerateFiles(dir2, "*",
                                 _options.IncludeSubDirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                    {
                        if (filesMarkedAsDuplicate.Contains(file2))
                        {
                            continue; // This file already marked as duplicate in previous analysis
                        }

                        if (await AreFilesEqualAsync(file1, file2))
                        {
                            sameFiles.Add(file2);
                            filesMarkedAsDuplicate.Add(file2);
                        }
                    }
                }

                if (sameFiles.Count > 1) // We have duplicates
                {
                    _outputHandler.Ingest(new OutputItem("\u2713 ", false, messageType: MessageType.DarkSuccess));
                    _outputHandler.Ingest(
                        new OutputItem("Duplicate Entry: ", false, messageType: MessageType.Warning));
                    _outputHandler.Ingest(new OutputItem(ByteSize.FromBytes(new FileInfo(file1).Length).ToString(),
                        true, messageType: MessageType.Success));
                    foreach (var file in sameFiles)
                    {
                        _outputHandler.Ingest(new OutputItem("\u2192 ", false, messageType: MessageType.DarkSuccess));
                        _outputHandler.Ingest(new OutputItem(file));
                    }

                    _outputHandler.Ingest(new OutputItem(""));
                    filesMarkedAsDuplicate.Add(file1);
                }
            }
        }


        sw.Stop();
        _outputHandler.Ingest(new OutputItem("Done. ", false, messageType: MessageType.DarkSuccess));
        _outputHandler.Ingest(new OutputItem($"Elapsed {sw.Elapsed:hh\\:mm\\:ss}. ", false));
        await Task.CompletedTask;
    }

    public async Task<bool> AreFilesEqualAsync(string filePath1, string filePath2)
    {
        // The path have to be exactly match, as for *nix systems path is case sensitive.
        if (string.Equals(filePath1, filePath2))
        {
            return false;
        }

        var fileInfo1 = new FileInfo(filePath1);
        var fileInfo2 = new FileInfo(filePath2);
        if (fileInfo1.Length != fileInfo2.Length)
        {
            return false;
        }

        var iterations = (int) Math.Ceiling((double) fileInfo1.Length / MaxBytesScan);
        await using var f1 = fileInfo1.OpenRead();
        await using var f2 = fileInfo2.OpenRead();
        var first = new byte[MaxBytesScan];
        var second = new byte[MaxBytesScan];

        for (var i = 0; i < iterations; i++)
        {
            f1.Read(first, 0, MaxBytesScan);
            f2.Read(second, 0, MaxBytesScan);
            if (!AreBytesEqual(first, second))
            {
                return false;
            }
        }

        return true;
    }

    private bool AreBytesEqual(ReadOnlySpan<byte> b1, ReadOnlySpan<byte> b2)
    {
        return b1.SequenceEqual(b2);
    }
}
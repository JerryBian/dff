using System.Diagnostics;
using ByteSizeLib;

namespace DuplicateFileFinder;

public class MainService
{
    private const int MaxBytesScan = 1024 * 2;
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
        var skippedFiles = new HashSet<string>();
        foreach (var dir in _options.Dirs)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            _outputHandler.Ingest(new OutputItem("Scanning folder: ", false, messageType: MessageType.Verbose, discard: !_options.EnableVerboseLog));
            _outputHandler.Ingest(new OutputItem($"{dir}", true, messageType: MessageType.Success, discard: !_options.EnableVerboseLog));
            // EnumerateFiles API will skip deleted one during iteration, but not for new added file.
            // Usually we don't have the new added case here, so it's safe.
            // However, if the input directory is root we may put duplicates directory to same too.
            // Anyway, we are good here.
            foreach (var file1 in Directory.EnumerateFiles(dir, "*",
                         _options.IncludeSubDirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                _outputHandler.Ingest(new OutputItem("", discard: !_options.EnableVerboseLog));
                _outputHandler.Ingest(new OutputItem("Checking file: ", false, messageType: MessageType.Verbose, discard: !_options.EnableVerboseLog));
                _outputHandler.Ingest(new OutputItem(file1, true, messageType: MessageType.Success, discard: !_options.EnableVerboseLog));
                if (skippedFiles.Contains(file1))
                {
                    _outputHandler.Ingest(new OutputItem("Skip.", true, messageType: MessageType.DarkWarning, discard: !_options.EnableVerboseLog));
                    continue; // This file already marked as duplicate in previous analysis
                }

                skippedFiles.Add(file1); // It has been iterated, so no necessary do again
                var duplicateFiles = new List<string> {file1}; // always feed file1 as one duplicate temporary
                foreach (var dir2 in _options.Dirs)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    foreach (var file2 in Directory.EnumerateFiles(dir2, "*",
                                 _options.IncludeSubDirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        _outputHandler.Ingest(new OutputItem("\u2192 ", false, messageType: MessageType.Success, discard: !_options.EnableVerboseLog));
                        _outputHandler.Ingest(new OutputItem(file2, true, messageType: MessageType.Verbose, discard: !_options.EnableVerboseLog));
                        if (skippedFiles.Contains(file2))
                        {
                            _outputHandler.Ingest(new OutputItem("Skip.", true, messageType: MessageType.Verbose, discard: !_options.EnableVerboseLog));
                            _outputHandler.Ingest(new OutputItem("", discard: !_options.EnableVerboseLog));
                            continue; // This file already marked as duplicate in previous analysis
                        }

                        if (await AreFilesEqualAsync(file1, file2))
                        {
                            duplicateFiles.Add(file2);
                            skippedFiles.Add(file2);
                            _outputHandler.Ingest(new OutputItem("Duplicate.", true, messageType: MessageType.DarkSuccess, discard: !_options.EnableVerboseLog));
                        }
                        else
                        {
                            _outputHandler.Ingest(new OutputItem("Non Duplicate.", true, messageType: MessageType.Verbose, discard: !_options.EnableVerboseLog));
                        }
                    }
                }

                if (duplicateFiles.Count > 1) // We have duplicates
                {
                    var delay = _options.EnableVerboseLog;
                    _outputHandler.Ingest(new OutputItem("\u2713 ", false, messageType: MessageType.DarkSuccess, delayToEnd: delay));
                    _outputHandler.Ingest(
                        new OutputItem("Duplicate Entry: ", false, messageType: MessageType.Warning, delayToEnd: delay));
                    _outputHandler.Ingest(new OutputItem(ByteSize.FromBytes(new FileInfo(file1).Length).ToString(),
                        true, messageType: MessageType.Success, delayToEnd: delay));
                    foreach (var file in duplicateFiles)
                    {
                        _outputHandler.Ingest(new OutputItem("\u2192 ", false, messageType: MessageType.DarkSuccess, delayToEnd: delay));
                        _outputHandler.Ingest(new OutputItem(file, delayToEnd: delay));
                    }

                    _outputHandler.Ingest(new OutputItem("", delayToEnd: delay));
                    skippedFiles.Add(file1);
                }
            }
        }

        sw.Stop();

        if (cancellationToken.IsCancellationRequested)
        {
            _outputHandler.Ingest(new OutputItem("User requested to cancel the operations ....", true, messageType: MessageType.DarkWarning, delayToEnd: true));
        }
        _outputHandler.Ingest(new OutputItem("", discard: !_options.EnableVerboseLog));
        _outputHandler.Ingest(new OutputItem("------ Final Results", discard: !_options.EnableVerboseLog));
        await _outputHandler.FlushAsync();
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
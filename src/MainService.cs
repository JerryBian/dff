using System.Diagnostics;
using ByteSizeLib;

namespace DuplicateFileFinder;

public class MainService
{
    private const int MaxBytesScan = 1024 * 2;
    private readonly IDictionary<long, List<string>> _groupedFiles;
    private readonly AppOptions _options;
    private readonly IOutputHandler _outputHandler;

    public MainService(AppOptions options, IOutputHandler outputHandler)
    {
        _options = options;
        _outputHandler = outputHandler;
        _groupedFiles = new Dictionary<long, List<string>>();
    }

    private void Scan(string folder, CancellationToken cancellationToken)
    {
        var items = new Dictionary<string, long>();
        foreach (var file in Directory.EnumerateFiles(folder, "*",
                     _options.IncludeSubDirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            items.Add(file, new FileInfo(file).Length);
        }

        var groupedFiles = items.GroupBy(x => x.Value).Where(x => x.Count() > 1)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Key).ToList());
        foreach (var groupedFile in groupedFiles)
        {
            if (_groupedFiles.ContainsKey(groupedFile.Key))
            {
                foreach (var item in groupedFile.Value)
                {
                    if (_groupedFiles[groupedFile.Key].FirstOrDefault(x => x == item) == null)
                    {
                        _groupedFiles[groupedFile.Key].Add(item);
                    }
                }
            }
            else
            {
                _groupedFiles.Add(groupedFile);
            }
        }
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        _outputHandler.Ingest(new OutputItem("Scanning folders:"));
        foreach (var dir in _options.Dirs)
        {
            _outputHandler.Ingest(new OutputItem("\u2192 ", false, messageType: MessageType.DarkSuccess));
            _outputHandler.Ingest(new OutputItem($"{dir}", true, messageType: MessageType.Success));
        }

        _outputHandler.Ingest(new OutputItem());
        _groupedFiles.Clear();
        foreach (var dir in _options.Dirs)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            Scan(dir, cancellationToken);
        }

        if (_groupedFiles.Any())
        {
            _outputHandler.Ingest(new OutputItem(
                $"Found {_groupedFiles.Count} groups which have exactly same file size.", true,
                messageType: MessageType.Verbose,
                discard: !_options.EnableVerboseLog));
            _outputHandler.Ingest(new OutputItem(discard:!_options.EnableVerboseLog));

            for (var i = 1; i <= _groupedFiles.Keys.Count; i++)
            {
                var fileSize = _groupedFiles.Keys.ElementAt(i-1);
                var groupedFiles = _groupedFiles[fileSize];
                var skippedFiles = new HashSet<string>();
                foreach (var file1 in groupedFiles)
                {
                    if (skippedFiles.Contains(file1))
                    {
                        continue;
                    }

                    var duplicateFiles = new List<string> {file1};
                    skippedFiles.Add(file1);
                    _outputHandler.Ingest(new OutputItem(discard: !_options.EnableVerboseLog));
                    _outputHandler.Ingest(new OutputItem($"[{i}/{_groupedFiles.Keys.Count}] ", false,
                        messageType: MessageType.DarkVerbose,
                        discard: !_options.EnableVerboseLog));
                    _outputHandler.Ingest(new OutputItem(
                        $"Comparing file {file1}({ByteSize.FromBytes(fileSize)}) with:", true,
                        messageType: MessageType.Default,
                        discard: !_options.EnableVerboseLog));
                    foreach (var file2 in groupedFiles)
                    {
                        if (skippedFiles.Contains(file2))
                        {
                            continue;
                        }

                        _outputHandler.Ingest(new OutputItem("\u2192 ", false, messageType: MessageType.DarkSuccess,
                            discard: !_options.EnableVerboseLog));
                        _outputHandler.Ingest(new OutputItem(file2 + " ", false, messageType: MessageType.Verbose,
                            discard: !_options.EnableVerboseLog));
                        if (await AreFilesEqualAsync(file1, file2))
                        {
                            duplicateFiles.Add(file2);
                            skippedFiles.Add(file2);
                            _outputHandler.Ingest(new OutputItem("Duplicate", true,
                                messageType: MessageType.DarkError, discard: !_options.EnableVerboseLog));
                        }
                        else
                        {
                            _outputHandler.Ingest(new OutputItem("Non Duplicate", true,
                                messageType: MessageType.Success, discard: !_options.EnableVerboseLog));
                        }
                    }

                    if (duplicateFiles.Count > 1) // We have duplicates
                    {
                        var delay = _options.EnableVerboseLog;
                        _outputHandler.Ingest(new OutputItem("\u2713 ", false, messageType: MessageType.DarkSuccess,
                            delayToEnd: delay));
                        _outputHandler.Ingest(
                            new OutputItem("Duplicate Files: ", false, messageType: MessageType.Warning,
                                delayToEnd: delay));
                        _outputHandler.Ingest(new OutputItem(ByteSize.FromBytes(fileSize).ToString(),
                            true, messageType: MessageType.Success, delayToEnd: delay));
                        foreach (var file in duplicateFiles)
                        {
                            _outputHandler.Ingest(new OutputItem("\u2192 ", false, messageType: MessageType.DarkSuccess,
                                delayToEnd: delay));
                            _outputHandler.Ingest(new OutputItem(file, delayToEnd: delay));
                        }

                        _outputHandler.Ingest(new OutputItem("", delayToEnd: delay));
                    }
                }
            }
        }
        else
        {
            _outputHandler.Ingest(
                new OutputItem("No duplicate items found.", false, messageType: MessageType.DarkSuccess));
        }

        sw.Stop();

        if (cancellationToken.IsCancellationRequested)
        {
            _outputHandler.Ingest(new OutputItem("User requested to cancel the operations ....", true,
                messageType: MessageType.DarkWarning, delayToEnd: true));
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
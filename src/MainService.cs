using System.Diagnostics;
using System.Text;
using ByteSizeLib;

namespace DuplicateFileFinder;

public class MainService
{
    private readonly List<List<string>> _duplicateItems;
    private readonly IDictionary<long, List<string>> _groupedFiles;
    private readonly int _maxBytesScan;
    private readonly AppOptions _options;
    private readonly IOutputHandler _outputHandler;

    public MainService(AppOptions options, IOutputHandler outputHandler)
    {
        _options = options;
        _outputHandler = outputHandler;
        _groupedFiles = new Dictionary<long, List<string>>();
        _duplicateItems = new List<List<string>>();
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        _maxBytesScan =
            Convert.ToInt32(Math.Min(gcMemoryInfo.TotalAvailableMemoryBytes / 10, 5 * 1024));
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
            _outputHandler.Ingest(new OutputItem(discard: !_options.EnableVerboseLog));

            for (var i = 1; i <= _groupedFiles.Keys.Count; i++)
            {
                var fileSize = _groupedFiles.Keys.ElementAt(i - 1);
                var groupedFiles = _groupedFiles[fileSize];
                var skippedFiles = new HashSet<string>();
                foreach (var file1 in groupedFiles)
                {
                    if (skippedFiles.Contains(file1))
                    {
                        continue;
                    }

                    var fileSizeStr = ByteSize.FromBytes(fileSize).ToString();
                    var duplicateFiles = new List<string> {file1};
                    skippedFiles.Add(file1);
                    _outputHandler.Ingest(new OutputItem(discard: !_options.EnableVerboseLog));
                    _outputHandler.Ingest(new OutputItem($"[{i}/{_groupedFiles.Keys.Count}] ", false,
                        messageType: MessageType.DarkVerbose,
                        discard: !_options.EnableVerboseLog));
                    _outputHandler.Ingest(new OutputItem(
                        $"Comparing file {file1}({fileSizeStr}) with:", true,
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
                        _duplicateItems.Add(duplicateFiles);
                    }
                }
            }
        }
        else
        {
            _outputHandler.Ingest(
                new OutputItem("No duplicate items found.", true, messageType: MessageType.DarkSuccess));
            _outputHandler.Ingest(new OutputItem());
        }

        sw.Stop();

        if (cancellationToken.IsCancellationRequested)
        {
            _outputHandler.Ingest(new OutputItem("User requested to cancel the operations ....", true,
                messageType: MessageType.DarkWarning));
        }

        _outputHandler.Ingest(new OutputItem("", discard: !_options.EnableVerboseLog));
        await ProcessResultsAsync();
        _outputHandler.Ingest(new OutputItem("Done. ", false, messageType: MessageType.DarkSuccess));
        _outputHandler.Ingest(new OutputItem($"Elapsed {sw.Elapsed:hh\\:mm\\:ss}. ", false));
        await Task.CompletedTask;
    }

    private async Task ProcessResultsAsync()
    {
        if (!_duplicateItems.Any())
        {
            return;
        }

        _outputHandler.Ingest(new OutputItem("=== Results ===", messageType: MessageType.Verbose));
        _outputHandler.Ingest(new OutputItem("\u2713 ", false, messageType: MessageType.DarkSuccess));
        var resultFile = _options.ExportDuplicatePath
            ? Path.Combine(_options.OutputDir, $"e_{Path.GetRandomFileName()}")
            : "";

        foreach (var files in _duplicateItems)
        {
            _outputHandler.Ingest(
                new OutputItem($"Duplicate Files{_duplicateItems.Count}: ", false, messageType: MessageType.Warning));
            _outputHandler.Ingest(new OutputItem(ByteSize.FromBytes(new FileInfo(files.First()).Length).ToString(),
                true, messageType: MessageType.Success));
            foreach (var file in files)
            {
                _outputHandler.Ingest(new OutputItem("\u2192 ", false, messageType: MessageType.DarkSuccess));
                _outputHandler.Ingest(new OutputItem(file));
            }

            if (!string.IsNullOrEmpty(resultFile))
            {
                var pathContents = new List<string>(files) {Environment.NewLine};
                await File.AppendAllLinesAsync(resultFile, pathContents, Encoding.UTF8);
            }

            _outputHandler.Ingest(new OutputItem());
        }

        if (!string.IsNullOrEmpty(resultFile))
        {
            _outputHandler.Ingest(new OutputItem("All duplicate files path are saved to: ", false));
            _outputHandler.Ingest(new OutputItem(resultFile, true, messageType: MessageType.DarkSuccess));
            _outputHandler.Ingest(new OutputItem());
        }
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

        var iterations = (int) Math.Ceiling((double) fileInfo1.Length / _maxBytesScan);
        await using var f1 = fileInfo1.OpenRead();
        await using var f2 = fileInfo2.OpenRead();
        var first = new byte[_maxBytesScan];
        var second = new byte[_maxBytesScan];

        for (var i = 0; i < iterations; i++)
        {
            f1.Read(first, 0, _maxBytesScan);
            f2.Read(second, 0, _maxBytesScan);
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
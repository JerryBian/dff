using System.Collections.Concurrent;
using System.Text;

namespace DuplicateFileFinder;

public class OutputHandler : IOutputHandler, IAsyncDisposable
{
    private readonly CancellationToken _cancellationToken;
    private readonly ConcurrentQueue<OutputItem> _items;
    private readonly string _logFilePath;
    private readonly Task _task;

    public OutputHandler(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _task = Task.Run(async () => await ProcessAsync());
        _logFilePath = Path.GetTempFileName();
        _items = new ConcurrentQueue<OutputItem>();
    }

    public async ValueTask DisposeAsync()
    {
        await _task.WaitAsync(CancellationToken.None);
        while (_items.TryDequeue(out var item))
        {
            await ProcessItemAsync(item);
        }

        await Console.Out.WriteAsync($"Logs are save to {_logFilePath}");
    }

    public void Ingest(OutputItem item)
    {
        _items.Enqueue(item);
    }

    private async Task ProcessAsync()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            if (_items.TryDequeue(out var item))
            {
                await ProcessItemAsync(item);
            }
        }
    }

    private async Task ProcessItemAsync(OutputItem item)
    {
        try
        {
            await Task.WhenAll(WriteToConsoleAsync(item), WriteToFileAsync(item));
        }
        catch
        {
            // ignore
        }
    }

    private async Task WriteToFileAsync(OutputItem item)
    {
        var message = item.Message;
        if (item.AppendNewLine)
        {
            message += Environment.NewLine;
            if (!string.IsNullOrEmpty(item.Exception))
            {
                message += item.Exception + Environment.NewLine;
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(item.Exception))
            {
                message += Environment.NewLine + item.Exception + Environment.NewLine;
            }
        }

        await File.AppendAllTextAsync(_logFilePath, message, Encoding.UTF8, CancellationToken.None);
    }

    private async Task WriteToConsoleAsync(OutputItem item)
    {
        var message = item.Message;
        if (item.AppendNewLine)
        {
            message += Environment.NewLine;
        }

        Console.ForegroundColor = item.MessageType switch
        {
            MessageType.DarkError => ConsoleColor.DarkRed,
            MessageType.DarkSuccess => ConsoleColor.DarkGreen,
            MessageType.DarkWarning => ConsoleColor.DarkYellow,
            MessageType.Error => ConsoleColor.Red,
            MessageType.Success => ConsoleColor.Green,
            MessageType.Warning => ConsoleColor.Yellow,
            _ => Console.ForegroundColor
        };

        if (item.IsError)
        {
            await Console.Error.WriteAsync(message);
        }
        else
        {
            await Console.Out.WriteAsync(message);
        }

        if (item.IsError || item.MessageType > MessageType.Default)
        {
            Console.ResetColor();
        }
    }
}
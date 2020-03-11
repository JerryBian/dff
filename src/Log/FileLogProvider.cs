using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace DuplicateFileFinder.Log
{
    public class FileLogProvider : ILogProvider
    {
        private readonly string _logPath;
        private readonly ConcurrentQueue<string> _messages;

        private bool _flushed;

        public FileLogProvider()
        {
            _messages = new ConcurrentQueue<string>();
            var logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "duplicate-file-finder");
            Directory.CreateDirectory(logFolder);
            _logPath = Path.Combine(logFolder, DateTime.Now.ToString("yyyyMMdd.hhmmss.ffffff.lo\\g"));
        }

        public Task InfoAsync(string message)
        {
            _messages.Enqueue($"{DateTime.Now}\t{message}");
            return Task.CompletedTask;
        }

        public Task ErrorAsync(string message, Exception ex)
        {
            _messages.Enqueue($"{DateTime.Now}\t{message}{Environment.NewLine}{ex}");
            return Task.CompletedTask;
        }

        public async Task FlushAsync()
        {
            if (_flushed)
            {
                return;
            }

            await using var sr = new StreamWriter(_logPath);
            while (_messages.TryDequeue(out var message))
            {
                await sr.WriteLineAsync(message);
            }

            await sr.FlushAsync();
            _flushed = true;
        }
    }
}
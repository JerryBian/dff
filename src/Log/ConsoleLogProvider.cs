using System;
using System.Threading.Tasks;

namespace DuplicateFileFinder.Log
{
    public class ConsoleLogProvider : ILogProvider
    {
        public Task InfoAsync(string message)
        {
            Console.WriteLine($"{DateTime.Now}\t{message}");
            return Task.CompletedTask;
        }

        public Task ErrorAsync(string message, Exception ex)
        {
            Console.WriteLine($"{DateTime.Now}\t{message}{Environment.NewLine}{ex.Message}");
            return Task.CompletedTask;
        }

        public Task FlushAsync()
        {
            return Task.CompletedTask;
        }
    }
}
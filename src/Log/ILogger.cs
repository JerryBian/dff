using System;
using System.Threading.Tasks;

namespace DuplicateFileFinder.Log
{
    public interface ILogger
    {
        void RegisterProvider(ILogProvider logProvider);

        Task InfoAsync(string message);

        Task ErrorAsync(string message, Exception ex);

        Task FlushAsync();
    }
}
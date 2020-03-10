using System;
using System.Threading.Tasks;

namespace DuplicateFileFinder.Log
{
    public interface ILogProvider
    {
        Task InfoAsync(string message);

        Task ErrorAsync(string message, Exception ex);

        Task FlushAsync();
    }
}
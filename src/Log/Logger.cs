using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DuplicateFileFinder.Log
{
    public class Logger : ILogger
    {
        private readonly List<ILogProvider> _logProviders;

        public Logger()
        {
            _logProviders = new List<ILogProvider>();
        }

        public void RegisterProvider(ILogProvider logProvider)
        {
            _logProviders.Add(logProvider);
        }

        public async Task InfoAsync(string message)
        {
            await Task.WhenAll(_logProviders.Select(x => x.InfoAsync(message)));
        }

        public async Task ErrorAsync(string message, Exception ex)
        {
            await Task.WhenAll(_logProviders.Select(x => x.ErrorAsync(message, ex)));
        }

        public async Task FlushAsync()
        {
            await Task.WhenAll(_logProviders.Select(x => x.FlushAsync()));
        }
    }
}
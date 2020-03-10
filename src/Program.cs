using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using DuplicateFileFinder.Core;
using DuplicateFileFinder.File;
using DuplicateFileFinder.Log;
using DuplicateFileFinder.Model;

namespace DuplicateFileFinder
{
    internal class Program
    {
        private static Logger _logger;

        private static async Task Main(string[] args)
        {
            _logger = new Logger();
            _logger.RegisterProvider(new ConsoleLogProvider());
            _logger.RegisterProvider(new FileLogProvider());

            AppDomain.CurrentDomain.ProcessExit += OnExit;
            Console.CancelKeyPress += OnExit;
            TaskScheduler.UnobservedTaskException += OnExit;

            var fileManager = new FileManager(_logger);
            var analysisEngine = new AnalysisEngine(_logger, fileManager);

            try
            {
                Parser.Default.ParseArguments<ArgOptions>(args)
                    .WithParsed(async arg =>
                    {
                        var inputFolder = arg.SourceFolder;
                        if (string.IsNullOrEmpty(inputFolder) || string.IsNullOrWhiteSpace(inputFolder))
                        {
                            inputFolder = Environment.CurrentDirectory;
                        }

                        if (!Directory.Exists(inputFolder))
                        {
                            await _logger.ErrorAsync($"Can't find valid director at \"{inputFolder}\".", null);
                            return;
                        }

                        await _logger.InfoAsync("Start processing ...");
                        var stopwatch = Stopwatch.StartNew();
                        await analysisEngine.ExecuteAsync(arg);
                        stopwatch.Stop();
                        await _logger.InfoAsync($"Process completed. It took {stopwatch.ElapsedMilliseconds}ms.");
                    })
                    .WithNotParsed(async e =>
                    {
                        foreach (var error in e)
                        {
                            await _logger.ErrorAsync($"Argument error: {error}", null);
                        }
                    });
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("Unexpected exception.", ex);
            }
            finally
            {
                await _logger.FlushAsync();
            }
        }

        private static async void OnExit(object sender, EventArgs args)
        {
            await _logger.FlushAsync();
        }
    }
}
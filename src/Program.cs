﻿using System.Text;
using CommandLine;

namespace DuplicateFileFinder;

internal class Program
{
    private static readonly CancellationTokenSource Cts = new();

    private static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += OnExit;
        Console.CancelKeyPress += OnExit;
        TaskScheduler.UnobservedTaskException += OnExit;
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        await Parser.Default.ParseArguments<InputArgument>(args).WithParsedAsync(async arg =>
        {
            var outputDir = arg.OutputDir;
            if (string.IsNullOrEmpty(outputDir))
            {
                outputDir = Path.GetTempPath();
            }

            try
            {
                Directory.CreateDirectory(outputDir);
            }
            catch
            {
                await Console.Error.WriteLineAsync($"Failed to find or create output folder: {outputDir}");
                return;
            }

            await using var outputHandler = new OutputHandler(outputDir, Cts.Token);
            try
            {
                var appOptions = GetAppOptions(arg, outputDir);

                var mainService = new MainService(appOptions, outputHandler);
                await mainService.ExecuteAsync(Cts.Token);
            }
            catch (Exception ex)
            {
                outputHandler.Ingest(new OutputItem(ex.Message, true, true, MessageType.Error)
                    {Exception = ex.ToString()});
            }

            Cts.Cancel();
        });
    }

    private static void OnExit(object sender, EventArgs args)
    {
        Cts.Cancel();
    }

    private static AppOptions GetAppOptions(InputArgument o, string outputDir)
    {
        var options = new AppOptions
        {
            IncludeSubDirs = o.Recursive,
            EnableVerboseLog = o.Verbose,
            ExportDuplicatePath = o.ExportDuplicatePath,
            OutputDir = outputDir
        };


        foreach (var dir in o.Dirs)
        {
            if (!Directory.Exists(dir))
            {
                throw new Exception($"Target folder not exists: {dir}");
            }

            options.Dirs.Add(dir);
        }

        if (!options.Dirs.Any())
        {
            options.Dirs.Add(Environment.CurrentDirectory);
        }

        return options;
    }
}
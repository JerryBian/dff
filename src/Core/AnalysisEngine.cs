using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DuplicateFileFinder.File;
using DuplicateFileFinder.Log;
using DuplicateFileFinder.Model;

namespace DuplicateFileFinder.Core
{
    public class AnalysisEngine
    {
        private readonly IFileManager _fileManager;
        private readonly ILogger _logger;

        public AnalysisEngine(ILogger logger, IFileManager fileManager)
        {
            _logger = logger;
            _fileManager = fileManager;
        }

        public int MaxBytesScan { get; set; } = 1024;

        public async Task ExecuteAsync(ArgOptions options)
        {
            var filesMarkedAsDuplicate = new HashSet<string>();

            // EnumerateFiles API will skip deleted one during iteration, but not for new added file.
            // Usually we don't have the new added case here, so it's safe.
            // However, if the input directory is root we may put duplicates directory to same too.
            // Anyway, we are good here.
            foreach (var file1 in Directory.EnumerateFiles(options.InputFolder, "*", SearchOption.AllDirectories))
            {
                if (filesMarkedAsDuplicate.Contains(file1))
                {
                    continue; // This file already marked as duplicate in previous analysis
                }

                var sameFiles = new List<string> {file1}; // always feed file1 as one duplicate temporary
                foreach (var file2 in Directory.EnumerateFiles(options.InputFolder, "*",
                    SearchOption.AllDirectories))
                {
                    if (filesMarkedAsDuplicate.Contains(file2))
                    {
                        continue; // This file already marked as duplicate in previous analysis
                    }

                    if (await AreFilesEqualAsync(file1, file2))
                    {
                        sameFiles.Add(file2);
                        filesMarkedAsDuplicate.Add(file2);
                    }
                }

                if (sameFiles.Count > 1) // We have duplicates
                {
                    _fileManager.ProcessDuplicateFiles(options.InputFolder, sameFiles);
                    filesMarkedAsDuplicate.Add(file1);
                }
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

            var iterations = (int) Math.Ceiling((double) fileInfo1.Length / MaxBytesScan);
            await using (var f1 = fileInfo1.OpenRead())
            {
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
            }

            return true;
        }

        private bool AreBytesEqual(ReadOnlySpan<byte> b1, ReadOnlySpan<byte> b2)
        {
            return b1.SequenceEqual(b2);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DuplicateFileFinder.Log;

namespace DuplicateFileFinder.File
{
    public class FileManager : IFileManager
    {
        private readonly ILogger _logger;

        public FileManager(ILogger logger)
        {
            _logger = logger;
        }

        public void ProcessDuplicateFiles(string inputFolder, List<string> sameFiles)
        {
            var parentFolderPath = inputFolder;
            var parentFolder = Directory.GetParent(inputFolder);
            if (parentFolder != null)
            {
                parentFolderPath = parentFolder.FullName;
            }

            var duplicateSaveTo = Path.Combine(parentFolderPath, $"{Path.GetFileName(inputFolder)}_duplicate");
            Directory.CreateDirectory(duplicateSaveTo);
            var originFile = GetOriginalFile(sameFiles);
            Parallel.ForEach(sameFiles.Where(x => x != originFile), file1 =>
            {
                var targetPath = file1.Replace(inputFolder, duplicateSaveTo);
                var targetParent = Directory.GetParent(targetPath);
                if (targetParent != null)
                {
                    Directory.CreateDirectory(targetParent.FullName);
                }

                System.IO.File.Move(file1, targetPath, true);
                _logger.InfoAsync(
                    $"Moved duplicate file \"{file1}\" to \"{targetPath}\", original file is \"{originFile}\".");
            });
        }

        private string GetOriginalFile(List<string> sameFiles)
        {
            DateTime? minCreationTime = null;
            DateTime? minLastWriteTime = null;
            var fileInfoCache = new Dictionary<string, KeyValuePair<DateTime, DateTime>>();

            foreach (var file in sameFiles)
            {
                var creationTime = System.IO.File.GetCreationTime(file);
                var lastWriteTime = System.IO.File.GetLastWriteTimeUtc(file);

                fileInfoCache.Add(file, new KeyValuePair<DateTime, DateTime>(creationTime, lastWriteTime));
                if (!minCreationTime.HasValue || minCreationTime > creationTime)
                {
                    minCreationTime = creationTime;
                }

                if (!minLastWriteTime.HasValue || minLastWriteTime > lastWriteTime)
                {
                    minLastWriteTime = lastWriteTime;
                }
            }

            var originalFiles =
                fileInfoCache.Where(x => x.Value.Key > minCreationTime || x.Value.Value > minLastWriteTime)
                    .Select(x => x.Key);
            return originalFiles.First();
        }
    }
}
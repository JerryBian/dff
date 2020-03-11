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
            var fileInfoCache = new Dictionary<string, DateTime>();

            foreach (var file in sameFiles)
            {
                var creationTime = System.IO.File.GetCreationTime(file);

                fileInfoCache.Add(file, creationTime);
                if (!minCreationTime.HasValue || minCreationTime > creationTime)
                {
                    minCreationTime = creationTime;
                }
            }

            var originalFiles = fileInfoCache.Where(x => x.Value > minCreationTime).ToList();
            if (originalFiles.Count > 1)
            {
                var minLastWriteTime = originalFiles.Min(x => System.IO.File.GetLastWriteTimeUtc(x.Key));
                originalFiles = originalFiles.Where(x => System.IO.File.GetLastWriteTimeUtc(x.Key) == minLastWriteTime)
                    .ToList();
            }
            else if(!originalFiles.Any()) // The creation time is same.
            {
                originalFiles = fileInfoCache.ToList();
            }

            return originalFiles.First().Key;
        }
    }
}
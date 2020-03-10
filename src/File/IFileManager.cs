using System.Collections.Generic;

namespace DuplicateFileFinder.File
{
    public interface IFileManager
    {
        void ProcessDuplicateFiles(string inputFolder, List<string> sameFiles);
    }
}
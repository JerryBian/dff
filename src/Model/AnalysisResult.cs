using System.Collections.Generic;

namespace DuplicateFileFinder.Model
{
    public class AnalysisResult
    {
        public int TotalFilesCount { get; set; }

        public int DuplicateFilesCount => DuplicateEntries.Count;

        public List<DuplicateEntry> DuplicateEntries { get; } = new List<DuplicateEntry>();
    }
}
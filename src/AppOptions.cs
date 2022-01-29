﻿namespace DuplicateFileFinder;

public class AppOptions
{
    public List<string> Dirs { get; } = new();

    public bool IncludeSubDirs { get; set; }

    public bool EnableVerboseLog { get; set; }
}
namespace DuplicateFileFinder;

public interface IOutputHandler
{
    void Ingest(OutputItem item);

    Task FlushAsync();
}
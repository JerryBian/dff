namespace DuplicateFileFinder;

public static class TaskExtension
{
    public static async Task UntilCancelled(this Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
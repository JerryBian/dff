namespace DuplicateFileFinder;

public class OutputItem
{
    public OutputItem(string message, bool appendNewLine = true, bool isError = false,
        MessageType messageType = MessageType.Default)
    {
        Message = message;
        MessageType = messageType;
        IsError = isError;
        AppendNewLine = appendNewLine;
    }

    public string Message { get; }

    public bool AppendNewLine { get; }

    public MessageType MessageType { get; }

    public bool IsError { get; }

    public string Exception { get; set; }
}
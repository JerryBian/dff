namespace DuplicateFileFinder;

public class OutputItem
{
    public OutputItem(string message = "", bool appendNewLine = true, bool isError = false,
        MessageType messageType = MessageType.Default, bool discard = false)
    {
        Message = message;
        MessageType = messageType;
        IsError = isError;
        AppendNewLine = appendNewLine;
        Discard = discard;
    }

    public string Message { get; }

    public bool AppendNewLine { get; }

    public MessageType MessageType { get; }

    public bool IsError { get; }

    public string Exception { get; set; }

    public bool Discard { get; }
}
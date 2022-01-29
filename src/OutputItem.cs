namespace DuplicateFileFinder;

public class OutputItem
{
    public OutputItem(string message, bool appendNewLine = true, bool isError = false,
        MessageType messageType = MessageType.Default, bool delayToEnd = false, bool discard = false)
    {
        Message = message;
        MessageType = messageType;
        IsError = isError;
        AppendNewLine = appendNewLine;
        DelayToEnd = delayToEnd;
        Discard = discard;
    }

    public string Message { get; }

    public bool AppendNewLine { get; }

    public MessageType MessageType { get; }

    public bool IsError { get; }

    public string Exception { get; set; }

    public bool DelayToEnd { get; }

    public bool Discard { get; }
}
namespace WTTCLIVizualizer.Commands;

[Serializable]
internal class NoSuchUserException : Exception
{
    public NoSuchUserException()
    {
    }

    public NoSuchUserException(string? message) : base(message)
    {
    }

    public NoSuchUserException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
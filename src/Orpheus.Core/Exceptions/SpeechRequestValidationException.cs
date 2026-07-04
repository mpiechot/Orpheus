namespace Orpheus.Core.Exceptions;

public sealed class SpeechRequestValidationException : Exception
{
    public SpeechRequestValidationException(string message)
        : base(message)
    {
    }
}

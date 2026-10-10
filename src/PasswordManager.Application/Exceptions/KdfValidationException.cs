namespace PasswordManager.Application.Exceptions;

/// <summary>
/// Exception thrown when untrusted or supplied KDF parameters violate security bounds or integrity requirements.
/// </summary>
public sealed class KdfValidationException : Exception
{
    public KdfValidationException(string message) : base(message)
    {
    }

    public KdfValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

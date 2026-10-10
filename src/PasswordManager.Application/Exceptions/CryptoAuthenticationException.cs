namespace PasswordManager.Application.Exceptions;

/// <summary>
/// Thrown when cryptographic authentication (AEAD auth tag, AAD context binding, or header validation) fails.
/// Fail-closed guarantee: no decrypted data is ever returned.
/// </summary>
public sealed class CryptoAuthenticationException : Exception
{
    public const string DefaultSafeMessage = "Kasa işlemi başarısız: kimlik doğrulama doğrulanamadı veya veri bozuk.";

    public CryptoAuthenticationException(string message = DefaultSafeMessage) : base(message)
    {
    }

    public CryptoAuthenticationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

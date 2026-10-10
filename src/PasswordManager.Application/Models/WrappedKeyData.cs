namespace PasswordManager.Application.Models;

/// <summary>
/// Immutable representation of a 256-bit AES-GCM wrapped root key.
/// </summary>
public sealed class WrappedKeyData
{
    public const int NonceLength = 12;
    public const int TagLength = 16;
    public const int CiphertextLength = 32;

    public byte[] Nonce { get; }
    public byte[] Tag { get; }
    public byte[] Ciphertext { get; }

    public WrappedKeyData(byte[] nonce, byte[] tag, byte[] ciphertext)
    {
        if (nonce is null || nonce.Length != NonceLength)
        {
            throw new ArgumentException($"Nonce must be exactly {NonceLength} bytes.", nameof(nonce));
        }

        if (tag is null || tag.Length != TagLength)
        {
            throw new ArgumentException($"Tag must be exactly {TagLength} bytes.", nameof(tag));
        }

        if (ciphertext is null || ciphertext.Length != CiphertextLength)
        {
            throw new ArgumentException($"Ciphertext must be exactly {CiphertextLength} bytes.", nameof(ciphertext));
        }

        Nonce = nonce;
        Tag = tag;
        Ciphertext = ciphertext;
    }
}

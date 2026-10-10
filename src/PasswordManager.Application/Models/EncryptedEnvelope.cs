namespace PasswordManager.Application.Models;

/// <summary>
/// Immutable representation of an AES-256-GCM encrypted envelope.
/// </summary>
public sealed class EncryptedEnvelope
{
    public const int NonceLength = 12; // 96 bits
    public const int TagLength = 16;   // 128 bits
    public const int CurrentVersion = 1;

    public int EnvelopeVersion { get; }
    public byte[] Nonce { get; }
    public byte[] Tag { get; }
    public byte[] Ciphertext { get; }

    public EncryptedEnvelope(int envelopeVersion, byte[] nonce, byte[] tag, byte[] ciphertext)
    {
        if (nonce is null || nonce.Length != NonceLength)
        {
            throw new ArgumentException($"Nonce must be exactly {NonceLength} bytes.", nameof(nonce));
        }

        if (tag is null || tag.Length != TagLength)
        {
            throw new ArgumentException($"Tag must be exactly {TagLength} bytes.", nameof(tag));
        }

        ArgumentNullException.ThrowIfNull(ciphertext);

        EnvelopeVersion = envelopeVersion;
        Nonce = nonce;
        Tag = tag;
        Ciphertext = ciphertext;
    }
}

using Konscious.Security.Cryptography;
using PasswordManager.Application.Abstractions;
using PasswordManager.Application.Models;
using PasswordManager.Application.Validation;

namespace PasswordManager.Security;

/// <summary>
/// Implements RFC 9106 Argon2id key derivation with bounded anti-DoS validation.
/// </summary>
public sealed class Argon2idKeyDerivationService : IKeyDerivationService
{
    public byte[] DeriveKey(byte[] passwordBytes, byte[] salt, in KdfParameters parameters)
    {
        KdfParametersValidator.ValidateInputs(parameters, passwordBytes, salt);

        using var argon2 = new Argon2id(passwordBytes)
        {
            Salt = salt,
            MemorySize = parameters.MemoryKiB,
            Iterations = parameters.Iterations,
            DegreeOfParallelism = parameters.DegreeOfParallelism
        };

        return argon2.GetBytes(parameters.KeyLength);
    }

    public async Task<byte[]> DeriveKeyAsync(
        byte[] passwordBytes,
        byte[] salt,
        KdfParameters parameters,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        KdfParametersValidator.ValidateInputs(parameters, passwordBytes, salt);

        using var argon2 = new Argon2id(passwordBytes)
        {
            Salt = salt,
            MemorySize = parameters.MemoryKiB,
            Iterations = parameters.Iterations,
            DegreeOfParallelism = parameters.DegreeOfParallelism
        };

        cancellationToken.ThrowIfCancellationRequested();
        return await argon2.GetBytesAsync(parameters.KeyLength).ConfigureAwait(false);
    }

    /// <summary>
    /// Computes raw Argon2id with explicit memory size in KiB, iterations, parallelism, secret and associated data.
    /// Primarily used for low-memory RFC test vector verification.
    /// </summary>
    public static byte[] DeriveRaw(
        byte[] passwordBytes,
        byte[] salt,
        int memoryKiB,
        int iterations,
        int degreeOfParallelism,
        int outputLength,
        byte[]? knownSecret = null,
        byte[]? associatedData = null)
    {
        using var argon2 = new Argon2id(passwordBytes)
        {
            Salt = salt,
            MemorySize = memoryKiB,
            Iterations = iterations,
            DegreeOfParallelism = degreeOfParallelism,
            KnownSecret = knownSecret,
            AssociatedData = associatedData
        };

        return argon2.GetBytes(outputLength);
    }
}

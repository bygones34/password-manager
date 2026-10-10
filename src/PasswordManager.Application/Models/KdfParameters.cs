namespace PasswordManager.Application.Models;

/// <summary>
/// Immutable parameters for Argon2id Key Derivation Function with RFC 9106 safety bounds.
/// </summary>
public readonly record struct KdfParameters
{
    public const int MinMemoryKiB = 16384;      // 16 MiB
    public const int MaxMemoryKiB = 524288;     // 512 MiB
    public const int MinIterations = 1;
    public const int MaxIterations = 10;
    public const int MinDegreeOfParallelism = 1;
    public const int MaxDegreeOfParallelism = 16;
    public const int MinSaltLength = 16;        // 128 bits
    public const int MaxSaltLength = 64;        // 512 bits
    public const int MinKeyLength = 16;         // 128 bits
    public const int MaxKeyLength = 64;         // 512 bits

    public int MemoryKiB { get; init; }
    public int Iterations { get; init; }
    public int DegreeOfParallelism { get; init; }
    public int SaltLength { get; init; }
    public int KeyLength { get; init; }

    public KdfParameters(
        int memoryKiB,
        int iterations,
        int degreeOfParallelism,
        int saltLength = 32,
        int keyLength = 32)
    {
        MemoryKiB = memoryKiB;
        Iterations = iterations;
        DegreeOfParallelism = degreeOfParallelism;
        SaltLength = saltLength;
        KeyLength = keyLength;
    }

    /// <summary>
    /// Default RFC 9106 desktop baseline profile: 64 MiB RAM, 3 iterations, 4 parallelism lanes, 32 bytes salt, 32 bytes KEK.
    /// </summary>
    public static KdfParameters Default => new(
        memoryKiB: 65536,
        iterations: 3,
        degreeOfParallelism: 4,
        saltLength: 32,
        keyLength: 32);
}

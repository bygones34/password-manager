using PasswordManager.Application.Exceptions;
using PasswordManager.Application.Models;

namespace PasswordManager.Application.Validation;

/// <summary>
/// Enforces bounded anti-DoS and security validation rules on KDF parameters before execution.
/// </summary>
public static class KdfParametersValidator
{
    public static void Validate(in KdfParameters parameters)
    {
        if (parameters.MemoryKiB < KdfParameters.MinMemoryKiB || parameters.MemoryKiB > KdfParameters.MaxMemoryKiB)
        {
            throw new KdfValidationException(
                $"Memory cost {parameters.MemoryKiB} KiB is out of safe bounds [{KdfParameters.MinMemoryKiB}..{KdfParameters.MaxMemoryKiB}].");
        }

        if (parameters.Iterations < KdfParameters.MinIterations || parameters.Iterations > KdfParameters.MaxIterations)
        {
            throw new KdfValidationException(
                $"Iterations count {parameters.Iterations} is out of safe bounds [{KdfParameters.MinIterations}..{KdfParameters.MaxIterations}].");
        }

        if (parameters.DegreeOfParallelism < KdfParameters.MinDegreeOfParallelism || parameters.DegreeOfParallelism > KdfParameters.MaxDegreeOfParallelism)
        {
            throw new KdfValidationException(
                $"Parallelism {parameters.DegreeOfParallelism} is out of safe bounds [{KdfParameters.MinDegreeOfParallelism}..{KdfParameters.MaxDegreeOfParallelism}].");
        }

        if (parameters.SaltLength < KdfParameters.MinSaltLength || parameters.SaltLength > KdfParameters.MaxSaltLength)
        {
            throw new KdfValidationException(
                $"Salt length {parameters.SaltLength} is out of safe bounds [{KdfParameters.MinSaltLength}..{KdfParameters.MaxSaltLength}].");
        }

        if (parameters.KeyLength < KdfParameters.MinKeyLength || parameters.KeyLength > KdfParameters.MaxKeyLength)
        {
            throw new KdfValidationException(
                $"Key length {parameters.KeyLength} is out of safe bounds [{KdfParameters.MinKeyLength}..{KdfParameters.MaxKeyLength}].");
        }
    }

    public static void ValidateInputs(in KdfParameters parameters, byte[]? passwordBytes, byte[]? saltBytes)
    {
        Validate(parameters);

        if (passwordBytes is null)
        {
            throw new KdfValidationException("Password bytes cannot be null.");
        }

        if (saltBytes is null)
        {
            throw new KdfValidationException("Salt bytes cannot be null.");
        }

        if (saltBytes.Length != parameters.SaltLength)
        {
            throw new KdfValidationException(
                $"Supplied salt length ({saltBytes.Length} bytes) does not match expected parameter salt length ({parameters.SaltLength} bytes).");
        }
    }
}

using System.Security.Cryptography;
using System.Text;
using PasswordManager.Application.Abstractions;
using PasswordManager.Application.Models;
using PasswordManager.Application.Session;
using PasswordManager.Application.Validation;
using PasswordManager.Domain.Enums;

namespace PasswordManager.Application.Services;

/// <summary>
/// Orchestrates vault lifecycle states (NoVault, Locked, Unlocking, Unlocked, Locking, Faulted),
/// active cryptographic sessions, generation tracking, deterministic key disposal, and operation scopes.
/// </summary>
public sealed class VaultLifecycleManager : IVaultLifecycleManager
{
    private readonly IStoragePathProvider _storagePathProvider;
    private readonly IVaultDatabaseInitializer _databaseInitializer;
    private readonly IVaultStorageService _storageService;
    private readonly IVaultHeaderService _headerService;
    private readonly IAeadEnvelopeService _envelopeService;
    private readonly IKeyDerivationService _kdfService;

    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private VaultState _currentState = VaultState.NoVault;
    private long _currentGeneration;
    private Guid? _vaultId;
    private VaultSession? _currentSession;
    private bool _isDisposed;

    public VaultState CurrentState => _currentState;
    public long CurrentGeneration => _currentGeneration;
    public Guid? VaultId => _vaultId;

    public event EventHandler<VaultStateChangedEventArgs>? StateChanged;

    public VaultLifecycleManager(
        IStoragePathProvider storagePathProvider,
        IVaultDatabaseInitializer databaseInitializer,
        IVaultStorageService storageService,
        IVaultHeaderService headerService,
        IAeadEnvelopeService envelopeService,
        IKeyDerivationService kdfService)
    {
        _storagePathProvider = storagePathProvider ?? throw new ArgumentNullException(nameof(storagePathProvider));
        _databaseInitializer = databaseInitializer ?? throw new ArgumentNullException(nameof(databaseInitializer));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _headerService = headerService ?? throw new ArgumentNullException(nameof(headerService));
        _envelopeService = envelopeService ?? throw new ArgumentNullException(nameof(envelopeService));
        _kdfService = kdfService ?? throw new ArgumentNullException(nameof(kdfService));
    }

    /// <inheritdoc />
    public async Task<bool> RefreshVaultStateAsync(CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();

            // Do not override state during active session or transition
            if (_currentState is VaultState.Unlocking or VaultState.Unlocked or VaultState.Locking)
            {
                return true;
            }

            var dbPath = _storagePathProvider.GetVaultDatabasePath();
            var exists = await _storageService.VaultExistsAsync(dbPath, cancellationToken).ConfigureAwait(false);

            if (exists)
            {
                if (_currentState != VaultState.Locked)
                {
                    SetState(VaultState.Locked);
                }
            }
            else
            {
                if (_currentState != VaultState.NoVault)
                {
                    SetState(VaultState.NoVault);
                }
                _vaultId = null;
            }

            return exists;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task CreateVaultAsync(
        ReadOnlyMemory<char> masterPassword,
        KdfParameters? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();

            if (_currentState is VaultState.Unlocked or VaultState.Unlocking)
            {
                throw new InvalidOperationException("Vault is already unlocked or in an unlocking transition.");
            }

            var dbPath = _storagePathProvider.GetVaultDatabasePath();
            if (await _storageService.VaultExistsAsync(dbPath, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("A vault already exists at the specified storage location.");
            }

            SetState(VaultState.Unlocking);

            var kdfParams = parameters ?? KdfParameters.Default;
            KdfParametersValidator.Validate(kdfParams);

            await _databaseInitializer.InitializeDatabaseAsync(dbPath, cancellationToken).ConfigureAwait(false);

            var vaultId = Guid.NewGuid();
            byte[]? passwordBytes = null;
            byte[]? kek = null;
            byte[]? rootKey = null;
            byte[]? recordKey = null;
            byte[]? manifestKey = null;

            try
            {
                passwordBytes = MasterPasswordToUtf8Bytes(masterPassword);
                var salt = _envelopeService.GenerateRandomBytes(kdfParams.SaltLength);
                kek = await _kdfService.DeriveKeyAsync(passwordBytes, salt, kdfParams, cancellationToken).ConfigureAwait(false);

                rootKey = _envelopeService.GenerateRandomBytes(32);
                var headerAad = _headerService.ComputeHeaderAad(vaultId, VaultHeaderData.Argon2idAlgorithmId, kdfParams, salt);
                var wrappedKey = _envelopeService.WrapRootKey(kek, rootKey, headerAad);

                var headerData = new VaultHeaderData(
                    vaultId,
                    kdfParams,
                    salt,
                    wrappedKey);

                var headerBytes = _headerService.Serialize(headerData);
                await _storageService.SaveHeaderAsync(dbPath, headerBytes, cancellationToken).ConfigureAwait(false);

                recordKey = _envelopeService.DeriveRecordKey(rootKey, vaultId);
                manifestKey = _envelopeService.DeriveManifestKey(rootKey, vaultId);

                // Initial empty manifest per VAULT_FORMAT specification
                var initialManifestJson = "{\"schema_version\":1,\"vault_name\":\"Personal Vault\",\"categories\":[\"General\",\"Work\",\"Finance\",\"Social\"],\"records\":[],\"last_modified_at\":\"" + DateTime.UtcNow.ToString("O") + "\"}";
                var manifestPayloadBytes = Encoding.UTF8.GetBytes(initialManifestJson);
                var manifestEnvelope = _envelopeService.EncryptManifest(manifestKey, vaultId, manifestPayloadBytes);
                await _storageService.SaveManifestAsync(dbPath, manifestEnvelope, cancellationToken).ConfigureAwait(false);

                _vaultId = vaultId;
                _currentGeneration++;
                _currentSession = new VaultSession(vaultId, _currentGeneration, rootKey, recordKey, manifestKey);
                SetState(VaultState.Unlocked);
            }
            catch
            {
                SetState(VaultState.Locked);
                throw;
            }
            finally
            {
                if (passwordBytes != null) CryptographicOperations.ZeroMemory(passwordBytes);
                if (kek != null) CryptographicOperations.ZeroMemory(kek);
                if (rootKey != null) CryptographicOperations.ZeroMemory(rootKey);
                if (recordKey != null) CryptographicOperations.ZeroMemory(recordKey);
                if (manifestKey != null) CryptographicOperations.ZeroMemory(manifestKey);
            }
        }
        finally
        {
            _stateLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UnlockVaultAsync(
        ReadOnlyMemory<char> masterPassword,
        CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();

            if (_currentState is VaultState.Unlocked or VaultState.Unlocking)
            {
                throw new InvalidOperationException("Vault is already unlocked or currently unlocking.");
            }

            var dbPath = _storagePathProvider.GetVaultDatabasePath();
            var headerBytes = await _storageService.GetHeaderBytesAsync(dbPath, cancellationToken).ConfigureAwait(false);
            if (headerBytes is null)
            {
                throw new InvalidOperationException("No valid vault found at storage location to unlock.");
            }

            SetState(VaultState.Unlocking);

            byte[]? passwordBytes = null;
            byte[]? kek = null;
            byte[]? rootKey = null;
            byte[]? recordKey = null;
            byte[]? manifestKey = null;

            try
            {
                var headerData = _headerService.Deserialize(headerBytes);
                passwordBytes = MasterPasswordToUtf8Bytes(masterPassword);
                kek = await _kdfService.DeriveKeyAsync(passwordBytes, headerData.Salt, headerData.Parameters, cancellationToken).ConfigureAwait(false);

                var headerAad = _headerService.ComputeHeaderAad(
                    headerData.VaultId,
                    headerData.KdfAlgorithmId,
                    headerData.Parameters,
                    headerData.Salt,
                    headerData.Magic,
                    headerData.CryptoFormatVersion);

                rootKey = _envelopeService.UnwrapRootKey(kek, headerData.WrappedRootKey, headerAad);

                recordKey = _envelopeService.DeriveRecordKey(rootKey, headerData.VaultId);
                manifestKey = _envelopeService.DeriveManifestKey(rootKey, headerData.VaultId);

                // Authenticate manifest envelope integrity
                var manifestEnvelope = await _storageService.GetManifestAsync(dbPath, cancellationToken).ConfigureAwait(false);
                if (manifestEnvelope != null)
                {
                    _envelopeService.DecryptManifest(manifestKey, headerData.VaultId, manifestEnvelope);
                }

                _vaultId = headerData.VaultId;
                _currentGeneration++;
                _currentSession = new VaultSession(headerData.VaultId, _currentGeneration, rootKey, recordKey, manifestKey);
                SetState(VaultState.Unlocked);
            }
            catch
            {
                SetState(VaultState.Locked);
                throw;
            }
            finally
            {
                if (passwordBytes != null) CryptographicOperations.ZeroMemory(passwordBytes);
                if (kek != null) CryptographicOperations.ZeroMemory(kek);
                if (rootKey != null) CryptographicOperations.ZeroMemory(rootKey);
                if (recordKey != null) CryptographicOperations.ZeroMemory(recordKey);
                if (manifestKey != null) CryptographicOperations.ZeroMemory(manifestKey);
            }
        }
        finally
        {
            _stateLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task LockVaultAsync(CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();

            if (_currentState != VaultState.Unlocked)
            {
                return;
            }

            SetState(VaultState.Locking);

            if (_currentSession != null)
            {
                _currentSession.Dispose();
                _currentSession = null;
            }

            SetState(VaultState.Locked);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    /// <inheritdoc />
    public VaultSession GetActiveSession()
    {
        ThrowIfDisposed();

        if (_currentState != VaultState.Unlocked || _currentSession is null || _currentSession.IsDisposed)
        {
            throw new InvalidOperationException("Vault is not unlocked. No active session exists.");
        }

        return _currentSession;
    }

    /// <inheritdoc />
    public IOperationScope CreateOperationScope(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_currentState != VaultState.Unlocked || _currentSession is null || _currentSession.IsDisposed)
        {
            throw new InvalidOperationException("Cannot create operation scope while vault is locked.");
        }

        return new OperationScope(_currentSession, () => _currentGeneration, cancellationToken);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_currentSession != null)
        {
            _currentSession.Dispose();
            _currentSession = null;
        }

        _currentState = VaultState.Locked;
        _stateLock.Dispose();
    }

    private void SetState(VaultState newState)
    {
        if (_currentState == newState)
        {
            return;
        }

        var oldState = _currentState;
        _currentState = newState;
        StateChanged?.Invoke(this, new VaultStateChangedEventArgs(oldState, newState, _currentGeneration));
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(VaultLifecycleManager), "Vault lifecycle manager has been disposed.");
        }
    }

    private static byte[] MasterPasswordToUtf8Bytes(ReadOnlyMemory<char> masterPassword)
    {
        var span = masterPassword.Span;
        int byteCount = Encoding.UTF8.GetByteCount(span);
        byte[] bytes = new byte[byteCount];
        Encoding.UTF8.GetBytes(span, bytes);
        return bytes;
    }
}

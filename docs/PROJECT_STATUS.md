# Project Status

Last updated: 2026-10-18
Current milestone: M1 (Secure Vault Core) — COMPLETED / M2 (Credential Management and Secure UX) — NEXT
Current task: M1.7 (Completed) / M2.1 (Next)

---

## 1. Completed Tasks

### Milestone M0 — Foundation & Technical Feasibility (COMPLETED)
* **M0.1 — Product Scope and Initial Threat Model:**
  - Project code name (`PasswordManager`), Windows 11 (x64) target architecture, and product boundaries established.
  - `docs/THREAT_MODEL.md` (v0) and `README.md` prepared; local git repository initialized.
* **M0.2 — Toolchain Verification and Architecture Decision Record:**
  - Visual Studio Community 2026 Insiders, .NET 10 SDK (`10.0.401`), and Windows Kits 10 SDK (`10.0.26100.0`) verified.
  - Windows App SDK 1.6 XAML compilation verified and `AppxMSBuildToolsPath` resolved for CLI builds.
  - `docs/adr/ADR-001.md` accepted.
* **M0.3 — Solution, Layer Projects, CPM, and Standards:**
  - `global.json`, `.editorconfig`, `.gitignore`, `Directory.Build.props`, and `Directory.Packages.props` configured.
  - 7 core source layer projects and 6 test projects created and wired to `PasswordManager.slnx`.
  - Dependency rules enforced and smoke tests passed.
* **M0.4 — WinUI Shell & Modern UI Skeleton:**
  - `Styles/Tokens.xaml` created with semantic colors (`ThemeDictionaries` for Light/Dark themes), card borders, typography, and spacing tokens, integrated into `App.xaml`.
  - `CommunityToolkit.Mvvm` (8.4.2) added to Central Package Management and integrated into the Desktop project.
  - `SyntheticVaultItem` model and `SyntheticVaultService` implemented.
  - `MainViewModel` developed (C# 13/14 partial properties, filtering, search, password masking/reveal, favorite toggling, and theme switching).
  - `MainWindow.xaml` & `MainWindow.xaml.cs` created featuring a left `NavigationView`, top search/theme bar, central item list, and right-hand details pane.
  - `WindowsAppSDKSelfContained=true` and `win-x64` configured for standalone operation.
* **M0.5 — Native Spike (Tray, Monitor/DPI, Foreground HWND, Session Lock):**
  - Win32 P/Invoke APIs (`NativeMethods.cs`) defined in `PasswordManager.Platform.Windows`.
  - Foreground HWND and process detection (`ForegroundTargetDetector.cs`) implemented.
  - Flyout positioning and edge-clamping algorithm (`TrayPositionCalculator.cs`) developed.
  - Win32 WTS session lock event listener (`SessionLockWatcher.cs`) and system tray icon manager (`TrayIconManager.cs`) created.
* **M0.6 — SQLite File Paths, ACLs, and Single-Instance Behavior:**
  - `IStoragePathProvider` and `ISingleInstanceManager` ports defined in the `Application` layer.
  - `WindowsStoragePathResolver` implemented: differentiated unpackaged (`%LOCALAPPDATA%\PasswordManager\vault.db`) vs. packaged (MSIX) environments; applied Windows NTFS ACLs (`DirectorySecurity`) granting Full Control exclusively to current user and administrators while restricting other local accounts.
  - `SingleInstanceAppLock` implemented: user-session-scoped named Mutex and `EventWaitHandle` signaling mechanism preventing database lock contention; brings existing instance to the foreground on secondary launch.
  - Architectural design and security specifics documented in `docs/STORAGE_AND_SINGLE_INSTANCE.md`.
* **M0.7 — Windows CI Build/Test Pipeline & Cross-Platform Verification:**
  - GitHub Actions workflow (`.github/workflows/ci.yml`) established: full solution build and tests on Windows, matrix build for cross-platform core projects (`Domain`, `Application`, `Security`, `Infrastructure`) on Ubuntu.
  - Added dynamic detection of `AppxMSBuildToolsPath` in `Directory.Build.props` for `windows-latest` (VS 2022 Enterprise) runners, ensuring WinUI 3 CLI build compatibility.
  - Release mode builds and tests executed successfully; GitHub Actions CI confirmed green.

### Milestone M1 — Secure Vault Core (COMPLETED)
* **M1.1 — Threat Model v1, Vault Format v1, Cryptographic Architecture, and ADRs:**
  - `docs/THREAT_MODEL.md` updated to v1.0: two-tier key hierarchy (KEK, RootKey, RecordKey, ManifestKey), AES-256-GCM AEAD, AAD context binding, CSPRNG 96-bit nonce security, KDF DoS / hostile import defenses, memory hygiene (`ZeroMemory`), and fail-closed principles defined.
  - `docs/VAULT_FORMAT.md` v1.0 specification finalized: database schema decoupled from crypto format; 132-byte binary header (`VaultHeader`: Magic `"PWMV"`, version 1, GUID `VaultId`, Argon2id parameters, Salt, Wrapped Root Key), canonical JSON schemas (`VaultRecord`, `VaultManifest`), AES-256-GCM AAD structures, $O(1)$ master password re-wrap protocol, and root-key rotation procedures detailed.
  - `docs/adr/ADR-002.md` accepted: Cryptographic Vault Format, Key Hierarchy, and AEAD Nonce Policy.
  - `docs/adr/ADR-003.md` accepted: Argon2id KDF Parameters, Package Selection (`Konscious.Security.Cryptography.Argon2`), and Bounded Anti-DoS Validation Policy (Memory: 16–512 MiB, Iterations: 1–10, Parallelism: 1–16, Salt: 16–64 bytes).
  - `docs/SECURITY_TEST_MATRIX.md` created: RFC 9106/5869/NIST test vectors, key separation, tampering/substitution, nonce uniqueness, bounded DoS, memory zeroing, and SQLite/log canary scanning requirements documented.
  - `DEVELOPMENT_ROADMAP.md` updated (`M1.1` marked complete).
* **M1.2 — Argon2id Package Integration, RFC Vectors, Benchmarks, and Bound Checks:**
  - Added `Konscious.Security.Cryptography.Argon2` (1.3.1) to `Directory.Packages.props` and referenced in `PasswordManager.Security`.
  - Implemented `IKeyDerivationService` port in `Application` layer, `KdfParameters` model (RFC 9106 bound constants and default desktop profile: 64 MiB RAM, 3 iterations, 4 lanes, 32-byte salt, 32-byte KEK), `KdfValidationException`, and `KdfParametersValidator`.
  - Implemented `Argon2idKeyDerivationService` in `Security` layer: synchronous (`DeriveKey`), asynchronous (`DeriveKeyAsync` with `CancellationToken`), and low-memory RFC vector helper (`DeriveRaw`). Enforced strict bounding checks prior to computation.
  - Created 3 test classes in `PasswordManager.Security.Tests`:
    - `Argon2idVectorTests`: Verified exact match with RFC 9106 Section 5.3 official test vector ($32 \text{ KiB}, t=3, p=4$, password, salt, secret, AAD, and 32-byte tag); added determinism and 1-bit sensitivity tests.
    - `KdfBoundingTests`: Validated memory bounds (min 16 MiB, max 512 MiB), iterations (1..10), parallelism (1..16), salt length (16..64 bytes), key length (16..64 bytes), and null/invalid inputs; verified all boundary violations throw `KdfValidationException`.
    - `Argon2idBenchmarkTests`: Executed 64 MiB desktop profile; verified execution duration and asynchronous cancellation behavior.
* **M1.3 — CSPRNG, Key Wrap, HKDF Key Separation, and AEAD Envelope Implementation:**
  - Defined `EncryptedEnvelope`, `WrappedKeyData`, `VaultHeaderData` models, `CryptoAuthenticationException`, `IAeadEnvelopeService`, and `IVaultHeaderService` ports in `Application`.
  - Implemented `VaultHeaderService` in `Security`: 132-byte binary header serialization/deserialization, `HeaderAad` (72-byte) computation, and anti-DoS boundary checks.
  - Implemented `AeadEnvelopeService` in `Security`: CSPRNG generation, root key wrapping with AES-256-GCM (`WrapRootKey` / `UnwrapRootKey`), RFC 5869 `HKDF-SHA-256` key derivation for `RecordKey` and `ManifestKey`, `RecordAad` (48-byte) and `ManifestAad` (34-byte) canonical context bindings, AEAD encryption/decryption, and fail-closed error handling.
  - Added 5 new test classes to `PasswordManager.Security.Tests`:
    - `HkdfVectorTests`: Verified RFC 5869 Test Case 1 (`HKDF-Expand` and `HKDF.DeriveKey` SHA-256) official test vectors.
    - `AesGcmVectorTests`: Verified NIST SP 800-38D official test vectors.
    - `KeySeparationTests`: Verified SEC-B01 (`RecordKey != ManifestKey != RootKey != KEK`), SEC-B02 (distinct `VaultId`), and SEC-B03 (cross-key decrypt prevention).
    - `EnvelopeTamperTests`: Verified SEC-C01 (1-bit flip), SEC-C02 (tag tampering), SEC-C03 (RecordId substitution), SEC-C04 (cross-vault swap), SEC-C05 (header AAD tampering), and SEC-C06 (invalid magic/version).
    - `NonceRandomnessTests`: Verified SEC-D01 (zero collisions across 1,000 consecutive encryptions) and SEC-D02 (high entropy).
    - `VaultHeaderSerializationTests`: Verified 132-byte header full roundtrip and size checks.
  - `DEVELOPMENT_ROADMAP.md` updated (`M1.3` marked complete).
* **M1.4 — Encrypted Envelope Schema, EF Core SQLite Integration, and Plaintext Isolation:**
  - Added `Microsoft.EntityFrameworkCore.Sqlite` (9.0.2) and vulnerability patch `SQLitePCLRaw.bundle_e_sqlite3` (2.1.13) to `Directory.Packages.props` and `PasswordManager.Infrastructure.csproj`.
  - Defined `IVaultStorageService` and `IVaultDatabaseInitializer` ports in `Application`. Zero plaintext credentials leaked into ports or parameters.
  - Created `VaultHeaderEntity`, `VaultManifestEntity`, `VaultRecordEntity`, and `VaultMetadataEntity` in `Infrastructure`. Mapped tables in `VaultDbContext`: `VaultHeader` (single-row constraint), `VaultManifest`, `VaultRecords`, and `VaultMetadata`.
  - Implemented `VaultDatabaseInitializer` strictly decoupling database schema version (`CurrentSchemaVersion = 1`) from crypto format version (`CurrentFormatVersion = 1`); enabled SQLite WAL mode (`PRAGMA journal_mode = WAL;`) and foreign keys; ensured fail-closed handling for unsupported future schema versions.
  - Implemented `SqliteVaultStorageService` providing CRUD operations for header, manifest, and record encrypted envelopes.
  - Added 3 test classes in `PasswordManager.Infrastructure.Tests`:
    - `VaultDatabaseInitializerTests`: Verified table creation, WAL mode enablement, idempotent startup, and fail-closed behavior on future schema versions.
    - `SqliteVaultStorageServiceTests`: Tested complete CRUD lifecycle of header (132B), manifest, and record envelopes.
    - `DatabaseZeroPlaintextTests` (Category G: SEC-G01): Scanned SQLite `.db` and `-wal` files following encrypted record insertion, proving zero plaintext of synthetic passwords/usernames in physical storage, and confirming no plaintext columns exist in `VaultRecords`.
  - `DEVELOPMENT_ROADMAP.md` updated (`M1.4` marked complete).
* **M1.5 — Create/Unlock/Lock Lifecycle, State Machine, Operation Scope, Key Disposal, and Cancellation:**
  - Defined `VaultState` enum (`NoVault`, `Locked`, `Unlocking`, `Unlocked`, `Locking`, `Faulted`) in `Domain`.
  - Implemented sensitive operation and session models in `Application`:
    - `VaultSession`: Manages decrypted symmetric keys (`RootKey`, `RecordKey`, `ManifestKey`), monotonically increasing `SessionGeneration` counter, and session cancellation token source (`CancellationTokenSource`). Wipes all key arrays on `Dispose` using `CryptographicOperations.ZeroMemory` (SEC-F01) and triggers cancellation.
    - `IOperationScope` & `OperationScope`: Binds sensitive operations (decrypt, reveal, autofill) to a session generation; immediately terminates in-flight operations with `OperationCanceledException` upon lock or generation change (SEC-F02), preventing delayed deliveries to UI.
    - `VaultStateChangedEventArgs`: Broadcasts state changes and active generation number.
    - `IVaultLifecycleManager` and `VaultLifecycleManager` orchestrator:
      - Serializes state mutations via `SemaphoreSlim(1, 1)`, rejecting concurrent unlock calls.
      - `CreateVaultAsync`: Performs file presence checks, DB schema initialization, Argon2id derivation, root key wrapping (AES-256-GCM + Header AAD), initial manifest creation, $O(1)$ session activation, and immediate zeroing of temporary KEK/key buffers.
      - `UnlockVaultAsync`: Deserializes 132-byte header, validates bounds, derives KEK (UTF-8, no-trim, no-normalize), unwraps root key, checks manifest integrity, and enforces fail-closed handling (zeroes memory and stays locked on `CryptoAuthenticationException`).
      - `LockVaultAsync`: Idempotent lock; purges active session and key material from memory, aborts operation scopes.
  - Added 23 unit and integration tests in `PasswordManager.Application.Tests`:
    - `VaultSessionTests`: Key cloning, size validation, deterministic `ZeroMemory` wipe (SEC-F01), and `Dispose` idempotence.
    - `OperationScopeTests`: Generation tracking, cancellation upon lock (SEC-F02), external token integration, and prevention of data delivery across subsequent generations.
    - `VaultLifecycleManagerTests`: Full lifecycle integration test with SQLite database and cryptographic services (`NoVault` → `Create` → `Unlocked` → `Lock` → `Locked` → `Unlock` → `Unlocked`); concurrent unlock rejection, fail-closed lock on bad password or corrupted manifest, immediate task cancellation on lock, and memory zeroing.
  - `DEVELOPMENT_ROADMAP.md` updated (`M1.5` marked complete).
* **M1.6 — Auto-Lock Inactivity Timer, Windows Session / Suspend Listeners, and Logging Whitelist:**
  - Implemented `ISessionLockListener` and `IAutoLockCoordinator` ports with `AutoLockCoordinator` in `Application`:
    - 5-minute default user inactivity timer (`InactivityTimeout = 5 min`) using testable `TimeProvider`.
    - `RecordUserActivity()` resets the timer upon keyboard/mouse/navigation input.
    - Timer starts automatically on `Unlocked` and stops on `Locked`.
    - Vault locks immediately on Windows lock (`WorkstationLocked`), logoff (`SessionLogoff`), remote disconnect (`RemoteDisconnect`), and system suspend (`SystemSuspend`); vault remains locked on system resume.
  - Implemented `WindowsSessionLockListener` in `Platform.Windows`:
    - Captured Win32 WTS session notifications (`WM_WTSSESSION_CHANGE`) and `NativeMethods.WM_POWERBROADCAST` power messages (`PBM_APMSUSPEND = 0x0004`) via `SessionLockWatcher`, mapping them to `ISessionLockListener` events.
  - Established strict Whitelist Security Logging (`ISecureLogger`) in `Application`:
    - `SecureLogEvent` model: Free-form text fields, DTO dumps, user inputs, and exception stack traces prohibited. Allowed whitelist fields: `event_code`, `duration_ms`, `success`, `error_code`, `timestamp`.
    - `SecureLogSanitizer`: Approved event code list and regex validation (`^[A-Z0-9_]{3,64}$`); unapproved fields sanitized to `SANITIZED_EVENT` and `SANITIZED_ERROR`.
    - Developed `SecureMemoryAuditLogger` and structured NDJSON stream logger `SecureJsonStreamLogger`.
    - Injected `ISecureLogger` into `VaultLifecycleManager`; logged `VAULT_CREATED`, `VAULT_UNLOCKED`, `UNLOCK_FAILED` (`AUTH_FAILED`), and `VAULT_LOCKED` events with durations and status.
  - Added 21 tests across test suites:
    - `AutoLockCoordinatorTests`: Verified 5-minute inactivity lock with `ManualTimeProvider`, timer postponement on user action, timer disabling, and instant lock on OS lock/suspend.
    - `LogSanitizationTests` (SEC-G02): Scanned NDJSON logs generated across vault creation, unlock, bad password attempts, and locking, confirming zero plaintext secret leakage and strict JSON whitelist adherence.
    - `WindowsSessionLockListenerTests`: Verified WTS and PowerBroadcast Win32 message mappings.
  - `DEVELOPMENT_ROADMAP.md` updated (`M1.6` marked complete).
* **M1.7 — Security Exit Gate Verification Suite and Physical Canary Scanning (MILESTONE M1 COMPLETED):**
  - Developed `MilestoneOneSecurityGateTests` integrating all Milestone M1 exit criteria and Security Test Matrix (Categories A–G):
    - **Gate 1 (Correct Password & Session Generation):** Verified vault create, lock, and unlock with correct master password; confirmed active keys and incremented `SessionGeneration`.
    - **Gate 2 (Incorrect Password & Fail-Closed):** Proved unlock with wrong password throws `CryptoAuthenticationException`, vault remains `Locked`, zero keys leaked, and only `UNLOCK_FAILED` (`AUTH_FAILED`) is logged.
    - **Gate 3 (Integrity & Anti-Tamper):** Verified 1-bit ciphertext flip, `RecordId` substitution, cross-vault (`VaultId`) swap, and database header byte tampering fail closed immediately.
    - **Gate 4 (Nonce Uniqueness):** Proved 100 consecutive encryptions of identical plaintext generate 100 distinct 96-bit CSPRNG nonces and ciphertexts.
    - **Gate 5 (Hostile Header / Bounded Anti-DoS):** Verified headers specifying 2 GB memory cost or invalid magic ("BAD!") are rejected before KDF execution via `KdfValidationException` or `CryptoAuthenticationException`.
    - **Gate 6 (Session Invalidation & Delayed Delivery Prevention):** Verified active `IOperationScope` instances abort immediately upon lock, and stale scopes throw `OperationCanceledException` without delivering data to subsequent generations.
    - **Gate 7 (Zero-Plaintext Binary Canary Scan):** After a complete lifecycle (create, add record, lock, unlock, lock), binary scanning of SQLite `.db`, `-wal`, and NDJSON logs confirmed zero occurrences of synthetic master passwords or credentials.
  - `DEVELOPMENT_ROADMAP.md` updated (`M1.7` marked complete; Milestone M1 completed).

---

## 2. Verification & Executed Commands
* `dotnet test tests/PasswordManager.Application.Tests/PasswordManager.Application.Tests.csproj -c Release` → **45 tests passed, 0 failures, 0 skipped (+7 tests)**.
* `dotnet test PasswordManager.slnx -c Release` → **124 tests passed across all 6 test assemblies, 0 failures, 0 skipped (100% success)**.
* `git diff --check` → Clean.

---

## 3. Manual Verification Status
* SEC-A01 .. SEC-A03: RFC 9106, RFC 5869, and NIST SP 800-38D vectors verified.
* SEC-B01 .. SEC-B03: Key Separation verified.
* SEC-C01 .. SEC-C06: Tampering, AAD, and header verification tests passed fail-closed.
* SEC-D01 .. SEC-D02: CSPRNG nonce uniqueness verified.
* SEC-E01 .. SEC-E04: Anti-DoS KDF parameter bounding checks verified.
* SEC-F01 .. SEC-F02: Deterministic `ZeroMemory` key cleanup and cancellation on lock verified.
* SEC-G01 .. SEC-G02: Zero plaintext binary canary scans across disk (.db / -wal) and log streams verified.
* Status: **Milestone M1 (Secure Vault Core) All Acceptance and Exit Criteria Met.**

---

## 4. Decisions & Active ADRs
* `docs/THREAT_MODEL.md` (v1.0) active.
* `docs/VAULT_FORMAT.md` (v1.0) active.
* `docs/SECURITY_TEST_MATRIX.md` (v1.0) active.
* `docs/adr/ADR-001.md` (Stack and Toolchain) active.
* `docs/adr/ADR-002.md` (Cryptographic Vault Format, Key Hierarchy, and Nonce Policy) active.
* `docs/adr/ADR-003.md` (Argon2id Parameters, Package Selection, and Bound Validation) active.
* `docs/STORAGE_AND_SINGLE_INSTANCE.md` (Storage, ACL, and Single Instance Architecture) active.

---

## 5. Next Milestone & Subtask
* **Milestone M2 — Credential Management & Secure UX**
  - **M2.1:** Domain login model, categories/favorites, and payload schema versioning.

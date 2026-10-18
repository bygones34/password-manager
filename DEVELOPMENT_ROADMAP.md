# Windows Password Manager — Development Roadmap

Version: 1.0 • Date: October 5, 2026 • Language: English

This document defines the product and engineering roadmap for the Windows password manager being developed in small, verifiable increments. Acceptance criteria defined here represent mandatory requirements for all implementation work. Since the final product name is pending, the code name `PasswordManager` is used throughout. `AGENTS.md` at the repository root defines operational rules and security contracts.

## 1. Product Goal & Scope

Goal: A modern, local-first password manager for Windows 11 featuring a sleek primary interface, a quick-access mini-vault in the notification area (system tray), and user-triggered autofill for verified applications.

The "system tray" is the notification area on the right side of the Windows taskbar. The primary objective is a dedicated mini-window that opens when clicking the tray icon; it is not an embedded toolbar/deskband or an intrusive Windows shell extension.

### User Experience

- Main window: Left navigation view, central records list, right-hand details pane for the selected account.
- Record management: Add, edit, delete; real-time search, favorites, categories, and secure password generation.
- Tray window: Lock status, unlocked account count, applications, recent items, and quick search.
- Contextual suggestions matching the active application; user-initiated credential fill only after target identity and account are validated.
- Locked state defense: When the vault is locked, account titles, usernames, domains, categories, recent items, and record counts are never shown. Only lock status and an unlock action are presented.
- Fully offline v1: The initial release functions completely offline. Third-party favicon/icon retrieval services are forbidden; account domain names are never leaked externally.

### Phases

| Phase | Milestone Scope | Usage Boundary |
| --- | --- | --- |
| A — M0–M3 | Secure core, modern UI, restorable backups | Synthetic records initially; controlled personal trial at M3 completion |
| B — M4–M6 | Tray mini-vault, tested Windows autofill, hardening | Personal desktop MVP; supported applications explicitly listed |
| C — M7 | Optional Windows Hello integration | Enabled only after cryptographic hardware key isolation is proven |
| D — M8 | Edge/Chrome browser integration | Distinct security audit and extension isolation gate |
| E — M9–M10 | Packaging, independent security audit, release readiness | Public release only after all exit criteria and audits pass |
| F — M11+ | Optional encrypted cloud sync & other platforms | Separate product project; does not block initial releases |

Payment cards, TOTP authenticator, passkey management, file attachments, item sharing, team vaults, mobile companion apps, Firefox support, and cloud synchronization are explicitly out of scope for v1. The domain model should not preclude them, but unnecessary abstractions must not be built prematurely.

## 2. Technical Foundation & Decision Gates

| Area | Initial Choice | Rationale & Verification |
| --- | --- | --- |
| Runtime | .NET 10; current supported servicing release | SDK pinned via `global.json`; WinUI toolchain verified on physical Windows in M0 |
| UI | WinUI 3 + Windows App SDK + MVVM | Modern Fluent Design; Mica material only where supported, opaque fallback |
| Target OS | Supported Windows 11 x64 | Minimum OS build and matrix established in ADR-001; ARM64 planned later |
| Storage | SQLite + EF Core | Single local vault; SQLite file is not assumed to be fully encrypted natively |
| Record Encryption | .NET `AesGcm`, AES-256-GCM | 32-byte key, 12-byte nonce, 16-byte tag; envelope protocol specified in M1 |
| Key Derivation | Argon2id | Evaluated based on maintenance, licensing, test vectors, and platform performance |
| Key Hierarchy | Random vault root key + wrapped key encryption | Master password rotation does not require re-encrypting all records |
| Native Integration | Minimal Win32 P/Invoke | Tray lifecycle, window identity, session lock events, and clipboard formats |
| Windows Autofill | UI Automation + Application Adapters | Not all controls supported; compatibility tested rather than assumed |
| Browser | Edge/Chrome extension + Native Messaging host | Tab origin and frame context verified directly by the browser extension |
| Distribution | MSIX candidate | Tray, native host registration, updates, and data paths verified in spikes; signed installer alternative via ADR |

Microsoft recommends WinUI 3 for new Windows client development [S1]. In this project, this choice was validated in M0. If any blocking WinUI/tray toolchain issues emerge, the team will not silently switch to WPF; the measured obstacle and WPF+Fluent alternative would be presented via ADR.

Specific package versions are never guessed. They are verified against official releases, pinned centrally in `Directory.Packages.props`, and audited. Floating versions and unwarranted framework upgrades are prohibited.

### Repository Structure

```text
PasswordManager.sln
AGENTS.md
DEVELOPMENT_ROADMAP.md
README.md
global.json
Directory.Build.props
Directory.Packages.props
.editorconfig
src/
  PasswordManager.Domain/
  PasswordManager.Application/
  PasswordManager.Security/
  PasswordManager.Infrastructure/
  PasswordManager.Platform.Windows/
  PasswordManager.Autofill.Windows/
  PasswordManager.Desktop/
  PasswordManager.NativeHost/            # Added in M8
extensions/
  chromium/                              # Added in M8
tests/
  PasswordManager.Domain.Tests/
  PasswordManager.Application.Tests/
  PasswordManager.Security.Tests/
  PasswordManager.Infrastructure.Tests/
  PasswordManager.Windows.Tests/
  PasswordManager.EndToEnd.Tests/
test-apps/
  AutofillTestApp/                        # Synthetic target added in M5
docs/
  PROJECT_STATUS.md
  THREAT_MODEL.md
  VAULT_FORMAT.md
  SECURITY_TEST_MATRIX.md
  MANUAL_TESTS.md
  AUTOFILL_COMPATIBILITY.md
  BACKUP_AND_RECOVERY.md
  adr/
```

Projects are created only when required by the active milestone; empty scaffolds for future milestones are avoided. Microservices, Web APIs, and local HTTP servers are completely excluded from the desktop architecture.

### Dependency Flow

Domain has zero project dependencies. Application depends only on Domain and defines port interfaces. Security implements cryptographic ports; it knows nothing of UI, EF Core, or Win32. Infrastructure implements storage ports defined by Application. Platform.Windows provides native OS services. Autofill.Windows depends on narrow autofill ports and UI Automation; it never accesses the database or master keys. Desktop serves as the composition root, driving workflows through Application use cases. NativeHost will later act as an IPC gateway; it never opens a vault directly.

Interfaces are introduced strictly across architectural boundaries or where required for test isolation. Boilerplate interfaces, premature generic frameworks, and mandatory CQRS/MediatR pipelines are prohibited.

## 3. Threat Model & Security Contract

### Protected Assets

Master password, cryptographic keys, record payloads, account metadata, backup snapshots, active vault session state, and autofill target authenticity.

### Security Goals & Mitigations

| Threat | Expected Mitigation | Boundary / Limitation |
| --- | --- | --- |
| Database / backup file theft | Authenticated encrypted payload + high-cost Argon2id KDF | Weak master passwords remain vulnerable to offline guessing |
| Record or header tampering | AEAD tag verification and authenticated context validation | Complete rollback to an earlier valid snapshot cannot be prevented purely locally in v1 |
| Record substitution / reordering | VaultId, RecordId, type, and version bound via AAD | Mass deletion of records or database destruction constitutes an availability attack |
| Autofill into spoofed window | Strict target binding, verification, and re-verification before write | A compromised target application can observe credentials written to it |
| Clipboard history leakage | OS clipboard history / cloud sync exclusion + conditional timer wipe | Cannot guarantee protection against kernel-level or privileged third-party keyloggers |
| UI data leakage when locked | Cache wiping, lists cleared, search reset, and pending tasks canceled | Cannot guarantee clearing of all OS compositor / GPU video memory copies |
| Same-user malware / admin attack | Reduce exposure time and attack surface | No security boundary claimed against malware or elevated admin running in the same user session |
| Malformed or hostile imports | Bounded parameter checks, format validation, atomic swap | While cryptographic integrity holds, availability must be guarded against resource exhaustion (DoS) |

"RAM only" denotes that the application intentionally never writes plaintext secrets to disk. Due to immutable managed strings, garbage collector copies, OS pagefiles, crash dumps, and hibernation files, absolute zero-disk guarantees cannot be made. OS disk encryption (e.g., BitLocker) is complementary and does not replace the application's internal cryptography.

### Mandatory Rules from M1 Onward

1. Master passwords are never persisted, logged, or sent to telemetry or clipboard. UTF-8 encoding rules are fixed; no silent trimming or Unicode normalization is performed.
2. Record titles, usernames, passwords, URLs/domains, notes, categories, application bindings, and timestamps reside strictly inside encrypted payloads. No plaintext FTS or search indexes. Random record IDs, format/KDF markers, ciphertext lengths, and physical row counts are documented metadata exposures.
3. Nonces are generated fresh for every encryption using a cryptographically secure pseudo-random number generator (CSPRNG) [S3]. Nonce reuse under the same key is strictly prohibited across retries, restores, and edits.
4. Cryptographic code is incomplete without explicit specifications for key separation, serialization, and AAD construction. Custom ciphers, XOR schemes, ECB mode, and silent algorithm downgrades are prohibited.
5. In the event of an authentication tag failure, all data is withheld. Partially decrypted records are never returned to the UI. Error reports never include plaintext.
6. All sensitive operations are bound to a specific session generation. Vault lock immediately aborts pending decrypt, reveal, and fill tasks; delayed task completions cannot deliver data to the UI.
7. Logging is strictly limited to an allowed whitelist (event codes, elapsed duration, generic status). Dumping DTOs/entities, EF sensitive data logging, SQL parameter output, and secret-containing exception details are prohibited.
8. The application runs with standard user privileges. UAC prompts, secure desktop, OS sign-in dialogs, and elevated administrator windows are outside the autofill scope.
9. Autofill and credential submission are never executed automatically without user initiation. Password generation must use a CSPRNG.
10. M0–M2 are tested exclusively with synthetic data. Real credentials must not be imported before M3. The M3 gate is not a commercial security certificate; users should not delete existing backups in existing password managers.

## 4. Vault & Key Architecture

### Key Lifecycle

Master password + random salt + stored Argon2id parameters → 32-byte Key Encryption Key (KEK). A CSPRNG independently generates a 32-byte vault root key. The KEK is used exclusively to wrap/unwrap this root key via AES-256-GCM. Using standard HKDF-SHA-256, dedicated record and manifest encryption keys are derived from the root key using distinct domain-separation labels and the VaultId. Precise encoding and label values are finalized in `VAULT_FORMAT.md`.

During vault unlock: header bounds are verified, KEK is derived, the wrapped root key is authenticated and decrypted, and the manifest is decrypted. Only then is the session transitioned to `Unlocked`. Incorrect passwords and corrupted headers produce uniform, safe authentication failure responses. No separate plaintext master-password verifier exists, and the KEK is never persisted.

Changing the master password generates a new salt, derives a new KEK, and re-wraps the existing root key. Records do not need to be re-encrypted. This change does not invalidate previously stolen backups or database copies. A true root key rotation requires re-encrypting all records, but cannot erase historic snapshots in an attacker's possession.

### KDF Policy

Baseline profile: Argon2id v1.3, 64 MiB memory cost, 3 iterations, 4 parallelism lanes, minimum 16-byte salt, and 32-byte output. This baseline derives from the RFC 9106 low-memory recommendation [S2]; it is not a blanket guarantee for all hardware. Unlock latency and memory consumption are measured on the target system and finalized in ADR-003. Local performance optimizations may never drop below the established cryptographic floor.

KDF parameters read from file headers are untrusted. Prior to executing the KDF, strict bounds checks are enforced for memory limits (maximum 1 GB), iterations, parallelism, total file size, and payload lengths to prevent denial-of-service (DoS) attacks from malformed files. KDF upgrades occur only upon successful unlock and verified atomic header updates.

### Persistent Format Outline

| Structure | Unencrypted Envelope Fields | Authenticated Encrypted Payload |
| --- | --- | --- |
| VaultHeader | Magic (`0x50574D52`), format version, VaultId, cipher/KDF IDs, bounded KDF parameters, salt, wrapped key nonce, tag, ciphertext | Vault root key (32 bytes) |
| VaultManifest | Nonce, tag, ciphertext | Vault preferences, category definitions, expected record ID sets, integrity metadata |
| VaultRecord | RecordId, envelope version, nonce, tag, ciphertext | Record type, title, account credentials, domain/app bindings, favorites, timestamps, revision |

Additional Authenticated Data (AAD) binds the VaultId, RecordId (for records), domain label, and envelope version via canonical binary encoding. Key-wrap AAD binds header metadata, salt, and cipher/KDF identifiers. Manifest and record mutations execute in the same SQLite transaction. The manifest detects record deletions and substitutions, but cannot prevent whole-database rollback to an earlier valid snapshot.

A 96-bit random nonce policy is documented with safe usage bounds, collision probabilities, and root-key rotation thresholds. Nonce collision tracking may be employed, but is not represented as an absolute mathematical uniqueness guarantee across historical snapshots.

SQLite stores solely encrypted envelopes. Plaintext credentials are never mapped to EF Core entities. Synthetic canary tokens are used to scan WAL/SHM files, temporary files, logs, and backups to confirm zero plaintext leakage. Salt, nonces, and tags are expected in the database file; they are public cryptographic metadata, not secrets.

### Session & Concurrency

State machine: `NoVault`, `Locked`, `Unlocking`, `Unlocked`, `Locking`, `Faulted`. Concurrent unlock attempts are rejected. Sensitive operations utilize bounded operation scopes with session generation tracking and cancellation tokens. Locking halts new work, cancels in-flight operations, cuts off delivery to the UI, and wipes key buffers and decrypted caches. Mutations are serialized; multiple application instances cannot write independently to the same database.

Deterministic memory zeroing (`CryptographicOperations.ZeroMemory`) and proper `IDisposable` patterns are applied to controlled byte buffers. The application does not rely on `SecureString` or claim to eliminate all transient copies in managed memory. Necessary UI bindings, reveal windows, and clipboard transfers minimize exposed copies and document residual risks.

## 5. Modern UI & Behavioral Standards

Primary interface: WinUI 3 `NavigationView` + records list + details pane. In narrow window configurations, the details view transitions to a separate sub-page. Vault creation, onboarding, unlock, record forms, password generator, settings, and backup management are implemented as distinct flows.

- Theme: System, Light, and Dark modes; consistent spacing, typography, corner radii, and semantic design tokens.
- UI strings are maintained in resource files (`.resw`) prepared for localization. Turkish and other language localizations are scheduled for the product phase. Code identifiers, comments, and project documentation are in English.
- Accessibility: Full keyboard navigation, visible focus indicators, screen reader compatibility, high contrast mode, dynamic DPI scaling (100%, 150%, 200%), and small display support.
- Password masking: Masked by default. Password reveal times out after 10 seconds and closes immediately upon focus loss or vault lock. Plaintext passwords are never exposed to the UI Automation tree.
- Clipboard: Sensitive clipboard items are cleared after 20 seconds. Clearing occurs conditionally only if the clipboard still contains the application's data sequence; subsequent user copies are preserved.
- Windows clipboard history and cloud sync exclusion formats are applied [S4]; clipboard logger immunity is never claimed.
- Auto-lock defaults to 5 minutes of user inactivity, managed via a testable virtual clock. Windows session lock, sleep, user switching, and system suspend immediately lock the vault. Resume requires re-authentication.
- Window close defaults to locking the vault and minimizing to the system tray. Explicit Exit is provided. Startup with Windows is opt-in, and the app always launches in the locked state.
- Generator previews, copied passwords, and sensitive unsaved form drafts are cleared on lock events. Unsaved sensitive forms are never auto-saved to disk.

## 6. Milestone Plan

Each subtask represents an isolated, reviewable development cycle. Order of execution strictly respects dependencies. The checklists below reflect project progress.

### M0 — Foundation & Technical Feasibility

Goal: A running Windows 11 application with verified toolchains and native capability spikes.

- [x] M0.1 Product scope, OS/x64 target, repo setup, and Threat Model v0.
- [x] M0.2 SDK, Visual Studio, Windows SDK, and Windows App SDK versions verified; ADR-001 stack/toolchain written.
- [x] M0.3 Solution structure, core projects, centralized package management, nullable/analyzers, and `.gitignore`.
- [x] M0.4 WinUI shell: Light/Dark theme, `NavigationView`, synthetic record list/detail view, and design tokens.
- [x] M0.5 Native spike: System tray icon, flyout window positioning, DPI handling, foreground HWND capture, and session event subscription.
- [x] M0.6 SQLite file paths, ACL permissions, and single-instance behavior; packaged vs. unpackaged differences documented.
- [x] M0.7 Windows CI build and test pipeline established; cross-platform core tests operational.

Exit Gate: Clean startup on Windows 11; all tests pass; SDK versions, tray feasibility, and packaging strategies documented. No real credentials or mock security bypasses.

### M1 — Secure Vault Core

Goal: Binary cryptographic format and create/unlock/lock lifecycle.

- [x] M1.1 Threat Model v1, `VAULT_FORMAT.md` v1, and Key Derivation ADRs completed.
- [x] M1.2 Argon2id package selection, RFC test vectors, performance benchmarks, and bounded parameter validation.
- [x] M1.3 CSPRNG nonce generator, AES-256-GCM key wrap, HKDF key separation, and record/manifest AEAD envelopes.
- [x] M1.4 Encrypted envelope database schema, EF Core migrations, and schema vs. crypto format version separation.
- [x] M1.5 Create, unlock, and lock lifecycle state machine, sensitive operation scopes, key disposal, and cancellation.
- [x] M1.6 Auto-lock inactivity timer, Windows session lock / suspend listeners, and logging whitelist enforcement.
- [x] M1.7 Security Exit Gate verification suite and physical database/WAL/log canary scans.

Exit Gate: Valid master password unlocks vault; invalid password fails closed; tampered ciphertext, tags, AAD, or headers fail closed; record substitution between vaults rejected; repeated encryptions produce unique nonces; malformed headers rejected before KDF execution. Delayed operations do not deliver to UI after lock. UI cannot bypass core security checks.

### M2 — Credential Management & Secure UX

Goal: Comprehensive account management within the modern WinUI interface.

- [ ] M2.1 Domain login model, categories, favorites, and payload schema versioning.
- [ ] M2.2 Add, update, and delete operations; intentional blank password handling; field length limits.
- [ ] M2.3 In-memory record search in unlocked state; purge search index and results immediately on lock.
- [ ] M2.4 Record detail view, masked password reveal, independent username and password copy actions.
- [ ] M2.5 CSPRNG password generator with customizable character sets, minimum rule guarantees, and unbiased rejection sampling.
- [ ] M2.6 Clipboard timeout and history exclusion, reveal timeout, and sanitized notification messages.
- [ ] M2.7 Safe URL launcher: strictly allow `http`/`https` schemes; prevent execution of local files or arbitrary commands.
- [ ] M2.8 Dynamic DPI scaling, theme switching, high contrast, screen reader compatibility, and empty/loading states.

Exit Gate: Synthetic records persist across application restarts; metadata invisible when locked; search index never writes plaintext to disk; clipboard cleanup does not clobber newer user data; reveal timers and edit drafts reset on lock; keyboard, screen reader, and DPI checks pass.

### M3 — Backup, Restore & Data Durability

Goal: Verified snapshot backup, atomic restore, and corruption resilience prior to personal use.

- [ ] M3.1 Encrypted snapshot backup format with portable master-password key wrapping.
- [ ] M3.2 SQLite online backup API for consistent snapshot generation without losing active WAL changes.
- [ ] M3.3 Import validation: bounded file sizes, format versions, KDF limits, and staging verification before commit.
- [ ] M3.4 Atomic replace restore: existing vault is preserved until imported snapshot is completely validated. Merge deferred.
- [ ] M3.5 Master password rotation and KDF parameter upgrades; transactional rollback verification.
- [ ] M3.6 Pre-migration backups and restore verification; read-only rejection of unsupported newer schema versions.
- [ ] M3.7 Failure-mode resilience: disk full, permissions errors, sudden termination, missing WAL, and duplicate instance conflicts.
- [ ] M3.8 `BACKUP_AND_RECOVERY.md` guide and cross-device/profile restore verification drill.

Exit Gate: Full record recovery verified on an independent test profile with correct master password; invalid passwords or corrupt snapshots do not touch the active vault; canceled restores preserve original data; new password unlocks vault while old password fails.

**Personal Trial Gate:** M1–M3 tests pass + session locking verified + canary scans negative + restore drill verified on a secondary machine. Only after passing this gate may limited personal credentials be stored; single-copy storage is prohibited.

### M4 — System Tray Mini-Vault

Goal: Fast-access companion window sharing the main application's secure session.

- [ ] M4.1 Production tray lifecycle: Explorer restarts, DPI transitions, and single-instance activation.
- [ ] M4.2 360–420 DIP flyout window; smart screen/taskbar edge clamping; accessible focus and Escape key dismissal.
- [ ] M4.3 Locked state view; unlocked record count, favorites, recent items, search, copy actions, generator, and open main vault.
- [ ] M4.4 Target window capture prior to opening tray flyout to provide context-sensitive account suggestions.
- [ ] M4.5 Global shortcut registration and conflict handling, startup opt-in, minimize-to-tray, and clean exit.
- [ ] M4.6 State synchronization and lock-race protection between tray flyout and main window.

Exit Gate: Unified session state; locking from tray clears main window immediately; tray icon survives `explorer.exe` restart; accessible on multi-monitor setups; unlock does not auto-fill previously captured target without re-validation.

### M5 — Windows Autofill: Controlled Pilot

Goal: User-initiated autofill for verified target applications.

- [ ] M5.1 Test-only synthetic WPF/Win32 target application: standard fields, masked fields, multi-account scenarios, focus changes, and latency.
- [ ] M5.2 `TargetDescriptor`: HWND, PID, process start identity, executable path, signature verification, session ID, control descriptors, and capture timestamp.
- [ ] M5.3 Encrypted `ApplicationBinding`. Executable basenames (e.g., `steam.exe`) or window titles alone are insufficient trust identifiers.
- [ ] M5.4 UI Automation control capability inspection; verify `ValuePattern` support on live targets. Password field support is never assumed.
- [ ] M5.5 Fill Username / Fill Password / Fill Both actions; clear account and target details shown to user. Single-use, expiring fill tokens.
- [ ] M5.6 Target re-verification upon returning focus: process identity, HWND, and control checks. Mismatches, PID reuse, modal dialogs, locks, or timeouts abort fill.
- [ ] M5.7 UI Automation thread isolation with strict timeouts to prevent unresponsive targets from hanging the UI thread.
- [ ] M5.8 `AUTOFILL_COMPATIBILITY.md` matrix: OS build, application version, control pattern, support status, and test date.
- [ ] M5.9 Controlled manual pilot with synthetic credentials on two real-world target applications chosen by user.

Fill Both is non-atomic. If target focus changes after writing username, password writing is aborted immediately. Each sensitive write re-verifies the target. Control values are not read back. Automatic Enter or submission is prohibited.

No arbitrary input injection fallback (e.g., blind `SendInput`) in the initial pilot. Unsupported targets offer clear guidance and manual copy buttons. SendInput will require a distinct ADR and strict failure-mode testing; no privilege elevation to administrator or UIAccess bypasses.

Microsoft documents restrictions on automated input into Windows authentication dialogs introduced in 2026 security updates [S7]. These do not block standard desktop application login screens; Windows OS credential and sign-in dialogs are explicitly excluded from scope.

Exit Gate: Flawless execution on synthetic target app; **zero password dispatch** during spoofed targets, focus races, or vault lock; verified compatibility reports for selected pilot apps. Unverified apps (Steam, Discord, Visual Studio) are never advertised as supported without tests. Web form autofill is excluded from this milestone.

### M6 — Personal Desktop MVP Hardening

Goal: Polish and harden M1–M5 for robust daily personal desktop use.

- [ ] M6.1 Extended runtime stress testing and repeated lock/unlock cycles; memory and event subscription leak profiling.
- [ ] M6.2 Application exit, OS sleep, resume, lock screen, user switching, reboot, and crash recovery testing.
- [ ] M6.3 Concurrency stress testing across clipboard, password reveal, autofill, and asynchronous navigation.
- [ ] M6.4 Cryptographic envelope and backup parser fuzz testing; hostile KDF parameters and extreme payload lengths.
- [ ] M6.5 Root-key rotation procedure and tooling; documentation of backup implications.
- [ ] M6.6 Dependency security vulnerability and licensing audit; log and crash dump sanitization review.
- [ ] M6.7 Local unlock retry delay, cancellation handling, and anti-DoS defense. (Not claimed as protection against offline attacks).
- [ ] M6.8 Performance benchmarks for startup, search, tray activation, and unlock across synthetic databases of 1,000 and 10,000 records.
- [ ] M6.9 Comprehensive documentation of known limitations and maintenance/backup routines.

Exit Gate: All test suites in the security matrix pass; all critical and high-severity issues resolved; personal backup restore drill successfully repeated; supported autofill matrix published. Desktop MVP is fully operational without requiring Windows Hello or browser extensions.

### M7 — Optional Windows Hello Integration

Goal: Convenient on-device vault unlocking while preserving the portable master-password recovery path.

- [ ] M7.1 API feasibility spike and ADR: Establishing cryptographic key protection via Windows Hello vs. simple user presence consent.
- [ ] M7.2 Opt-in enrollment requiring master password verification; device-bound protected key architecture.
- [ ] M7.3 Edge case handling: Hello unavailable, user cancellation, biometric lockout, PIN change, TPM/hardware profile changes, and fallback to master password.
- [ ] M7.4 Revocation and device re-authorization flows upon master password change or root-key rotation.
- [ ] M7.5 Cross-device backup isolation: Hello credentials are device-specific and never included in portable backups.

`UserConsentVerifier` provides an OS authentication check [S5]. Standard DPAPI `CurrentUser` does not provide Hello-gated cryptographic isolation against other processes running in the same user session [S6]. A simple "Hello success → DPAPI decrypt" flow is merely a convenience feature, not hardware-protected key isolation. If hardware-backed key protection is required, it must bind key operations directly to the OS TPM/Hello subsystem via approved APIs. If standard models are not feasible, this milestone will be deferred; custom cryptographic primitives derived from digital signatures are prohibited.

Exit Gate: Threat model updated; actual key protection mechanism documented in an ADR; bypass and fallback paths verified. Inability to use Hello never impairs the master-password workflow.

### M8 — Edge & Chrome Browser Integration

Goal: User-initiated autofill bound strictly to validated web origins and frames.

- [ ] M8.1 Manifest V3 extension, minimal host permissions, and architectural review ADR.
- [ ] M8.2 Native Messaging host registration spike: package identity and Store vs. unpackaged differences.
- [ ] M8.3 Versioned bounded message schema: pairing handshakes, request IDs, expiration, replay defense, and session generations.
- [ ] M8.4 Host-to-Desktop IPC: user-scoped ACLs, process identity validation, and strict boundaries. No open local HTTP ports.
- [ ] M8.5 Origin verification: background service worker validates tab and frame origins; page-supplied URLs are never trusted.
- [ ] M8.6 Strict exact-origin matching by default: scheme, Punycode hostname, and effective port. Related domains require explicit user association.
- [ ] M8.7 Security tests: `example.com.evil.test`, port variations, HTTP downgrades, IDN homograph attacks, and iframe navigation races.
- [ ] M8.8 Multi-step login flows, Single Page Applications (SPA), and Shadow DOM compatibility; cross-origin iframes blocked by default.
- [ ] M8.9 Extension storage, logs, and crash reports contain zero secrets; entire vault is never transmitted to the browser extension.
- [ ] M8.10 Spoofed host processes, malformed messages, expired session tokens, and sudden browser termination tests.

Native Messaging transports and `allowed_origins` alone do not provide a cryptographically secure channel against local malicious processes running under the same user account [S8, S9]. Additional pairing authorization is required; complete immunity from same-user malware cannot be guaranteed. Plaintext credentials written into web forms are visible to scripts on that page.

Exit Gate: Autofill requires explicit user action; lock states stay synchronized between browser and desktop; stale requests rejected across page navigation; zero secrets sent to invalid origins or frames; incognito disabled by default; minimal permissions audited. Firefox support deferred to a future milestone.

### M9 — Packaging & Controlled Beta Readiness

Goal: Clean installation, update, and uninstallation workflows on clean Windows systems.

- [ ] M9.1 MSIX packaging decision; tray startup, native host registration, and clean uninstall verification.
- [ ] M9.2 Production release CI pipeline: locked dependencies, Software Bill of Materials (SBOM), reproducible artifact hashes, and private key security.
- [ ] M9.3 Code signing and package verification; updates rely on trusted platform mechanisms rather than custom updaters.
- [ ] M9.4 Pre-update automated encrypted backups; migration failure rollback tests and forward compatibility policy.
- [ ] M9.5 Clean VM testing: fresh install, version upgrade, and clean uninstall; user vault database is never deleted without explicit consent.
- [ ] M9.6 Documentation: updated README, user security guide, support sanitization guide, and `SECURITY.md`.
- [ ] M9.7 Private beta test suite with synthetic datasets; distribution channel and documented limitations.

Exit Gate: Verified release artifact; zero signing secrets in source or CI logs; upgrade and restore drills succeed from earlier milestones. The user provides signing identities; synthetic signatures are never misrepresented as production trust.

### M10 — Productization & Public Release Gate

Goal: Technical and operational readiness for a publicly distributed password manager.

- [ ] M10.1 Independent third-party security audit: cryptographic format, key lifecycle, autofill trust boundaries, Hello, and extension IPC.
- [ ] M10.2 Audit remediation and re-testing; any critical or high vulnerabilities block release.
- [ ] M10.3 Legal & compliance: `LICENSE`, third-party license notices, trademark review, and distribution rights.
- [ ] M10.4 Privacy policy and telemetry design. Default: zero telemetry and zero secrets; optional crash reports require explicit opt-in and redaction.
- [ ] M10.5 Incident response process, vulnerability disclosure policy, security update SLA targets, and designated maintenance owners.
- [ ] M10.6 Clear user education regarding account recovery limitations, backup responsibilities, supported platforms, and security boundaries.
- [ ] M10.7 Regional regulatory, software liability, and consumer protection assessments; compliance claims are not made without legal review.
- [ ] M10.8 Release candidate test matrix execution and open issue review; product owner formal sign-off.

Exit Gate: External security audit report and proof of remediation; successful restore and upgrade tests; verified release packages and long-term maintenance plan. High unit test coverage or using AES does not prove a system is secure. Unsubstantiated claims of "zero-knowledge," "certified," or "malware-proof" are strictly forbidden.

## 7. M11+ — Optional Cloud Sync & Expansion

The local-first v1 release stands as a complete product. To support future cloud sync, the core maintains immutable VaultIds, payload versioning, and portable key wrapping; no backend services are deployed at this time.

### Future Cloud Sync Architectural Requirements

- Account identity on the sync server is distinct from vault unlock keys. Server login tokens or MFA do not grant access to vault decryption keys.
- Client-side end-to-end encryption: server never receives plaintext records, master passwords, or KEKs. Standard, audited authentication protocols (e.g., SRP/PAKE) must be used rather than custom implementations.
- Device enrollment, revocation, key distribution, recovery mechanisms, and multi-device master password rotation.
- Ciphertext versioning, tombstone markers, conflict resolution, and idempotent synchronization. "Last-write-wins" must not be adopted without evaluating data loss risks.
- Defenses against malicious servers returning stale snapshots, deleting records, or presenting diverging histories across devices. AEAD alone does not prevent rollback or fork attacks.
- Metadata privacy: minimal server-side exposure of user IDs, ciphertext sizes, access timestamps, and IP addresses.
- Transport security, access controls, rate limiting, quotas, and incident management are distinct from cryptographic payload security.
- Comprehensive external security audit. Local v1 audit certifications do not transfer to cloud synchronization architectures.

Subsequent expansions (mobile companion apps, Firefox support, TOTP, passkeys, item sharing) will be planned with dedicated threat models. Clean separation of Windows UI from core libraries preserves portability without premature abstraction.

## 8. Testing & Verification Matrix

| Layer | Required Test Focus | Execution Environment |
| --- | --- | --- |
| Security | RFC test vectors for Argon2/HKDF/AEAD, tampering, AAD substitution, invalid passwords, nonces, hostile headers | Cross-platform runner + Windows verification |
| Application | State transitions, lock races, cancellation, clipboard ownership, virtual clock, validation | Unit tests, test doubles for platform ports |
| Infrastructure | Physical SQLite transactions, WAL canary scans, migrations, I/O failure injection | Temporary isolated test directories |
| Backup | Restore on clean environments, incorrect password rejection, truncated files, schema upgrades, rollback | Isolated test profiles / VMs |
| Windows | Tray lifecycle, session lock/suspend events, DPI scaling, UI Automation target/focus integrity | Physical Windows, synthetic test target |
| Browser | Origin, frame, and navigation checks; IPC message validation, replay defense, minimal permissions | Physical Chrome/Edge + local test pages |
| Release | Clean installation, version upgrade, uninstall cleanup, code signatures, restore from backup | Clean Windows VM |

Tests must verify actual security behavior rather than mocking out the core logic. "Test passed" is recorded only with actual command output. Windows and UI events not validated in automated suites are flagged as `Manual verification pending`.

### Definition of Done for Milestones

- All acceptance criteria satisfied; all regression tests passing.
- `git diff --check`, build, and test runs completed without warnings or errors.
- Architecture Decision Records (ADRs), project status, security test matrix, and manual test logs updated.
- Zero secrets, real passwords, personal credentials, or signing keys in code, commits, or logs.
- Format updates accompanied by backward compatibility and migration proofs.
- Any unexecuted Windows or device checks explicitly listed as pending manual verification.
- Known limitations and residual risks documented; future milestones not assumed complete.

## 9. Codex Workflow & Handoff

Place `AGENTS.md` and this document at the repository root. Tasks start from M0.1–M0.2. Large milestones must never be assigned as monolithic tasks. The agent reads the current status, ADRs, and repository state, selecting the smallest open subtask.

At the conclusion of each subtask, report: behavioral changes, modified files, exact command lines and actual results, manual test steps, blockers, and the recommended next task. Keep documentation updated. Staging, commits, pushes, merges, and releases require explicit user authorization in the active session.

### Initial `docs/PROJECT_STATUS.md` Template

```markdown
# Project Status
Last updated: <date>
Current milestone: M0
Current task: M0.1–M0.2
Completed: None
Verified toolchain: Not yet verified
Build/test evidence: Not run
Manual checks: Pending
Decisions/ADRs: None
Known risks/blockers: Windows environment/toolchain verification pending
Next task: Record product scope and run Windows toolchain spike
```

### Initial Agent Prompt

```text
Read AGENTS.md and DEVELOPMENT_ROADMAP.md completely.
The objective of this session is strictly subtasks M0.1 and M0.2.
Inspect the existing repository and toolchain; record product scope and Windows target.
Verify .NET 10 / WinUI 3 / Windows App SDK compatibility in the actual environment.
Create initial THREAT_MODEL.md, PROJECT_STATUS.md, and toolchain ADR.
If the environment is non-Windows, complete core preparation and mark Windows verification as pending.
Do not write production code, crypto, autofill, or cloud components for future milestones.
Preserve user files; use synthetic test credentials only.
Report changes, test evidence, blockers, and recommendations for M0.3 upon completion.
This prompt does not grant commit, push, merge, or release authority.
```

Subsequent prompt pattern: "Review status and ADRs; implement strictly subtask M1.3; adhere to the approved `VAULT_FORMAT.md` specification; execute test vectors and tampering test suites; report results before starting M1.4."

## 10. Architecture Decision Records (ADRs)

| ADR | Decision Focus | Target Milestone |
| --- | --- | --- |
| 001 | Target OS, SDK, WinUI toolchain, UI fallback, and packaging candidate | M0 |
| 002 | Cryptographic format, root key / KEK separation, and AAD design | Prior to M1 |
| 003 | Argon2id provider, parameters, benchmarks, and parser bounding | M1 |
| 004 | Session state, lock lifecycle, plaintext buffer zeroing, and concurrency | M1 |
| 005 | SQLite encrypted envelope, manifest, transactions, and nonce policy | M1 |
| 006 | Backup/restore format, master password change, rotation, and migration | M3 |
| 007 | Autofill target trust model, verification, and fallback boundaries | M5 |
| 008 | Windows Hello key protection mechanisms and guarantee limits | M7 |
| 009 | Browser extension origin policy, pairing, and native IPC | M8 |
| 010 | Packaging, code signing, updates, and release policies | M9 |
| 011 | Sync protocol, device enrollment, recovery, and conflict resolution | Prior to M11 |

Each ADR includes context, chosen decision, considered alternatives, rationale, security implications, verification evidence, and review triggers. Areas not yet decided remain marked as "TBD"; decisions are never fabricated.

## 11. Technical References

References verified October 5, 2026. Roadmap sequencing, UX timeouts, and acceptance criteria represent project requirements. API and platform behaviors are re-verified during implementation.

- [S1 — Microsoft WinUI 3](https://learn.microsoft.com/en-us/windows/apps/winui/)
- [S2 — RFC 9106: Argon2](https://www.rfc-editor.org/rfc/rfc9106.html)
- [S3 — .NET AesGcm.Encrypt](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm.encrypt?view=net-10.0)
- [S4 — Microsoft Clipboard Formats](https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats)
- [S5 — UserConsentVerifier](https://learn.microsoft.com/en-us/uwp/api/windows.security.credentials.ui.userconsentverifier)
- [S6 — CryptProtectData / DPAPI](https://learn.microsoft.com/en-us/windows/win32/api/dpapi/nf-dpapi-cryptprotectdata)
- [S7 — Microsoft Windows authentication auto-fill restrictions, KB5080542](https://support.microsoft.com/en-us/servicing/os/windows/docs/2026/01/new-behavior-restricting-certain-applications-to-autofill-credentials-introduced-by-the-windows-janu)
- [S8 — Chrome Native Messaging](https://developer.chrome.com/docs/extensions/develop/concepts/native-messaging)
- [S9 — Chromium Extensions Security FAQ](https://chromium.googlesource.com/chromium/src/+/main/extensions/docs/security_faq.md)
- [S10 — Microsoft UI Automation security](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-securityoverview)
- [S11 — .NET releases and support](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support)

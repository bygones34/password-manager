# Threat Model — PasswordManager (v1.0)

* **Version:** 1.0 (Milestone M1.1 Update)
* **Date:** 2026-10-10
* **Status:** Approved / Active
* **Target Platform:** Windows 11 (x64)
* **Related Standards & Documents:** `VAULT_FORMAT.md`, `ADR-002`, `ADR-003`, `SECURITY_TEST_MATRIX.md`

---

## 1. System Definition & Scope

### 1.1. Product Goal
A modern, local-first password manager (`PasswordManager`) for Windows 11 featuring a sleek WinUI 3 interface, a system tray mini-vault, and user-initiated autofill.

### 1.2. Target Platform & Execution Environment
* **Operating System:** Windows 11 (x64 architecture).
* **Privilege Level:** Standard user (requires no UAC elevation or UIAccess privileges).
* **Network Status:** Functions entirely offline. No external favicon/icon download services or telemetry; account domain names and passwords are never transmitted across the network.
* **Storage Model:** SQLite storing AES-256-GCM encrypted envelopes. Zero plaintext database columns, search indexes, or logs.

### 1.3. Out of Scope (v1 Release)
* Cloud synchronization and remote server infrastructure (post-M10).
* Browser extensions (Chromium / Firefox) — out of scope until Milestone M8.
* Windows Hello / Biometric integration — out of scope until Milestone M7.
* Autofill on Windows Sign-in, UAC, or Secure Desktop screens.
* Payment cards, TOTP authenticator, Passkeys, and secure file attachments.
* Blind input injection via `SendInput`.

---

## 2. Protected Assets & Sensitivity Levels

| Asset ID | Asset Name | Description & Protection Level | Lifecycle & Storage Format |
| --- | --- | --- | --- |
| **A-01** | **Master Password** | Single primary password supplied by the user to unlock the vault. Highest confidentiality. | Never written to disk or logged; wiped from memory via `ZeroMemory` immediately after deriving the KEK. |
| **A-02** | **KEK (Key Encryption Key)** | Ephemeral 256-bit key derived from master password and salt via Argon2id. | Held in memory only while unlocking/locking to wrap/unwrap the root key; never persisted to disk. |
| **A-03** | **Vault Root Key** | Independent 256-bit primary key generated via CSPRNG. | Stored on disk wrapped with KEK using AES-256-GCM. Kept in memory only while vault is unlocked; zeroed immediately on lock. |
| **A-04** | **Record & Manifest Keys** | Dedicated 256-bit sub-keys derived from the Root Key via HKDF-SHA-256 using domain separation labels. | Derived ephemerally in memory; used to encrypt/decrypt records and manifests. |
| **A-05** | **Record Data (Credentials)** | Usernames, passwords, URLs, notes, titles, categories, favorites. | Stored on disk strictly inside `RecordKey`-encrypted envelopes. Decrypted in memory only upon active viewing. |
| **A-06** | **Vault Manifest & Integrity** | Vault configuration, record IDs (`RecordId`), and revision mappings. | Stored inside `ManifestKey`-encrypted envelope; detects record deletion and substitution. |
| **A-07** | **Active Vault Session** | In-memory session state, search caches, clipboard buffers. | Deterministically purged from memory on lock, system lock, or inactivity timeout. |

---

## 3. Threat Actors & Trust Boundaries

### 3.1. Threat Actors
1. **Passive At-Rest Attacker:** An adversary who obtains the database (`.db`), WAL files, or backup copies from local disk, removable media, or backup destinations.
2. **Active File Attacker (Tampering / Substitution):** An adversary who manipulates database files directly to delete records, substitute records, tamper with headers, or craft hostile KDF parameters to trigger Denial of Service (DoS).
3. **Shoulder Surfer / Casual Observer:** An observer viewing the display when the user steps away or when the vault is locked.
4. **Clipboard Monitor:** Untrusted third-party software monitoring the Windows clipboard to steal copied credentials.
5. **Same-User Malware:** Malicious processes running within the same standard user session capable of inspecting memory or dispatching window messages.

### 3.2. Trust Boundaries
* **Storage Boundary:** Database and filesystem store strictly encrypted envelopes. Plaintext never crosses into persistent storage. SQLite WAL, SHM, and temporary files remain encrypted.
* **Session & Memory Boundary:** When the vault is locked, cryptographic key material and decrypted records are wiped from memory. The UI layer cannot bypass core security controls to access database or key derivation APIs directly.
* **Autofill Target Boundary:** Desktop target window (HWND), process (PID, integrity level, binary signature), and focus context must be re-verified immediately before credential dispatch.

---

## 4. Threat Analysis, Mitigations & Boundaries

| Threat ID | Threat & Attack Scenario | Design Mitigation | Accepted Boundary / Residual Risk |
| --- | --- | --- | --- |
| **T-01** | Offline brute-force cracking of stolen database or backup file | Independent 256-bit CSPRNG Root Key; KEK derived with Argon2id v1.3 (64 MiB RAM, 3 iterations, 4 lanes, 32-byte salt); root key wrapped with AES-256-GCM. | Weak master passwords remain vulnerable to dictionary attacks. Password strength meters and complexity rules guide the user. |
| **T-02** | Tampering with records or header | AES-256-GCM 128-bit authentication tags and AAD context binding. Fail-closed on verification failure. | An attacker can corrupt files to make the vault unopenable (Denial of Service / availability attack). |
| **T-03** | Record substitution or cross-vault injection | Canonical AAD binding: `VaultId` (16B) \|\| `RecordId` (16B) \|\| `EnvelopeVersion` (uint32) \|\| Purpose. Records cannot be decrypted under another record ID or foreign vault key. | Whole-database deletion or rollback to an earlier valid snapshot cannot be detected purely locally in v1. |
| **T-04** | KDF Denial-of-Service via hostile header parameters | Header and import KDF parameters undergo strict bounding checks prior to execution (Memory: 16–512 MiB, Iterations: 1–10, Parallelism: 1–16). Out-of-bounds parameters rejected immediately. | Opening excessively large files is bounded by overall file size limits (e.g., 50 MB). |
| **T-05** | AES-GCM Nonce Reuse Catastrophe | Fresh 96-bit CSPRNG nonces generated for every encryption. Fixed IVs or deterministic counters are prohibited. | Birthday paradox bound: Under a single key, collision probability remains negligible ($< 2^{-32}$) up to $2^{32}$ encryptions. Root key rotation manages long-term risk. |
| **T-06** | Information leakage from memory or UI when locked | On vault lock, sensitive UI views, models, search indexes, and DTOs are cleared. Key buffers are wiped via `ZeroMemory`. In-flight decrypt/reveal tasks are aborted. | Immutable managed string copies retained by the .NET Garbage Collector and OS pagefile/crash dumps cannot provide an absolute 100% wipe guarantee. |
| **T-07** | Leakage via Windows clipboard history and cloud sync | `ExcludeClipboardContentFromMonitorProcessing` and cloud exclusion formats applied; clipboard conditionally cleared after 20 seconds. | Kernel-level keyloggers or aggressive clipboard hooks ignoring Windows APIs cannot be blocked. |
| **T-08** | Autofill dispatch into spoofed or fraudulent window | User-initiated fill only (no automatic submission). Target HWND, PID, executable path, and focus re-verified immediately before writing. | A compromised target application can misuse credentials legally dispatched to it. |
| **T-09** | Same-user malicious processes (Same-User Malware) | Session exposure minimized (5-minute inactivity lock); unsaved sensitive forms never written to disk; UI isolation maintained. | Malicious processes running with identical user privileges can read process memory or inject code; full isolation requires OS-level sandbox or separate accounts. |

---

## 5. Non-Negotiable Security Rules

1. **Plaintext Never Written to Disk:** Master passwords, decrypted account credentials, and symmetric keys are never written to disk, stored in unencrypted SQLite columns, logged, or placed in temporary files.
2. **Cryptographic Key Separation:** The KEK is used exclusively to wrap/unwrap the Root Key; it never encrypts records directly. Records are encrypted with `RecordKey`, and the manifest is encrypted with `ManifestKey`. Keys cannot be used interchangeably.
3. **Nonce Uniqueness:** Every AEAD operation utilizes a freshly generated 96-bit CSPRNG nonce. Re-encryption, edits, and restore operations strictly require fresh nonces.
4. **Strict Fail-Closed Principle:** Upon authentication tag mismatch, validation error, or KDF boundary violation, zero data is returned. Error messages do not leak internal secrets ("Failed to open vault: authentication failed or data corrupted").
5. **Log Whitelist Rule:** Logs are restricted to approved event codes and durations. Exception details and log entries must never contain passwords, usernames, or key dumps.
6. **User-Initiated Autofill:** Credentials are never dispatched without explicit user initiation; automated form submission (e.g., automated `Enter`) is prohibited.

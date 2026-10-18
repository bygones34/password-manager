# Security Test Matrix — SECURITY_TEST_MATRIX (v1.0)

* **Version:** 1.0 (Milestone M1.1)
* **Date:** 2026-10-10
* **Status:** Approved / Active
* **Target Projects:** `PasswordManager.Security.Tests`, `PasswordManager.Infrastructure.Tests`, `PasswordManager.Windows.Tests`
* **Related Documents:** `THREAT_MODEL.md`, `VAULT_FORMAT.md`, `ADR-002`, `ADR-003`

---

## 1. Overview

This document specifies the mandatory security test cases required to verify the `PasswordManager` cryptographic core, storage layer, and OS integrations. Merely compiling or passing a single positive (happy path) test is insufficient to consider a feature complete; all negative, boundary violation, and tampering test cases must pass.

---

## 2. Test Categories & Acceptance Criteria

### 2.1. Category A: Standard Cryptographic Vectors (RFC Vectors)
*Objective: Independently prove that library implementations strictly adhere to cryptographic standards.*

| Test ID | Test Scenario | Expected Result | Verification Method |
| --- | --- | --- | --- |
| **SEC-A01** | Argon2id v1.3 Official RFC 9106 Test Vectors | Output produced using known password, salt, and $m, t, p$ parameters matches official RFC 9106 bytes exactly. | Unit Test (`Argon2idVectorTests`) |
| **SEC-A02** | HKDF-SHA-256 RFC 5869 Test Vectors | `HKDF-Expand` output using RFC 5869 IKM, salt, and info values is fully validated. | Unit Test (`HkdfVectorTests`) |
| **SEC-A03** | AES-256-GCM NIST SP 800-38D Vectors | Ciphertext and auth tag generated from NIST-approved key, IV, plaintext, and AAD inputs match exactly. | Unit Test (`AesGcmVectorTests`) |

---

### 2.2. Category B: Cryptographic Key Separation
*Objective: Prove that keys derived for distinct purposes can never be used interchangeably.*

| Test ID | Test Scenario | Expected Result | Verification Method |
| --- | --- | --- | --- |
| **SEC-B01** | `RecordKey != ManifestKey != RootKey != KEK` | `RecordKey` and `ManifestKey` derived from the same root key produce strictly distinct 256-bit sequences. | Unit Test (`KeySeparationTests`) |
| **SEC-B02** | Derivation with Distinct Vault IDs (`VaultId`) | Given identical `RootKey`, supplying different `VaultId` values produces entirely different derived sub-keys. | Unit Test (`KeySeparationTests`) |
| **SEC-B03** | Manifest Envelope Decryption Attempt with `RecordKey` | An envelope encrypted with `RecordKey` cannot be decrypted using `ManifestKey`; authentication tag mismatch is thrown. | Unit Test (`KeySeparationTests`) |

---

### 2.3. Category C: Tampering, Integrity & AAD Binding
*Objective: Ensure any modification to the database or cryptographic envelopes is immediately detected and rejected.*

| Test ID | Test Scenario | Expected Result | Verification Method |
| --- | --- | --- | --- |
| **SEC-C01** | 1-Bit Ciphertext Modification (Bit Flip) | Modifying a single bit in ciphertext fails AES-GCM tag verification; zero plaintext leaked. | Unit Test (`EnvelopeTamperTests`) |
| **SEC-C02** | Auth Tag Tampering | Modifying 1 byte in the auth tag results in fail-closed rejection of the decryption operation. | Unit Test (`EnvelopeTamperTests`) |
| **SEC-C03** | Record ID Substitution (`RecordId`) | If a valid ciphertext is moved under another `RecordId` in the database, decryption fails due to AAD mismatch. | Integration Test (`RecordSubstitutionTests`) |
| **SEC-C04** | Cross-Vault Record Injection (Cross-Vault Swap) | If a record from Vault A is injected into Vault B, decryption fails due to `VaultId` AAD mismatch. | Integration Test (`CrossVaultTamperTests`) |
| **SEC-C05** | Header Parameter Tampering | Modifying `MemoryKiB`, `Iterations`, or `Salt` in `VaultHeader` causes `WrappedRootKey` AAD verification to fail, preventing root key decryption. | Unit Test (`HeaderTamperTests`) |
| **SEC-C06** | Magic Byte / Format Version Corruption | Headers lacking `"PWMV"` magic bytes or with unsupported format versions are rejected immediately before running KDF. | Unit Test (`HeaderValidationTests`) |

---

### 2.4. Category D: Nonce Security & Collision Prevention
*Objective: Prevent catastrophic AEAD nonce reuse.*

| Test ID | Test Scenario | Expected Result | Verification Method |
| --- | --- | --- | --- |
| **SEC-D01** | Re-saving Identical Record | Saving identical plaintext 1,000 consecutive times yields 1,000 distinct 96-bit nonces and 1,000 distinct ciphertexts. | Unit Test (`NonceRandomnessTests`) |
| **SEC-D02** | CSPRNG Entropy Verification | Byte distribution of generated nonces is cryptographically random; no static IVs or sequential counters used. | Unit Test (`NonceRandomnessTests`) |

---

### 2.5. Category E: KDF Parameter Bounding & DoS Mitigation
*Objective: Prevent memory or CPU exhaustion attacks from maliciously crafted file headers.*

| Test ID | Test Scenario | Expected Result | Verification Method |
| --- | --- | --- | --- |
| **SEC-E01** | Excessive Memory Cost ($> 512 \text{ MiB}$) | Header specifying excessive memory (e.g., 2 GB / 2097152 KiB) is rejected with boundary error before KDF execution. | Unit Test (`KdfBoundingTests`) |
| **SEC-E02** | Insufficient Memory Cost ($< 16 \text{ MiB}$) | Header specifying memory below cryptographic floor (e.g., 1 MB) is immediately rejected. | Unit Test (`KdfBoundingTests`) |
| **SEC-E03** | Excessive Iterations ($> 10$) or Parallelism ($> 16$) | Exceeding iteration or lane limits triggers fail-closed error to prevent CPU DoS. | Unit Test (`KdfBoundingTests`) |
| **SEC-E04** | Invalid Salt Length ($< 16$ or $> 64$ Bytes) | Headers with non-standard salt lengths are rejected. | Unit Test (`KdfBoundingTests`) |

---

### 2.6. Category F: Memory Hygiene & Lifecycle Cancellation
*Objective: Minimize plaintext key and password lifespan in memory.*

| Test ID | Test Scenario | Expected Result | Verification Method |
| --- | --- | --- | --- |
| **SEC-F01** | Deterministic Key Cleanup (`ZeroMemory`) | When `VaultSession` or key wrappers are disposed, internal key byte arrays are zeroed (`0x00`). | Unit Test (`MemoryZeroingTests`) |
| **SEC-F02** | In-Flight Task Cancellation on Vault Lock | If `Lock()` occurs during active decryption or reveal operations, pending tasks abort via `OperationCanceledException`; no data delivered to UI. | Concurrency Test (`LockRaceTests`) |

---

### 2.7. Category G: Synthetic Secret & Physical Canary Scans
*Objective: Prove zero plaintext passwords leak into database, WAL, temporary files, or logs.*

| Test ID | Test Scenario | Expected Result | Verification Method |
| --- | --- | --- | --- |
| **SEC-G01** | Canary Scan Across SQLite `.db` and `-wal` Files | Following vault creation and record writes, raw bytes of database and WAL files are searched for synthetic master passwords or account secrets; zero plaintext matches found. | Integration Test (`DatabaseCanaryScannerTests`) |
| **SEC-G02** | Plaintext Leakage Scan Across Log Streams | Scanning NDJSON log streams across all CRUD, unlock, and lock flows confirms zero passwords or usernames are logged. | Integration Test (`LogSanitizationTests`) |

---

## 3. Matrix Execution & Acceptance Threshold
* All tests across **Categories A, B, C, D, E, F, and G** must pass with `100%` success in both local and CI environments.
* No tests may be skipped (`[Skip]` or `[Ignore]` is prohibited).
* Security tests must use synthetic, test-only credentials exclusively; real passwords or personal accounts must never be used.

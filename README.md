# PasswordManager (Windows 11)

A modern, local-first, and secure desktop password manager designed for Windows 11.

> **Status:** Active Development (Milestone M1 completed — Moving to Milestone M2)
> **Code Name:** `PasswordManager`
> **Target Platform:** Windows 11 (x64), .NET 10

---

## Project Goal

A password management solution providing a modern user interface compliant with Fluent Design standards on Windows 11, a quick-access mini-vault running in the System Tray, and user-initiated, verified autofill.

### Core Features (v1 Scope)
- **Local-First:** All data is encrypted and stored locally in a SQLite database. The initial release operates entirely offline.
- **Cryptographic Security:** AES-256-GCM authenticated encryption, Argon2id key derivation (KDF), HKDF key separation, and CSPRNG nonce generation for every encryption.
- **Modern User Experience:** WinUI 3 + Windows App SDK main window, light/dark theme support, accessibility compliance, and keyboard-first navigation.
- **System Tray Mini-Vault:** Fast search, copy, and vault state controls directly from the Windows taskbar.
- **Verified Autofill:** User-initiated, target-bound autofill verifying window handle (HWND), process (PID/path), and control identity before writing (no automatic Enter/submit).
- **Data Durability:** Atomic SQLite WAL backups (consistent snapshot) and portable verified restore.

---

## Architecture & Layers

The solution is structured strictly adhering to Clean Architecture and Ports-and-Adapters principles:

```text
src/
  PasswordManager.Domain/              # Core business entities, enums, rules (independent)
  PasswordManager.Application/         # Use cases, ports, session lifecycle, operation scopes
  PasswordManager.Security/            # Cryptography, Argon2id, AES-GCM, HKDF implementations
  PasswordManager.Infrastructure/      # Encrypted envelope SQLite storage, EF Core, migrations
  PasswordManager.Platform.Windows/    # Windows OS services (tray, WTS session lock, ACLs)
  PasswordManager.Autofill.Windows/    # UI Automation target validation and fill adapters
  PasswordManager.Desktop/             # WinUI 3 composition root, MVVM, and UI views
```

---

## Development Principles & Security Policy

1. **Strict Zero-Knowledge:** Master passwords are never written to disk or logged. When the vault is locked, zero sensitive metadata or decrypted credentials exist in memory or UI.
2. **Incremental & Verified Steps:** Development follows the milestone gates and acceptance criteria defined in [DEVELOPMENT_ROADMAP.md](DEVELOPMENT_ROADMAP.md).
3. **Execution Rules:** All development follows the non-negotiable security rules in [AGENTS.md](AGENTS.md).
4. **Threat Model:** Security boundaries and threat analysis are documented in [docs/THREAT_MODEL.md](docs/THREAT_MODEL.md).

---

## License & Notice
This repository is under active development and not yet intended for production credentials. All test suites operate strictly with synthetic test data.

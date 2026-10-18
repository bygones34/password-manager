# Project Instructions — PasswordManager

This file is maintained at the root of the repository. It applies to all engineering work across all subdirectories. Explicit user instructions in the active session take precedence. `DEVELOPMENT_ROADMAP.md` defines the product plan, and this file defines operational rules and security contracts.

## 1. Project Goal

We are developing a modern, local-first password manager for Windows 11. Key targets include a WinUI 3 main interface, a system tray mini-vault, and user-triggered autofill for proven applications. The initial release uses a local SQLite encrypted vault. Cloud sync, browser extension integration, Windows Hello, and packaging follow across subsequent roadmap milestones.

The project code name is `PasswordManager`; do not invent product names without explicit instruction. Project documentation, markdown files, and code identifiers must be in English. Keep UI strings in localized resources.

## 2. Start of Each Session

1. Read this file, the roadmap, `docs/PROJECT_STATUS.md` (if present), relevant ADRs, and the security test matrix.
2. Inspect `git status` and existing working tree changes; preserve user changes.
3. Determine the active subtask and acceptance criteria. Follow explicit user instructions; on general "continue" instructions, select the smallest open subtask from the status document.
4. Verify availability of platform, SDK, and test toolchains in the actual environment. Windows verification cannot be considered complete on Linux.
5. Report a brief objective and verification plan before proceeding with implementation.

Do not fabricate non-existent tests, commits, approvals, or releases. Do not silently resolve critical security decisions marked as TBD in code; document them in an ADR. Avoid unnecessary approval cycles for safe, reversible, routine implementation decisions.

## 3. Scope & Cadence

- Complete each subtask in small, reviewable increments. Do not convert an entire milestone into a single massive patch.
- Do not introduce premature crypto, autofill, sync, or Hello code outside the active scope.
- Do not deploy to production credentials or public distribution before dependencies and exit gates on the roadmap are satisfied.
- The user grants implementation authority; verify active session permissions before commit/push/merge/publish. If authorized, proceed; otherwise leave changes and test evidence clean and reviewable.
- Never perform reset, clean, force-push, user data deletion, destructive migrations, or degradation of existing security guarantees without explicit user confirmation after explaining the full impact.
- Operate under standard user privileges; never introduce administrator requirements, UIAccess, or UAC bypasses to solve autofill.
- When adding new dependencies, verify official documentation, licensing, maintenance status, security track record, and version compatibility. Manage package versions centrally and pinned.

## 4. Architectural Rules

- Domain is independent; Application defines use cases, models, and ports.
- Security knows nothing of UI, EF Core, or Win32; it implements cryptographic ports using standard, approved primitives.
- Infrastructure stores only encrypted envelopes and manifests. Never create plaintext credential EF entities or FTS indexes.
- Platform.Windows encapsulates native OS services; Autofill.Windows encapsulates target verification and UI Automation adapters.
- Autofill never receives database keys, direct DB access, or unbounded vault read rights. It receives only short-lived, session-bound credential access for single user-triggered fill actions.
- Desktop is the composition root. The UI never bypasses security controls to call database or key derivation APIs directly.
- In future phases, NativeHost will not open a separate vault; it will communicate with the active desktop session over narrow IPC.
- Avoid unnecessary generic frameworks, interfaces for every single class, microservices, local HTTP APIs, or mandatory CQRS/MediatR patterns.

## 5. Non-Negotiable Security Rules

1. Never place real passwords, master passwords, user accounts, tokens, vault DBs, backups, signing keys, or certificate secrets in source code, test fixtures, issues, logs, or commits. Test data must be synthetic and explicitly test-only.
2. Never store master passwords; never silently trim or normalize password input. The encoding policy matches `VAULT_FORMAT.md`.
3. Preserve key separation across KEK, root key, record key, and manifest key. Any change to algorithms, HKDF labels, AAD, serializers, nonces, tags, or KDF parameters requires format, ADR, and test updates.
4. AES-GCM requires a fresh CSPRNG nonce for every encryption. Prevent key/nonce reuse across retries, restores, and edits. Never use fixed nonces, deterministic IVs, or `System.Random`.
5. Fail closed on unknown formats, authentication tag mismatches, or corrupted data. Never fallback to plaintext recovery for corrupted ciphertext.
6. Header and import KDF/size parameters must undergo strict bounding checks before execution. Never run KDF on unbounded attacker-controlled parameters.
7. In the locked state, hide account metadata, counts, search results, form drafts, and secrets. All UI and tray surfaces bind to the same session generation.
8. Locking cancels all pending decrypt, reveal, clipboard, and fill tasks; delayed results cannot be delivered to UI. Vault remains locked following OS session lock or power suspend.
9. Deterministically wipe controlled key and byte buffers (`CryptographicOperations.ZeroMemory`). Do not claim to clear immutable managed strings or entire OS memory pages.
10. Logging must strictly follow an allowed-fields whitelist. Never dump entities/DTOs, enable EF sensitive data logging, or include secrets in exception context. Do not enable production debug dumps or telemetry silently.
11. Exclude sensitive data from Windows clipboard history/cloud sync and perform conditional cleanup. Never clear clipboard contents if the user has since copied something else. Do not claim clipboard logger immunity.
12. Password reveal is time-limited; mask immediately on lock or focus loss. Never publish passwords to accessibility or automation trees.
13. Backup and restore must be atomic and verified; never copy only the primary SQLite file while a WAL file exists. The active vault is preserved until restore is verified.
14. Local retry throttling is not defense against offline brute-force cracking; never disguise weak master password risks.
15. AEAD alone does not prevent rollback to an earlier valid snapshot. Never misrepresent incomplete protections as "tamper-proof".

Use the roadmap security test matrix when testing these rules. Do not consider a security feature complete simply because it compiles.

## 6. Autofill Rules

- Never dispatch secrets without explicit user selection and fill trigger; automatic submit or Enter is forbidden.
- Capture the foreground target descriptor before opening the tray; re-verify after unlock and focus return.
- Process name or window title alone is not a sufficient trust identity. Check HWND, PID, process start identity, executable path, integrity level, and expected control context.
- Re-verify target identity immediately prior to writing password. Abort on focus changes, PID reuse, unexpected dialogs, lock, timeout, or cancellation.
- Do not assume UI Automation providers support writing password fields; verify capabilities and real application behavior. Never attempt to read existing target passwords.
- The initial pilot offers no blind SendInput fallback; offer clear unsupported status and manual copy fallback.
- OS sign-in, UAC, secure desktop, and elevated processes are strictly out of scope.
- Prove supported applications and versions in `AUTOFILL_COMPATIBILITY.md`. Never list applications like Steam or Discord without verified empirical test evidence.
- Browser autofill requires exact origin, frame, and navigation-bound request validation; desktop UIA does not substitute for browser extension integration.
- Native Messaging and allowed_origins do not grant absolute immunity against co-located same-user malware. Never expose browser features to production without an IPC protocol threat review.

## 7. Windows Hello & Recovery

Hello consent and cryptographically gated key access are distinct mechanisms. DPAPI `CurrentUser` alone does not enforce Hello authorization for every decryption. Specify mechanisms and limitations in an ADR; do not promise hardware-backed security without verified proof.

The master password pathway remains portable. Never migrate machine-specific Hello wrappers to other devices via backup. Never retain plaintext master passwords or backdoor master keys for recovery. An forgotten master password cannot be recovered simply because a backup exists.

## 8. Verification & Completion

Run real build and test commands for the relevant scope. Commands are specified in the README according to the solution and toolchain; never report WinUI builds or native tests passed merely because generic `dotnet test` succeeded.

- Core: known crypto vectors, tamper/AAD, state/lock race, bounded validation.
- Storage: real SQLite, transactions, WAL/journal/backup canary scans, migrations.
- Recovery: restore in distinct test profiles; invalid passwords or damaged files do not alter active vaults.
- Windows: tray, session, DPI, and UI Automation target verification tests.
- Browser: origin/frame/navigation, native-host IPC, and malformed/replay tests.
- Release: clean VM installation, upgrade, uninstallation, and signature verification.

After code changes, complete `git diff --check`, execute relevant tests, and verify no sensitive or generated artifacts were introduced. Valid verifications are not needlessly repeated without modifications or failures.

Never include real secrets in UI test screenshots or logs. Record unexecuted checks as `Not run` or `Manual verification pending`. Never fabricate test counts or success claims.

## 9. Documentation & Delivery Standards

Update `docs/PROJECT_STATUS.md` at the end of each subtask:

- Date, active milestone/subtask, and completed scope.
- Executed commands and real results.
- Manual verification status, open issues/blockers, and risks.
- New or updated ADRs, and the next subtask.

When format or API behavior changes, update `VAULT_FORMAT.md`, `THREAT_MODEL.md`, `BACKUP_AND_RECOVERY.md`, `MANUAL_TESTS.md`, and the security test matrix. Provide a concise report to the user: outcome, verification evidence, remaining blockers, and next task. Never describe an undocumented assumption as "verified" or "secure".

If a security decision permanently changes the data format, prepare a verifiable spike/ADR/test plan before proceeding. If human or physical device verification is required, provide concrete test steps rather than marking the task as complete prematurely.

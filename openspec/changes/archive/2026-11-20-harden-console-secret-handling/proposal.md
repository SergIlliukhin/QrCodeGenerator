# Proposal

## Why

The seed phrase is echoed in plain text while typed, the decrypted seed and ciphertext stay on screen and in terminal scrollback after the program finishes, and every secret lives in immutable .NET strings that are never wiped from memory. Anything that can read the terminal (scrollback files, session restore, screen sharing, AI agents attached to the terminal) or the process memory (swap, crash dumps) can recover them.

## What Changes

- Seed phrase entry is masked like the password (`*` per character, backspace supported); validation errors still never echo the input.
- The decrypted seed phrase is still shown on explicit confirmation (the user needs it to verify), but the screen **and scrollback** are wiped once the user presses a key, before any further prompt and before exit, including Ctrl+C and error exits.
- The ciphertext is no longer printed after QR generation; it is shown only if the user explicitly asks for it, and wiped the same way afterwards. **BREAKING** (UX): users who copied the base64 from the console must opt in.
- Secret prompts refuse to run when stdin is redirected, and reveal prompts refuse when stdout is redirected, with a clear message instead of a crash or a write to a pipe/file.
- Seed phrase, password, derived key, and decrypted plaintext are held in mutable buffers that are zeroed after use, never as `string`.
- README gains a "Safe use" section: run in a standalone terminal with session logging off, not inside an editor/AI-enabled terminal, never let an agent run the program, and how to clear the clipboard.

Out of scope: README example vectors and the example QR image (they are intentional test data); QR file permissions/location; ciphertext format changes (existing QR codes and encrypted text must keep decrypting).

## Capabilities

### New Capabilities
- `console-secret-io`: how the console accepts, reveals, and erases secrets (seed phrase, password, ciphertext), including redirected-stream behavior.
- `secret-memory-hygiene`: lifetime and erasure of secret material in process memory.

### Modified Capabilities

None (no existing specs).

## Impact

- Code: `ConsoleHelper.cs`, `SeedPhraseManager.cs`, `UserInterface.cs`, `Shared/SeedEncryptor.cs`, `Shared/Bip39Words.cs` (span-based lookup).
- Target framework: `net9.0` → `net11.0` (latest), so the key can be set from a span with `SymmetricAlgorithm.SetKey`. Users need the .NET 11 runtime; NuGet packages may need bumping.
- Encryption API changes from `string` to `ReadOnlySpan<char>` / byte buffers; on-disk/QR format (version byte 1, AES-256-CBC, PBKDF2-SHA256 600k) unchanged.
- New test project for the encryptor/validation (no tests exist today); main `.csproj` must exclude it from compilation.
- `README.md`: new safe-use section, updated usage flow.

# Design

## Context

Current flow (see proposal.md - Why): `SeedPhraseManager.ReadSeedPhrase` uses `Console.ReadLine` (echoed), `ConsoleHelper.ReadMaskedLine` builds a `StringBuilder` and returns a `string`, `SeedEncryptor` takes/returns `string` and uses `Rfc2898DeriveBytes` + `CryptoStream`/`StreamReader`, and `UserInterface` prints the ciphertext and the decrypted seed with `Console.WriteLine`. `Console.Clear()` runs only at the top of the menu loop; exit paths never clear.

Constraints: retargeted to .NET 11 (`net11.0`, the latest; matches the only runtime installed on the dev machine), `TreatWarningsAsErrors` + `AnalysisMode=All`, runs on Windows/macOS/Linux, ciphertext format v1 (`version | salt32 | iv16 | AES-256-CBC/PKCS7`, PBKDF2-SHA256 600k over UTF-8 password) must stay byte-compatible.

## Goals / Non-Goals

**Goals:**
- No secret ever becomes a `System.String`; every secret buffer is pinned, fixed-size, and zeroed in `finally`/`Dispose`.
- One wipe routine used after every reveal and on every exit path.

**Non-Goals:**
- Erasing copies held inside the .NET console input stack (`Console.ReadKey` internal byte buffers) or the terminal emulator's own memory.
- Preventing a process with debugger/ptrace access from reading memory while the secret is in use.
- Changing the ciphertext format or the KDF parameters.

## Decisions

### 1. `SecretBuffer`: pinned fixed-capacity char buffer
New `internal sealed class SecretBuffer : IDisposable` wrapping `GC.AllocateArray<char>(capacity, pinned: true)` with a `Length`, `Append(char)`, `RemoveLast()`, `ReadOnlySpan<char> Span`, and `Dispose()` → `CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(array.AsSpan()))`. Capacity 1024 (matches `MAX_PLAINTEXT_LENGTH`); input past capacity is ignored. A byte counterpart (`pinned byte[]` + zero in `finally`) is used for the UTF-8 plaintext and the key.
- Pinned + fixed capacity means no GC compaction copies and no grow-and-copy (spec: buffers not relocated).
- Alternative: `SecureString` - rejected, deprecated for new code, and you have to marshal it back to a string/BSTR to use it.
- Alternative: `StringBuilder`/`List<char>` - rejected, both reallocate and leave stale chunks.

### 2. Masked input for both seed and password
`ConsoleHelper.ReadMaskedLine(prompt)` returns a `SecretBuffer` (caller disposes). `SeedPhraseManager.ReadSeedPhrase` switches to it. `Console.ReadKey(intercept: true)` already works on all three OSes.

### 3. Span-based validation
- Seed: iterate `span.Split(' ')` (`MemoryExtensions.Split`) and look each word up through `ValidWords.GetAlternateLookup<ReadOnlySpan<char>>()` (the default string comparer supports alternate lookup). Word count, spacing, and lowercase rules unchanged. Error messages stay rule-only, with no echoed input.
- Password: both existing checks run today, `SeedPhraseManager.ValidatePasswordFormat` (Unicode `char.Is*`) and `SeedEncryptor.ValidatePassword` (ASCII regexes), and a password must pass both. Both move to `ReadOnlySpan<char>` char loops with unchanged semantics (regexes become ASCII range checks) so the accepted set stays exactly the same. No password that works today gets rejected.

### 4. Encryptor over spans, no streams
- `Encrypt(ReadOnlySpan<char> seed, ReadOnlySpan<char> password) : string` and `Decrypt(string ciphertext, ReadOnlySpan<char> password) : SecretBuffer`.
- Key: `Rfc2898DeriveBytes.Pbkdf2(ReadOnlySpan<char>, salt, Span<byte> key, 600_000, SHA256)` into a pinned 32-byte array; it UTF-8-encodes the password exactly like the old constructor, so output is unchanged.
- Cipher: `aes.SetKey(key)` (`SymmetricAlgorithm.SetKey(ReadOnlySpan<byte>)`, present in the net11.0 reference pack), so the caller never hands over a cloned `byte[]`; then `aes.EncryptCbc(plain, iv, PaddingMode.PKCS7)` / `aes.DecryptCbc(cipher, iv, Span<byte> dest, PaddingMode.PKCS7)` into a pinned buffer. Removes `CryptoStream`/`MemoryStream`/`StreamReader`, which keep internal plaintext copies.
- Decrypt bounds: before decrypting, reject ciphertext whose body exceeds 4096 + 16 bytes (the UTF-8 maximum for 1024 chars plus one padding block) with `CryptographicException`. Decode with `new UTF8Encoding(false, throwOnInvalidBytes: true)`; invalid UTF-8 or more than 1024 chars → `CryptographicException` (reported as a wrong password), never truncation. A wrong password that happens to pass PKCS7 therefore fails cleanly.
- All key/plaintext byte arrays are zeroed in `finally`; `Aes` is disposed (it zeroes its key copy).
- Ciphertext stays a `string`: it isn't a secret on its own, and the QR/base64 APIs need strings.

### 5. Reveal writes raw bytes to stdout
`ConsoleHelper.RevealSecret(ReadOnlySpan<char>)` UTF-8-encodes into a pinned byte buffer and writes it via a cached `Console.OpenStandardOutput()` stream, then zeroes the buffer. `Console.Out` (a `StreamWriter`) is flushed first so output order holds.
- Alternative: `Console.Out.Write(span)` - rejected, the `StreamWriter` char/byte buffers keep the text until overwritten.
- Before revealing: if `Console.IsOutputRedirected`, refuse with a message.

### 6. Wipe routine and exit paths
`ConsoleHelper.WipeScreen()`: `Console.Clear()`, then on non-Windows write `ESC[3J` (erase scrollback) via the raw stdout stream; no-op when output is redirected. `ConsoleHelper.SecretsTouched` (static bool) is set by any secret read/reveal.
- After every reveal: wait for a key, `WipeScreen()`, dispose the buffer.
- `Program.Main`:
  - if `Console.IsInputRedirected`, print a message and return exit code 2;
  - register a `Console.CancelKeyPress` handler that wipes if `SecretsTouched`;
  - wrap `UserInterface.Run()` in `catch (Exception)` → wipe → print message → return 1 (no rethrow, so no crash report with a stack dump);
  - after a normal return, wipe if `SecretsTouched`.
- The `UserInterface` top-level generic catch drops its `throw;` / message so `Main` reports the error after the wipe.
- Windows: `Console.Clear()` clears the whole conhost buffer; `ESC[3J` isn't sent there because VT processing isn't guaranteed to be on.

### 7. Ciphertext opt-in
After `GenerateQRCode`, print the saved path, then `AskYesNo("Show encrypted text? (y/n)")`; on yes, run the same reveal → key → wipe sequence.

### 8. Tests
New `tests/QrCodeGenerator.Tests` (xUnit) referencing the main project; the main `.csproj` gets `<Compile Remove="tests/**" />` and `InternalsVisibleTo`. Covered: decrypting the two README example vectors (format-compat regression), round trip, wrong password → `CryptographicException`, `SecretBuffer` zero after `Dispose`, validation messages containing none of the input words. Console behavior is verified manually in tmux (`capture-pane -S -` shows no seed after the wipe).

## Risks / Trade-offs

- [Risk] `Console.ReadKey` and the terminal emulator keep their own copies of keystrokes → out of scope; README tells users to close the terminal window after use.
- [Risk] Windows Terminal may keep scrollback after `Console.Clear()` → README advises closing the tab; listed as a known limit.
- [Risk] `Aes`/`Rfc2898DeriveBytes.Pbkdf2` make internal key copies that depend on the runtime implementation → rely on `Aes.Dispose` zeroing and the runtime's cleared rented buffers [INFERENCE: not verified per platform].
- [Risk] Tightening analyzers (`CA2000` etc.) on new disposables → every `SecretBuffer` is in `using`.
- [Trade-off] The ciphertext is no longer printed by default → one extra prompt for users who want it.

## Migration Plan

No data migration; format unchanged. Rollback = revert the commit. Existing QR codes are checked by the README-vector test.

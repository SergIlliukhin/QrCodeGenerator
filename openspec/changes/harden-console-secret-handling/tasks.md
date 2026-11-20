# Tasks

The project is retargeted to `net11.0` (group 1), so no roll-forward is needed.

## 1. Retarget and test harness

- [x] 1.1 Retarget `QrCodeGenerator.csproj` to `net11.0`, update `SkiaSharp`/`ZXing.Net` to their latest stable versions if the current ones produce warnings on net11, and fix any new analyzer errors that `AnalysisLevel=latest` raises; verify `dotnet build` succeeds with warnings-as-errors and `dotnet run` shows the menu
- [x] 1.2 Create `tests/QrCodeGenerator.Tests` (xUnit, `net11.0`) referencing `QrCodeGenerator.csproj`; add `<Compile Remove="tests/**" />` and `<InternalsVisibleTo Include="QrCodeGenerator.Tests" />` to the main csproj and add the test project to `QrCodeGenerator.sln`; verify `dotnet build QrCodeGenerator.sln` succeeds with warnings-as-errors
- [x] 1.3 Add a baseline test decrypting both README example vectors with the current `string` API and asserting the README phrases; verify it passes before any refactor (this pins format compatibility)

## 2. Secret buffers

- [x] 2.1 Add `SecretBuffer` (pinned `GC.AllocateArray<char>(1024, pinned: true)`, `Append`, `RemoveLast`, `Span`, `Length`, zeroing `Dispose`) per design §1; verify with a test that the backing array is all zero after `Dispose` and that `Append` past capacity is ignored

## 3. Encryptor over spans

- [x] 3.1 Rewrite `SeedEncryptor.Encrypt(ReadOnlySpan<char>, ReadOnlySpan<char>)` and `Decrypt(string, ReadOnlySpan<char>) : SecretBuffer` using `Rfc2898DeriveBytes.Pbkdf2` into a pinned key array, `aes.SetKey(key)`, `Aes.EncryptCbc`/`DecryptCbc` into pinned buffers, the ciphertext size bound, and strict `UTF8Encoding(false, true).GetChars` into the `SecretBuffer`; zero key/plaintext arrays in `finally` (design §4); verify the README-vector test (moved to the new API) and a new round-trip test pass
- [x] 3.2 Replace `ValidatePassword` regexes with ASCII range loops over `ReadOnlySpan<char>`; verify with a test table that the passwords `Sergii19_Update` and `Mariaaaa)$34` still pass and that missing upper/lower/digit/special or <12 chars each fail as before
- [x] 3.3 Add tests that a wrong password, an oversized ciphertext body (> 4112 bytes), and a ciphertext decrypting to invalid UTF-8 each throw `CryptographicException` and never return truncated text; verify they pass

## 4. Validation without strings

- [x] 4.1 Port `SeedPhraseManager.ValidateSeedPhrase` to `ReadOnlySpan<char>` using `span.Split(' ')` and add `Bip39Words.IsValidWord(ReadOnlySpan<char>)` via `GetAlternateLookup<ReadOnlySpan<char>>()`; remove the `string` overloads; verify tests for 12/24 words, double space, leading/trailing space, and an invalid word, asserting each `ArgumentException.Message` contains none of the input words
- [x] 4.2 Port `ValidatePasswordFormat` to `ReadOnlySpan<char>` with unchanged `char.Is*` semantics; verify the 3.2 table also passes through this function

## 5. Masked input and reveal

- [x] 5.1 Change `ConsoleHelper.ReadMaskedLine` to fill and return a `SecretBuffer` (no `StringBuilder`), set `SecretsTouched`; verify manually that `*` and backspace behave as before
- [x] 5.2 Switch `SeedPhraseManager.ReadSeedPhrase`/`ReadPassword` to masked `SecretBuffer` input with `using`, and dispose on every validation-failure retry; verify manually in a terminal that the seed prompt shows only `*`
- [x] 5.3 Add `ConsoleHelper.RevealSecret(ReadOnlySpan<char>)`: flush `Console.Out`, refuse with a message if `Console.IsOutputRedirected`, otherwise UTF-8-encode into a pinned buffer, write it to a cached `Console.OpenStandardOutput()` stream, then zero the buffer (design §5); verify `dotnet run > out.txt` with a confirmed reveal leaves no seed in `out.txt`
- [x] 5.4 Add `ConsoleHelper.WipeScreen()` (`Console.Clear()` + `ESC[3J` on non-Windows, no-op when output is redirected); verify in tmux that `tmux capture-pane -p -S -` returns no prior lines after a wipe

## 6. Flow changes

- [x] 6.1 `UserInterface.EncryptAndGenerateQR`: pass spans to `Encrypt`, dispose buffers, stop printing the ciphertext; print the file path, then offer the opt-in "show encrypted text" → reveal → key → wipe; verify manually that by default only the path is printed
- [x] 6.2 `UserInterface.DecryptQR`/`DecryptText`/`ShowDecryptedSeedPhrase`: use the `SecretBuffer` from `Decrypt`; after a confirmed reveal wait for a key, wipe, then dispose; dispose on decline and on wrong-password retries; verify manually in tmux that after the key press `capture-pane -S -` contains no seed words
- [x] 6.3 `Program.Main`: exit code 2 with a message when `Console.IsInputRedirected`; `CancelKeyPress` handler that wipes if `SecretsTouched`; `catch (Exception)` → wipe → message → return 1; wipe after a normal return; remove the `throw;` from the `UserInterface` generic catch (design §6); verify `echo 1 | dotnet run` exits 2 without prompting, and in tmux that Ctrl+C after typing a seed leaves an empty scrollback
- [x] 6.4 Remove the now-unused `SeedPhraseManager.ReadText` and any leftover `string` secret paths; verify `rg -n "ReadLine\(\)" *.cs` hits only the menu, file-path, yes/no, and ciphertext prompts

## 7. Documentation

- [x] 7.1 Update `README.md`: system requirements `.NET 11.0` (and drop the "C# 9.0" claim), masked seed entry, opt-in ciphertext display, automatic wipe, and a "Safe use" section (standalone terminal with session logging and restore off; not inside an editor or AI-enabled terminal; never let an agent run the program; clear clipboard history; close the window afterwards; the Windows Terminal scrollback limit); verify the documented flow matches a manual run

## 8. Integration check

- [x] 8.1 Run `dotnet test` (all green), then do one full manual session in tmux: encrypt → decrypt the produced QR with reveal → exit with `n`; verify `capture-pane -S -` is empty at exit and the produced QR decrypts to the original phrase

# Tasks

Run GUI scenarios with `QRGEN_E2E_GUI=1 dotnet test tests/QrCodeGenerator.E2ETests`; tmux scenarios run with plain `dotnet test`.

## 1. E2E project and app fixture

- [x] 1.1 Create `tests/QrCodeGenerator.E2ETests` (xUnit 2.9, `net11.0`, `Xunit.SkippableFact`), with a `ProjectReference` to `QrCodeGenerator.csproj` (`ReferenceOutputAssembly="false"`), `DisableTestParallelization`, and add it to `QrCodeGenerator.sln`; verify `dotnet build QrCodeGenerator.sln` succeeds with warnings-as-errors
- [x] 1.2 Add `AppBuild` (repo-root discovery via `QrCodeGenerator.sln`, Debug dll path, and a once-per-run Release build into a temp dir) and the launch-wrapper builder (design §3); verify with a test that the Debug dll exists and that the Release build produces a dll

## 2. Debug-only fault hook

- [x] 2.1 Add the `#if DEBUG` `QRGEN_E2E_FAULT=after-seed` throw in `UserInterface.EncryptAndGenerateQR` after `ReadSeedPhrase()` (design §8); verify `dotnet build -c Release` still succeeds and the unit tests still pass (behavioral checks come in 6.6 and 6.7)

## 3. Driver interface and tmux driver

- [x] 3.1 Add `ITerminalSession`, `TerminalDrivers.All` with skip reasons (design §7), and `SecretAssert` (design §9); verify a test listing drivers reports Terminal.app/iTerm2 as skipped with a reason mentioning `QRGEN_E2E_GUI` when the variable is unset
- [x] 3.2 Implement the tmux driver (dedicated socket, unique session, remain-on-exit, `capture-pane -J -S -`, `C-c`, exit-code from marker or pane status; cleanup in `Dispose`) per design §4; verify driver self-tests: scrollback returns the first of 300 printed lines, `WaitFor` times out with the last screen in the message, and `Dispose` leaves no session and no app process

## 4. Terminal.app driver

- [x] 4.1 Implement the Terminal.app driver via `osascript` (design §5): open, 200 columns, `do script … in tab`, `history`, close; 20 s call timeout; `-1743`/timeout mapped to the Automation-permission message; verify the same driver self-tests as 3.2 pass with `QRGEN_E2E_GUI=1`
- [x] 4.2 Prove input reaches a running program: self-test starts `/bin/sh -c 'read x; echo got:$x'` in the tab, sends a line, and expects `got:` plus the text. If it fails, switch typing to System Events `keystroke` and document the Accessibility permission. Verify the self-test passes

## 5. iTerm2 driver

- [x] 5.1 Install iTerm2 (`brew install --cask iterm2`) and launch it once so it completes first-run setup; verify `/Applications/iTerm.app` exists and `osascript -e 'tell application "iTerm2" to version'` returns a version
- [x] 5.2 Implement the iTerm2 driver (design §6); self-test whether session `contents` includes the first of 300 printed lines. If not, implement reading via the iTerm2 Python API and document enabling it. Verify the 3.2 self-tests plus the 4.2 input test pass for iTerm2

## 6. Scenario suite (each a `SkippableTheory` over all drivers)

- [x] 6.1 Masked entry: example seed sent as `seed + "x" + DEL`; assert mask count equals the seed length and no seed word appears; verify it passes on tmux, Terminal.app, iTerm2
- [x] 6.2 Invalid seed: enter a seed with a non-BIP-39 word; assert the BIP-39 error appears and no entered word appears; verify on all three
- [x] 6.3 Encrypt, ciphertext declined: assert the QR file exists in the temp dir and no `Зашифрована сід-фраза:` / base64 appears; encrypt, ciphertext shown: capture the ciphertext before the key press, then assert it's absent from `ReadAll()` after; verify on all three
- [x] 6.4 Decrypt the generated QR with reveal: assert the seed is visible before the key press and absent after; decrypt with decline: assert no seed word ever appears; verify on all three
- [x] 6.5 Decrypt text mode with the README ciphertext: wrong password then the right one; assert `Невірний пароль` then success; verify on all three
- [x] 6.6 Exit paths: reveal then `n` → exit code 0, no prompt/secret text left; interrupt after typing the seed → exit 130 or SIGINT, no seed prompt or `*` run left; Debug run with `QRGEN_E2E_FAULT=after-seed` → exit 1, error message shown, no seed prompt or `*` run left; verify on all three
- [x] 6.7 Release build with `QRGEN_E2E_FAULT=after-seed` in tmux reaches the password prompt (hook absent); verify it passes

## 7. Redirected streams

- [x] 7.1 Process test: `echo 1 | dotnet <dll>` → exit 2, refusal on stderr, empty stdout; tmux test: app with `> out.txt`, decrypt with confirmed reveal → `out.txt` has no seed word and the pane shows the refusal; verify both pass

## 8. Documentation

- [x] 8.1 README: how to run unit vs e2e tests, `QRGEN_E2E_GUI=1`, granting Automation permission (and Accessibility/Python API if 4.2/5.2 needed them), that GUI windows open and take focus, and expected runtime; verify by following the steps from a clean shell

## 9. Integration check

- [x] 9.1 Run `QRGEN_E2E_GUI=1 dotnet test QrCodeGenerator.sln`; all unit tests pass and every e2e scenario passes or is skipped with a reason. If a wipe scenario fails in Terminal.app or iTerm2, stop and report it as a product finding instead of weakening the assertion

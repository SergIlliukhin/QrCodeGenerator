# Proposal

## Why

The console secret-handling behavior from `harden-console-secret-handling` (masked entry, reveal-then-wipe, wipe on exit, Ctrl+C and errors, refusal on redirected streams) has only been checked by a one-off tmux script. Whether `ESC[3J` actually clears scrollback depends on the terminal and has never been checked in Terminal.app or iTerm2, the terminals users actually run. Without repeatable end-to-end tests, a regression would leave a seed phrase in someone's scrollback with nothing to catch it.

## What Changes

- New end-to-end test project `tests/QrCodeGenerator.E2ETests` that starts the built app inside a real terminal, types into it, and reads back the screen **and scrollback**.
- Three terminal drivers behind one interface:
  - **tmux**: runs without a window, works on macOS and Linux, and is the default.
  - **Terminal.app**: driven through AppleScript; opt-in because it opens windows.
  - **iTerm2**: driven through AppleScript; opt-in, and installed on the dev machine with `brew install --cask iterm2`.
- A scenario suite covering the important cases, each run against every available terminal:
  - masked entry and backspace;
  - validation errors that don't echo the input;
  - ciphertext hidden by default, and shown then wiped on request;
  - decrypted seed shown then wiped, or declined;
  - wrong password, then a retry;
  - wipe on normal exit, on Ctrl+C, and on an unexpected error.
- Process-level tests for redirected streams: piped stdin exits with code 2, and a reveal with redirected stdout writes no secret to the redirected output.
- A **Debug-only** fault-injection hook (environment variable, compiled out of Release builds) so the "unexpected error → wipe" path can be tested.
- README section on running the e2e tests and granting macOS Automation permission.

Out of scope: Windows terminals; GUI terminals in CI (no hosted runner has an interactive Terminal.app/iTerm2 session with Automation permission); macOS Terminal's saved window state ("Restore contents"), which isn't scriptable.

## Capabilities

### New Capabilities
- `console-e2e-tests`: the end-to-end test harness and its guarantees: which terminals it drives, what each scenario asserts about screen and scrollback, how unavailable terminals are reported, and the Debug-only fault hook it relies on.

### Modified Capabilities

None. `openspec/specs/` is empty; the `console-secret-io` behavior these tests check is still in the unarchived change `harden-console-secret-handling` and isn't modified here.

## Impact

- New `tests/QrCodeGenerator.E2ETests/` project, added to `QrCodeGenerator.sln`; the existing `<Compile Remove="tests/**" />` already keeps it out of the app build.
- `UserInterface.cs` (or `Program.cs`): a `#if DEBUG` fault hook. Release output is unchanged.
- Dev-machine tooling: tmux (already installed), iTerm2 (to install), and a one-time macOS Automation permission for the test host to control Terminal and iTerm2.
- If Terminal.app or iTerm2 turns out to keep scrollback after the wipe, the tests fail on purpose. Fixing the wipe would then be a follow-up change, not something these tests hide.
- `README.md`: how to run the e2e tests.

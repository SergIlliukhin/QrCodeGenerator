# Design

## Context

See proposal.md for motivation. Current state:
- `tests/QrCodeGenerator.Tests` is an xUnit 2.9 project (`net11.0`) with unit tests only.
- The main csproj excludes `tests/**` from compilation.
- The app reads secrets with `Console.ReadKey(intercept: true)`, wipes with `Console.Clear()` + `ESC[3J`, and refuses redirected stdin (exit 2) and redirected-stdout reveals.
- The dev machine (macOS) has tmux and Terminal.app; iTerm2 isn't installed yet. Terminal.app's AppleScript dictionary has `do script`, `history` ("contents of the entire scrolling buffer"), `contents`, `busy`, and `tty`.

## Goals / Non-Goals

**Goals:**
- One scenario suite, run unchanged against every terminal driver.
- Assertions on screen **and** scrollback text, as the terminal itself holds it.
- Deterministic waits: poll until expected text appears, with a timeout, and never use fixed sleeps for correctness.

**Non-Goals:**
- Pixel/visual checks, terminal-emulator memory, or macOS saved window state.
- Running GUI terminals in CI. Only tmux scenarios are CI-suitable.
- Fixing a terminal's scrollback handling. If a terminal fails a wipe assertion, that's a finding for a follow-up change.

## Decisions

### 1. Separate e2e project that launches the built app
`tests/QrCodeGenerator.E2ETests` (xUnit 2.9 + `Xunit.SkippableFact` for runtime skips with a reason) gets a `ProjectReference` to `QrCodeGenerator.csproj` with `ReferenceOutputAssembly="false"`, so the app is built first but not linked. An `AppBuild` helper finds the repo root (walking up to `QrCodeGenerator.sln`) and resolves `bin/<Configuration>/net11.0/QrCodeGenerator.dll`.
- A separate project keeps the fast unit tests free of terminal dependencies and lets `dotnet test tests/QrCodeGenerator.Tests` stay sub-second.
- Alternative: drive the app in-process with redirected console streams. Rejected: the app refuses redirected stdin by design, and it wouldn't exercise a terminal's scrollback.

### 2. Driver interface shaped by the weakest terminal
```
interface ITerminalSession : IDisposable
  string Name
  void SendLine(string text)   // text + Enter
  void PressEnter()
  void Interrupt()             // SIGINT to the app
  string ReadAll()             // screen + scrollback
  string WaitFor(Func<string,bool> predicate, string description, TimeSpan? timeout = null)
  int WaitForExit(TimeSpan? timeout = null)
```
Terminal.app's `do script "…" in tab` always appends a newline, so there is no "type without Enter". Backspace is tested as `SendLine(seed + "x" + "\u007f")`: the DEL char reaches `ReadKey` as Backspace before Enter submits, so the submitted seed must be valid and show exactly `seed.Length` asterisks.
- "Press any key" uses Enter (`PressEnter`), which `ReadKey` accepts.

### 3. Uniform launch wrapper
Each session starts a shell in a fresh temp dir and types:
`/bin/sh -c 'echo $$ > app.pid; exec env <vars> dotnet <dll>'; echo "E2E_EXIT=$?"`
- `app.pid` gives the real app PID (the `exec` keeps it). `Interrupt()` sends `kill -INT <pid>` in the GUI drivers; tmux sends a real `C-c` keystroke.
- The `E2E_EXIT=<code>` marker, printed after the app exits, gives the exit code in every terminal. An interrupt yields 130.
- `Dispose()`: `kill -KILL` the PID if still alive, close the window/session, delete the temp dir.
- Found during implementation:
  - The wrapper runs under a non-interactive `/bin/sh -c`, because an interactive zsh drops the rest of the command list when the app dies from SIGINT, which would lose the exit marker.
  - GUI `Close()` first SIGKILLs every job on the window's tty except `login` and the login shell (PIDs via `lsof -t <tty>`, since `pkill -t` matches nothing from the test host). Otherwise Terminal.app leaves a "terminate running processes?" sheet, and killing iTerm2's login shell triggers a blocking "Session ended very soon after starting" alert.
  - iTerm2 `contents` includes full scrollback (no Python API needed), and each row is padded with a trailing space, which the driver strips.

### 4. tmux driver
Uses a dedicated socket (`tmux -L qrgen-e2e`), a unique session per scenario, `-x 200 -y 50`, and `history-limit 10000`.
- `send-keys -l` for text, `Enter`, `C-c`; `capture-pane -p -J -S -` for screen + scrollback (`-J` joins wrapped lines).
- The tmux `C-c` also kills the wrapper shell, so the session sets `remain-on-exit on` and reads the exit status from `#{pane_dead_status}` / `#{pane_dead_signal}` when `E2E_EXIT=` never appears.

### 5. Terminal.app driver (AppleScript via `osascript`)
- Open: `do script "<wrapper>"` returns a tab; store `id of window`, then `set number of columns … to 200`.
- Type: `do script "<text>" in tab 1 of window id N`. While the app runs in the tab this types into its tty. The task 4.2 test proves this; fallback is System Events `keystroke` (needs Accessibility permission).
- Read: `history of tab 1 of window id N`.
- Close: `close window id N saving no` after the app is dead.
- AppleScript string escaping: `"` and `\` are escaped; DEL is passed as `(ASCII character 127)` concatenation.
- Every `osascript` call has a 20 s timeout. Error `-1743` (not authorized) or a timeout on first contact becomes: "Grant Automation permission: System Settings → Privacy & Security → Automation → <host> → Terminal".

### 6. iTerm2 driver (AppleScript)
- Install on the dev machine: `brew install --cask iterm2`.
- Open: `create window with default profile` (a shell, so the window survives app exit), then `write text "<wrapper>"` to its current session; store the window id.
- Type: `write text "<text>"` (Enter included).
- Read: `contents` of the session. Whether `contents` includes scrollback is unverified; the task 5.2 test decides. If it doesn't, read via the iTerm2 Python API (`async_get_line_info` + `async_get_contents` from the first scrollback line), which needs the Python API enabled in iTerm2 settings. The README would document that.
- Permission handling as for Terminal.app.

### 7. Availability and opt-in
- `TerminalDrivers.All` yields `(name, factory, skipReason?)`.
- tmux: skip if `tmux` isn't on PATH.
- Terminal.app/iTerm2: skip unless macOS **and** `QRGEN_E2E_GUI=1`; iTerm2 also skips if `/Applications/iTerm.app` is missing.
- Scenarios are `[SkippableTheory]` over `TerminalDrivers.All` and call `Skip.If(skipReason != null, skipReason)` first, so skipped ≠ passed.
- `[assembly: CollectionBehavior(DisableTestParallelization = true)]`: GUI windows and one tmux server stay predictable. Expected runtime is about 2–3 minutes with all terminals.

### 8. Debug-only fault hook
In `UserInterface.EncryptAndGenerateQR`, right after `ReadSeedPhrase()`:
```csharp
#if DEBUG
if (Environment.GetEnvironmentVariable("QRGEN_E2E_FAULT") == "after-seed")
    throw new InvalidOperationException("Injected e2e fault");
#endif
```
`InvalidOperationException` isn't caught by `Run`, so it reaches `Program.Main`'s last-chance handler (wipe → message → exit 1). The `using` disposes the seed buffer on the way out.
- Release check: an e2e fixture builds `-c Release` once (`dotnet build -c Release <csproj> -o <tmp>`) and runs that build in tmux with the variable set. It must reach the password prompt.
- Alternative: a hidden menu option. Rejected because it would ship in Release.

### 9. Assertions
`SecretAssert`:
- `NoSeedWords(text, seed)`: whole-word regex per word, plus the full phrase.
- `NoText(text, ciphertext)`.
- `MaskCount(text, fromMarker, toMarker)`: counts `*` between the seed prompt and the next prompt; the password-rules line contains `*`, so the count is bounded by markers.
- Failure messages include the driver name and the last 40 lines of `ReadAll()`.

Test data: the README example seed/password/ciphertext (public examples).

## Risks / Trade-offs

- [Risk] `do script … in tab` might not type into a running program on some macOS versions → task 4.2 tests it; the fallback is System Events `keystroke` plus an Accessibility permission note in the README.
- [Risk] iTerm2 `contents` may be screen-only → task 5.2 tests it; fallback is the Python API (an extra setting to enable).
- [Risk] Terminal.app or iTerm2 ignore `ESC[3J` → the wipe scenarios fail in that terminal. That's the point of the suite. Apply pauses and reports instead of weakening the assertion.
- [Risk] First run triggers macOS Automation prompts, which block `osascript` → 20 s timeout with the permission message; the README documents granting it up front.
- [Risk] GUI windows steal focus while the suite runs → documented; AppleScript targets windows by id, so input doesn't follow focus.
- [Trade-off] Interrupt in GUI terminals is `kill -INT` rather than a literal Ctrl+C keystroke (that would need Accessibility permission). The tmux driver covers the real keystroke path.
- [Trade-off] The Debug-only hook adds a few lines of test-only code to the app; `#if DEBUG` and the Release scenario keep it out of shipped builds.

## Migration Plan

Additive. Rollback = remove the e2e project and the `#if DEBUG` block.

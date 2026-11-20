# Spec Delta

## Purpose

End-to-end tests that run the console app inside real terminals (tmux, Terminal.app, iTerm2), type input and read back screen and scrollback, so that secret handling stays checked against the terminals people actually use.

## ADDED Requirements

### Requirement: Real terminal drivers
The e2e suite SHALL drive the built app inside tmux, Terminal.app, and iTerm2, sending keystrokes (text, Enter, Backspace, interrupt) and reading the full screen plus scrollback of the session.

#### Scenario: Reading scrollback
- **WHEN** a driver session has printed more lines than fit on screen
- **THEN** the text the driver returns includes the earliest printed line

#### Scenario: Same scenario on every terminal
- **WHEN** the suite runs with all three terminals available and enabled
- **THEN** every terminal scenario runs once per terminal and reports which terminal each result belongs to

### Requirement: Explicit availability and opt-in
tmux scenarios SHALL run by default when tmux is installed. Terminal.app and iTerm2 scenarios SHALL run only when `QRGEN_E2E_GUI=1` is set. A terminal that is unavailable or not enabled SHALL be reported as skipped with the reason, never as passed.

#### Scenario: GUI terminals not enabled
- **WHEN** `dotnet test` runs without `QRGEN_E2E_GUI=1`
- **THEN** Terminal.app and iTerm2 scenarios are reported as skipped with a message naming the variable, and no terminal window opens

#### Scenario: Terminal not installed
- **WHEN** `QRGEN_E2E_GUI=1` is set but iTerm2 is not installed
- **THEN** iTerm2 scenarios are reported as skipped with "iTerm2 not installed"

#### Scenario: Automation permission missing
- **WHEN** macOS denies the test host permission to control Terminal or iTerm2
- **THEN** the affected scenarios fail with a message telling the user to grant Automation permission, rather than hanging

### Requirement: Isolated, self-cleaning sessions
Each scenario SHALL use its own terminal session and temporary directory, SHALL close its session and kill the app process when it finishes or fails, and SHALL time out instead of hanging when expected output never appears.

#### Scenario: Expected prompt never appears
- **WHEN** a scenario waits for text that the app never prints
- **THEN** it fails within its timeout and includes the session's last screen in the failure message

#### Scenario: Cleanup after failure
- **WHEN** a scenario fails midway
- **THEN** its terminal window or tmux session is closed and no app process from it remains

### Requirement: Masked-entry scenarios
The suite SHALL check that typed seed phrases and passwords never appear as text in screen or scrollback, and that Backspace removes exactly one mask character.

#### Scenario: Seed entry masked
- **WHEN** the scenario types the 12-word example seed at the seed prompt
- **THEN** screen and scrollback contain one `*` per character and none of the seed words

#### Scenario: Invalid seed rejected without echo
- **WHEN** the scenario enters a seed containing a non-BIP-39 word
- **THEN** the error is shown and no entered word appears in screen or scrollback

### Requirement: Reveal-and-wipe scenarios
The suite SHALL check, in every terminal, that a revealed secret is visible before the key press and absent from screen and scrollback after it.

#### Scenario: Ciphertext hidden by default
- **WHEN** a QR code is generated and the scenario declines to show the encrypted text
- **THEN** the QR file exists and no ciphertext appears in screen or scrollback

#### Scenario: Ciphertext shown then wiped
- **WHEN** the scenario asks to see the encrypted text and then presses a key
- **THEN** the ciphertext is visible before the key press and absent from screen and scrollback afterwards

#### Scenario: Decrypted seed shown then wiped
- **WHEN** the scenario decrypts the generated QR code, confirms the reveal, and presses a key
- **THEN** the original seed phrase is visible before the key press and absent from screen and scrollback afterwards

#### Scenario: Reveal declined
- **WHEN** the scenario decrypts and declines the reveal
- **THEN** no seed word appears in screen or scrollback at any point

#### Scenario: Wrong password then retry
- **WHEN** the scenario decrypts encrypted text with a wrong password and then the right one
- **THEN** a wrong-password message appears and the retry succeeds

### Requirement: Exit-path wipe scenarios
The suite SHALL check that screen and scrollback hold no secret material after a normal exit, an interrupt, and an unexpected error, and SHALL check the exit code of each.

#### Scenario: Normal exit
- **WHEN** the scenario finishes an operation that revealed a secret and answers `n`
- **THEN** the app exits with code 0 and the scrollback holds no prompt or secret from the session

#### Scenario: Interrupt
- **WHEN** the scenario interrupts the app after typing a seed
- **THEN** the app terminates and the scrollback holds no seed prompt and no mask characters

#### Scenario: Unexpected error
- **WHEN** the scenario runs a Debug build with the fault hook set and enters a seed
- **THEN** the app exits with code 1, the error message is shown, and the scrollback holds no seed prompt or mask characters

### Requirement: Redirected-stream scenarios
The suite SHALL check that piped input is refused and that a reveal with redirected output writes no secret to that output.

#### Scenario: Piped input
- **WHEN** the app is started with input piped in
- **THEN** it exits with code 2, prints the refusal on stderr, and writes nothing to stdout

#### Scenario: Redirected output reveal
- **WHEN** the app runs in a terminal with stdout redirected to a file and the scenario confirms a reveal
- **THEN** the file contains no seed word and the terminal shows the refusal message

### Requirement: Debug-only fault injection
The app SHALL throw an unexpected error right after a seed phrase is entered when `QRGEN_E2E_FAULT=after-seed` is set, and SHALL ignore that variable entirely in Release builds.

#### Scenario: Debug build with hook
- **WHEN** a Debug build runs with `QRGEN_E2E_FAULT=after-seed` and a valid seed is entered
- **THEN** an unexpected error is raised and handled by the last-chance handler

#### Scenario: Release build
- **WHEN** a Release build runs with `QRGEN_E2E_FAULT=after-seed`
- **THEN** the app behaves exactly as without the variable

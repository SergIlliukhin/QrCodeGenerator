# console-secret-io Specification

## Purpose
Defines how the console program accepts, reveals, and erases secrets (seed phrase, password, ciphertext) so they are not left readable on screen, in terminal scrollback, or in redirected streams.

## Requirements

### Requirement: Masked seed phrase entry
The system SHALL read the seed phrase without echoing its characters, printing one `*` per accepted character and supporting backspace, exactly like password entry.

#### Scenario: Typing a seed phrase
- **WHEN** the user types a 12-word seed phrase at the seed prompt
- **THEN** the terminal shows only `*` characters for the typed input and never the words

#### Scenario: Correcting a typo
- **WHEN** the user presses backspace during seed entry
- **THEN** the last character is removed from the input and one `*` is erased from the display

### Requirement: Validation errors never echo secrets
The system SHALL report seed phrase and password validation failures without including any part of the entered value.

#### Scenario: Invalid BIP-39 word
- **WHEN** the entered seed phrase contains a word not in the BIP-39 list
- **THEN** the error message states the rule that failed and contains none of the entered words

### Requirement: Confirmed reveal of decrypted seed phrase
The system SHALL display the decrypted seed phrase only after the user explicitly confirms, and SHALL keep it displayed until the user presses a key.

#### Scenario: User confirms reveal
- **WHEN** decryption succeeds and the user answers `y` to the reveal prompt
- **THEN** the seed phrase is printed and the program waits for a key press before continuing

#### Scenario: User declines reveal
- **WHEN** decryption succeeds and the user answers anything other than `y`/`yes`
- **THEN** the seed phrase is not printed

### Requirement: Ciphertext shown only on request
After generating a QR code, the system SHALL NOT print the ciphertext unless the user explicitly asks to see it; when shown, it SHALL stay displayed until a key press.

#### Scenario: Default QR generation
- **WHEN** the user generates a QR code and declines the "show encrypted text" prompt
- **THEN** only the saved file path is printed, with no ciphertext

#### Scenario: User asks for ciphertext
- **WHEN** the user answers `y` to the "show encrypted text" prompt
- **THEN** the ciphertext is printed and the program waits for a key press before continuing

### Requirement: Screen and scrollback wiped after a reveal
After any revealed secret is acknowledged, the system SHALL clear the visible screen and the terminal scrollback before printing anything else.

#### Scenario: Continue after reveal
- **WHEN** the user presses a key after a seed phrase or ciphertext was shown
- **THEN** the screen and scrollback are cleared before the next prompt appears

### Requirement: Wipe on every exit path
The system SHALL clear the screen and scrollback before exiting whenever a secret was entered or revealed during the session, including normal exit, Ctrl+C, and unhandled errors.

#### Scenario: Normal exit after an operation
- **WHEN** the user finishes an operation and answers `n` to "another operation?"
- **THEN** the screen and scrollback are cleared before the farewell message and exit

#### Scenario: Ctrl+C during a prompt
- **WHEN** the user presses Ctrl+C at any prompt after entering a secret
- **THEN** the screen and scrollback are cleared before the process terminates

#### Scenario: Unexpected error
- **WHEN** an unhandled error ends the program after a secret was entered
- **THEN** the screen and scrollback are cleared before the error is reported

### Requirement: Refuse secret I/O on redirected streams
The system SHALL refuse secret entry when standard input is redirected and SHALL refuse to reveal a secret when standard output is redirected, reporting a clear message instead.

#### Scenario: Piped input
- **WHEN** the program is started with standard input redirected
- **THEN** it prints a message that secrets can only be typed in an interactive terminal and exits with a non-zero code without reading any input

#### Scenario: Redirected output
- **WHEN** standard output is redirected and the user confirms a reveal
- **THEN** the secret is not written and a message explains that reveal requires an interactive terminal

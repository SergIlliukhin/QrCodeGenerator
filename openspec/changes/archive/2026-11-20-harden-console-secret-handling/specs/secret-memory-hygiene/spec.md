# Spec Delta

## Purpose

Limits how long secret material (seed phrase, password, derived key, decrypted plaintext) survives in process memory, so it cannot be recovered later from swap, crash dumps, or memory inspection.

## ADDED Requirements

### Requirement: Secrets held only in erasable buffers
The system SHALL hold the seed phrase, password, derived encryption key, and decrypted plaintext only in mutable buffers that it can overwrite, and SHALL NOT convert them into immutable strings at any point.

#### Scenario: Seed phrase validation
- **WHEN** an entered seed phrase is split into words and checked against the BIP-39 list
- **THEN** no immutable string containing a seed word or the whole phrase is created

#### Scenario: Revealing the decrypted seed
- **WHEN** the decrypted seed phrase is printed
- **THEN** it is written to the console directly from the erasable buffer

### Requirement: Secrets zeroed after use
The system SHALL overwrite every secret buffer with zeros as soon as the operation that needed it finishes, whether it succeeds or fails.

#### Scenario: Successful encryption
- **WHEN** a QR code has been generated
- **THEN** the seed phrase, password, and derived key buffers contain only zeros

#### Scenario: Wrong password on decryption
- **WHEN** decryption fails because of a wrong password
- **THEN** the password and derived key buffers are zeroed before the user is asked again

#### Scenario: Revealed seed acknowledged
- **WHEN** the user presses a key after the decrypted seed was shown, or declines to show it
- **THEN** the decrypted plaintext buffers are zeroed

### Requirement: Secret buffers not relocated
The system SHALL keep secret buffers at a fixed memory location for their lifetime, so no stale copies are left behind by garbage-collector compaction or buffer growth.

#### Scenario: Long seed phrase input
- **WHEN** the user types a 24-word seed phrase
- **THEN** input is collected in a single fixed-location buffer that is never reallocated or copied

### Requirement: Ciphertext format compatibility
The system SHALL keep decrypting ciphertext produced by earlier versions (version byte 1) and SHALL keep producing the same format.

#### Scenario: Decrypting an existing QR code
- **WHEN** the user decrypts a QR code or encrypted text created before this change with the correct password
- **THEN** the original seed phrase is recovered

#### Scenario: Round trip
- **WHEN** a seed phrase is encrypted and then decrypted with the same password
- **THEN** the recovered phrase equals the original

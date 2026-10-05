## Purpose

Keeps a record of what the issuing add-on did and what went wrong, for the vendor's own
troubleshooting, without the log becoming a source of working keys.

Serves the **vendor**. Product decision: PDR-0028.

## ADDED Requirements

### Requirement: Log file in the data folder
The system SHALL write a log to a `logs` folder inside the data folder, one file per day, with
files older than 30 days removed. Each entry SHALL have a UTC timestamp, a level and a message.

#### Scenario: Log written
- **WHEN** the vendor issues a key
- **THEN** an entry SHALL be appended to that day's log file

### Requirement: Actions logged
The system SHALL log setup, data folder moves, signing keys folder changes, product, license type
and signing key changes, add-on link changes (both license references), every issued key (product, role, license reference, key identifier,
expiry, order reference, signing key ID), every reveal of a key string (key identifier only),
inspections (state and key identifier), exports (folder and row counts) and errors.

#### Scenario: Issue logged
- **WHEN** a key is issued
- **THEN** the log SHALL contain its license reference and key identifier

### Requirement: Secrets never logged
The log SHALL NOT contain a key string or any part of a key after its key identifier, a private
key, or a private key file's contents. Unexpected errors SHALL be logged with their details,
which SHALL NOT include those values either.

#### Scenario: Key not in log
- **WHEN** a key is issued, revealed and inspected
- **THEN** no log file SHALL contain the key string's signed contents or signature

### Requirement: Errors reported and logged
An unexpected error SHALL be logged in full and shown to the vendor as a short message naming the
log file, and SHALL return the vendor to the menu without recording a partial issue.

#### Scenario: Database error during issue
- **WHEN** recording an issued key fails
- **THEN** the vendor SHALL see that no key was issued, the log SHALL hold the error, and no key string SHALL have been shown

### Requirement: Log on the console
When started with the verbose option, the system SHALL also write log entries to the console.

#### Scenario: Verbose
- **WHEN** the vendor starts the tool with the verbose option
- **THEN** log entries SHALL appear on the console as well as in the file

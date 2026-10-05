## Purpose

Gets the issuing add-on ready to use the first time it runs, and keeps it pointed at the vendor's
records afterwards: where the data lives, where private keys live, and the vendor's product ID
prefix.

Serves the **vendor**. Product decisions: PDR-0023, PDR-0007, PDR-0017.

## ADDED Requirements

### Requirement: First-run setup
When no setup has been completed, the system SHALL run a guided setup before offering any other
action. Setup SHALL ask for the data folder, the signing keys folder and the product ID prefix,
SHALL show the backup guidance, and SHALL save the answers only when all are valid. Leaving
setup before it completes SHALL save nothing.

#### Scenario: First run
- **WHEN** the vendor starts the tool and no setup has been completed
- **THEN** the system SHALL start setup and offer no other action until it completes

#### Scenario: Setup abandoned
- **WHEN** the vendor leaves setup before confirming
- **THEN** no settings, folders or database SHALL be created, and the next start SHALL run setup again

#### Scenario: Defaults offered
- **WHEN** setup asks for the data folder
- **THEN** it SHALL offer a per-user application folder as the default, and accept any other folder the vendor enters

### Requirement: Data folder
The data folder SHALL hold the database and the activity log. When the chosen folder does not
exist, setup SHALL create it. When it already holds a database from this tool, setup SHALL use
that database and its records, and SHALL say so.

#### Scenario: Existing database reused
- **WHEN** the vendor chooses a data folder that already holds this tool's database
- **THEN** the system SHALL use it, report how many products and licenses it holds, and keep the signing keys folder and prefix stored in it unless the vendor changes them

#### Scenario: Folder not writable
- **WHEN** the chosen data folder cannot be created or written
- **THEN** setup SHALL report why and ask again

### Requirement: Signing keys folder kept apart
The signing keys folder SHALL NOT be the data folder, inside it, or contain it. Setup and any
later change of either folder SHALL reject a choice that breaks this, naming the rule (PDR-0007).

#### Scenario: Keys folder inside data folder
- **WHEN** the vendor chooses a signing keys folder inside the data folder
- **THEN** the system SHALL reject it and ask again

#### Scenario: Data folder inside keys folder
- **WHEN** the vendor chooses a data folder inside the signing keys folder
- **THEN** the system SHALL reject it and ask again

### Requirement: Product ID prefix
Setup SHALL ask for the vendor part of product IDs and SHALL accept it only if `<prefix>.product`
is a valid product ID by the library's rules. It SHALL pre-fill new product IDs and SHALL be
changeable later without affecting existing products.

#### Scenario: Invalid prefix
- **WHEN** the vendor enters `Acme`
- **THEN** the system SHALL reject it with the library's product ID rule

### Requirement: Backup guidance
Setup SHALL tell the vendor to back up the data folder and the signing keys folder separately,
and that losing a private key file stops issuing for that product.

#### Scenario: Guidance shown
- **WHEN** setup reaches its confirmation step
- **THEN** it SHALL show both folders and the backup guidance before saving

### Requirement: Missing data at start
When setup has been completed but the data folder or its database cannot be found, the system
SHALL say which is missing and offer to locate the folder, run setup again, or exit. It SHALL
NOT create an empty database without the vendor choosing to.

#### Scenario: Data folder missing
- **WHEN** the tool starts and the saved data folder does not exist
- **THEN** the system SHALL report it and offer locate, setup or exit, and SHALL NOT create the folder

### Requirement: Data folder for one run
The system SHALL accept a data folder at start, overriding the saved one for that run only.
If that folder holds no database, the system SHALL say so and ask whether to create one there,
defaulting to no. Only on yes SHALL it run setup for that folder; that setup's answers SHALL be
stored with the new database, and the saved data folder SHALL stay unchanged (PDR-0023).

#### Scenario: Override
- **WHEN** the vendor starts the tool with another data folder that holds a database
- **THEN** the system SHALL use that folder for the run and leave the saved settings unchanged

#### Scenario: No database, declined
- **WHEN** the vendor starts the tool with a data folder that holds no database and accepts the default
- **THEN** the system SHALL create nothing and exit

#### Scenario: No database, accepted
- **WHEN** the vendor starts the tool with a data folder that holds no database and chooses to create one
- **THEN** the system SHALL run setup for that folder, and the next start without a folder SHALL use the saved data folder

### Requirement: One copy per data folder
The system SHALL NOT run two copies against the same data folder at once. A copy started on a
data folder already in use SHALL say that another copy is open and exit without changing
anything. Copies on different data folders MAY run at the same time (PDR-0023).

#### Scenario: Second copy refused
- **WHEN** the tool is open on a data folder and the vendor starts it again on the same folder
- **THEN** the second copy SHALL say another copy is open and exit

#### Scenario: Different folders
- **WHEN** the tool is open on one data folder and the vendor starts it on another
- **THEN** both copies SHALL run

#### Scenario: Copy closed unexpectedly
- **WHEN** a copy ended without closing normally and the vendor starts the tool on the same folder
- **THEN** the new copy SHALL start

### Requirement: Move the data folder
The vendor SHALL be able to move the data to a new folder. The system SHALL copy the database
to the new folder, check the copy opens and holds the same number of records, switch to it, and
only then remove the old database, after asking. If the new folder already holds a database,
the system SHALL refuse the move and offer to switch to that database instead.

#### Scenario: Successful move
- **WHEN** the vendor moves the data to an empty folder
- **THEN** the records SHALL be available from the new folder, the saved settings SHALL point to it, and the old database SHALL be removed only if the vendor agrees

#### Scenario: Copy fails
- **WHEN** the copy cannot be written or does not match
- **THEN** the system SHALL keep using the old folder, leave it unchanged, and report why

### Requirement: Change the signing keys folder
The vendor SHALL be able to change the signing keys folder for keys created afterwards. Existing
signing keys SHALL keep the file location recorded when they were created; the system SHALL NOT
move private key files.

#### Scenario: Existing keys unaffected
- **WHEN** the vendor changes the signing keys folder
- **THEN** issuing with an existing signing key SHALL still read its recorded file, and new signing keys SHALL be written to the new folder

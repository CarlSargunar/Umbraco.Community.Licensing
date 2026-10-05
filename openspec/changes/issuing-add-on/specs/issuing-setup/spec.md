## Purpose

Gets the issuing add-on ready to use the first time it runs, and keeps it pointed at the vendor's
records afterwards: where the data lives, where private keys live, and the vendor's product ID
prefix.

Serves the **vendor**. Product decisions: PDR-0023, PDR-0007, PDR-0017, PDR-0031, PDR-0032.

## ADDED Requirements

### Requirement: First-run setup
When no setup has been completed on the machine, the system SHALL run a guided setup before
offering any other action. Setup SHALL ask for one location for the tool's data and signing keys,
and for the product ID prefix unless an existing database supplies it. It SHALL show the backup
guidance, and SHALL save the answers only when all are valid. Leaving setup before it completes
SHALL save nothing.

#### Scenario: First run
- **WHEN** the vendor starts the tool and no setup has been completed on the machine
- **THEN** the system SHALL start setup and offer no other action until it completes

#### Scenario: Setup abandoned
- **WHEN** the vendor leaves setup before confirming
- **THEN** no settings, folders or database SHALL be created, and the next start SHALL run setup again

### Requirement: One location for data and signing keys
Setup SHALL offer `<home>/.<toolname>` as the default location, showing its full path on the
machine, and SHALL accept any other folder. Under the location the system SHALL use a `data`
folder as the data folder and a `signing-keys` folder as the signing keys folder, creating any
that do not exist, readable only by the current user where the operating system supports it
(PDR-0032).

#### Scenario: Default accepted
- **WHEN** the vendor accepts the default location on a machine whose home directory is `/Users/carl`
- **THEN** the data folder SHALL be `/Users/carl/.<toolname>/data` and the signing keys folder `/Users/carl/.<toolname>/signing-keys`

#### Scenario: Other location
- **WHEN** the vendor enters `D:\Licensing`
- **THEN** the data folder SHALL be `D:\Licensing\data` and the signing keys folder `D:\Licensing\signing-keys`

#### Scenario: Folder not writable
- **WHEN** the chosen location, or a folder under it, cannot be created or written
- **THEN** setup SHALL report why and ask again

### Requirement: Settings saved per machine
The data folder and the signing keys folder SHALL be saved in a per-user settings file on the
machine, never in the database. A default folder SHALL be saved as default and resolved against
the current home directory at each start; a folder the vendor chose SHALL be saved as its path.
The product ID prefix SHALL be stored in the database (PDR-0032).

#### Scenario: Home directory differs
- **WHEN** the vendor chose the default location and the tool starts with a different home directory
- **THEN** the data folder and the signing keys folder SHALL be resolved under the new home directory

#### Scenario: Database holds no paths
- **WHEN** a database is copied from one machine to another
- **THEN** it SHALL hold no data folder or signing keys folder path

### Requirement: Data folder
The data folder SHALL hold the database and the activity log. When the data folder already holds
a database from this tool, setup SHALL use that database and its records, and SHALL say so.

#### Scenario: Existing database reused
- **WHEN** the vendor chooses a location whose data folder already holds this tool's database
- **THEN** the system SHALL use it, report how many products and licenses it holds, keep the product ID prefix stored in it unless the vendor changes it, and check the signing key files

#### Scenario: New machine
- **WHEN** the vendor has restored the data folder and the private key files under the default location on a new machine, and starts the tool
- **THEN** setup SHALL find the database, report its products and licenses, find every current signing key file, and complete without asking for any path other than confirming the location

### Requirement: Signing keys folder kept apart
The signing keys folder SHALL NOT be the data folder, inside it, or contain it. Any change of
either folder SHALL reject a choice that breaks this, naming the rule (PDR-0007).

#### Scenario: Keys folder inside data folder
- **WHEN** the vendor changes the signing keys folder to a folder inside the data folder
- **THEN** the system SHALL reject it and ask again

#### Scenario: Data folder inside keys folder
- **WHEN** the vendor moves the data folder into the signing keys folder
- **THEN** the system SHALL reject it and ask again

### Requirement: Signing key files checked
A current or pending signing key (PDR-0031) SHALL be usable only when its private key file is in
the signing keys folder, is readable and matches the recorded public key. The system SHALL check
this at setup when an existing database is used, and at every start. Finding, showing,
re-sending, inspecting and exporting records SHALL work whether or not signing keys are usable
(PDR-0032).

#### Scenario: Keys not found at setup
- **WHEN** setup uses an existing database and the current key file of `acme.commerce` is not in the signing keys folder
- **THEN** setup SHALL name the product and file, and offer to choose the signing keys folder again or continue without it

#### Scenario: Notice at start
- **WHEN** the tool starts and the current key files of `acme.commerce` and `acme.shipping` are not usable
- **THEN** the menu SHALL show a notice naming both products and the folder searched, and every action except issuing for those products SHALL be available

#### Scenario: Retired key file missing
- **WHEN** the file of a retired signing key is missing
- **THEN** the system SHALL report it where signing keys are listed, and SHALL NOT show a notice at start

### Requirement: Product ID prefix
Setup SHALL ask for the vendor part of product IDs and SHALL accept it only if `<prefix>.product`
is a valid product ID by the library's rules. It SHALL pre-fill new product IDs and SHALL be
changeable later without affecting existing products.

#### Scenario: Invalid prefix
- **WHEN** the vendor enters `Acme`
- **THEN** the system SHALL reject it with the library's product ID rule

### Requirement: Backup guidance
Setup SHALL tell the vendor to back up the data folder often, to back up the signing keys rarely
and keep the copy offline, that losing a private key file stops issuing for that product, and
never to sync, share or send the whole location, moving the data folder instead to keep records
in a synced folder (PDR-0032).

#### Scenario: Guidance shown
- **WHEN** setup reaches its confirmation step
- **THEN** it SHALL show the location, both folders and the backup guidance before saving

### Requirement: Missing data at start
When setup has been completed but the data folder or its database cannot be found, the system
SHALL say which is missing and offer to locate the folder, run setup again, or exit. It SHALL
NOT create an empty database without the vendor choosing to.

#### Scenario: Data folder missing
- **WHEN** the tool starts and the saved data folder does not exist
- **THEN** the system SHALL report it and offer locate, setup or exit, and SHALL NOT create the folder

### Requirement: Data folder for one run
The system SHALL accept a data folder at start, overriding the saved one for that run only, and
SHALL use the machine's signing keys folder. If that folder holds no database, the system SHALL
say so and ask whether to create one there, defaulting to no. Only on yes SHALL it ask for the
product ID prefix and store it with the new database; the saved settings SHALL stay unchanged
(PDR-0023).

#### Scenario: Override
- **WHEN** the vendor starts the tool with another data folder that holds a database
- **THEN** the system SHALL use that folder for the run and leave the saved settings unchanged

#### Scenario: No database, declined
- **WHEN** the vendor starts the tool with a data folder that holds no database and accepts the default
- **THEN** the system SHALL create nothing and exit

#### Scenario: No database, accepted
- **WHEN** the vendor starts the tool with a data folder that holds no database and chooses to create one
- **THEN** the system SHALL create the database there with the prefix entered, and the next start without a folder SHALL use the saved data folder

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
The vendor SHALL be able to change the signing keys folder to the folder where the private key
files now are. The system SHALL NOT move private key files. Before accepting the new folder it
SHALL check every current and pending signing key file there; when any is not usable it SHALL
name them and keep the old folder. Missing retired key files SHALL be reported and SHALL NOT
block the change (PDR-0032).

#### Scenario: Keys found
- **WHEN** the vendor changes the signing keys folder to a folder holding every current and pending key file
- **THEN** the saved settings SHALL point to it and issuing SHALL read key files from it

#### Scenario: Key missing in new folder
- **WHEN** the new folder lacks the current key file of `acme.commerce`
- **THEN** the system SHALL name it, keep the old folder, and save nothing

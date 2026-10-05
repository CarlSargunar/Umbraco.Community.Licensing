## Purpose

Lets the vendor find licenses and keys in its records, re-send a key, check a key a site owner
sends in, and take a full copy of its records as CSV files.

Serves the **vendor** answering support requests, and the **site owner**, who needs a lost key
re-sent. Product decisions: PDR-0006, PDR-0009, PDR-0019, PDR-0020, PDR-0026, PDR-0027,
PDR-0028, PDR-0030.

## ADDED Requirements

### Requirement: List licenses
The vendor SHALL be able to list licenses, optionally filtered by product and by status. Each
license SHALL have exactly one status, read from its current key against the current UTC date
(PDR-0030): perpetual (no expiry), active (expires more than 30 days after today), expiring
(expires today or within the next 30 days) or expired (expired before today). Each row SHALL
show product, license type, role, license reference, status, current key's expiry and order
reference.

#### Scenario: Expiring filter
- **WHEN** today is 2027-03-10 and a license's current key expires 2027-03-31
- **THEN** its status SHALL be expiring, and it SHALL appear under expiring and not under active

#### Scenario: Expires today
- **WHEN** today is 2027-03-10 and a license's current key expires 2027-03-10
- **THEN** its status SHALL be expiring

#### Scenario: Perpetual is not active
- **WHEN** a license's current key has no expiry
- **THEN** its status SHALL be perpetual, and it SHALL NOT appear under active

### Requirement: Find a license
The vendor SHALL be able to find licenses by license reference or key identifier, matched
ignoring case, hyphens and spaces as the library reads them, or by order reference, matched
exactly ignoring case. A key identifier SHALL find the license it belongs to.

#### Scenario: By reference
- **WHEN** the vendor searches `lic 8f3ak m7rxb`
- **THEN** the system SHALL show license `LIC-8F3AK-M7RXB`

#### Scenario: By order reference
- **WHEN** the vendor searches `shop-1001`
- **THEN** the system SHALL list every key whose order reference is `SHOP-1001`, with its license

### Requirement: Show a license
Showing a license SHALL list all its keys in issue order with key identifier, kind of issue,
issue time, expiry, features, order reference and signing key ID, the current key marked. For a
base license it SHALL list linked add-ons; for an add-on, its linked base.

#### Scenario: History
- **WHEN** the vendor shows a license issued and renewed once
- **THEN** both keys SHALL be listed, the renewal marked current

### Requirement: Re-send a key
The vendor SHALL be able to display the full key string of any recorded key, after choosing to
reveal it, for re-sending to the site owner.

#### Scenario: Reveal
- **WHEN** the vendor chooses to reveal the current key of a license
- **THEN** the system SHALL show its key string on one line

### Requirement: Inspect a key
The vendor SHALL be able to paste a key string and see how the library evaluates it, using the
public keys recorded for the product the key claims, current and retired. The system SHALL show
the library's state and, for a verified key, its contents; for a failed key, only what the
library reports (PDR-0019). It SHALL say whether the key identifier matches a recorded key and,
if so, whether that key is the license's current key.

#### Scenario: Valid recorded key
- **WHEN** the vendor pastes a current key from the records
- **THEN** the system SHALL show state valid, its contents and the matching record

#### Scenario: Superseded copy
- **WHEN** the vendor pastes a key that has since been renewed
- **THEN** the system SHALL show its state and say a later key exists for the license

#### Scenario: Unknown product
- **WHEN** the pasted key claims a product not in the records
- **THEN** the system SHALL say no public key is recorded for that product and show the claimed product ID only

#### Scenario: Damaged key
- **WHEN** the pasted key has been edited
- **THEN** the system SHALL show the library's failure state and no contents

### Requirement: Export to CSV
The vendor SHALL be able to export all records to a folder it chooses, as one CSV file each for
products, license types, license type features, signing keys, issued keys and issued key
features (PDR-0028). Each features file SHALL have one row per feature, naming the license type
or key identifier it belongs to. Issued keys SHALL include the key string;
signing keys SHALL include the public key and never private key material. Before writing, the
system SHALL warn that the files contain working keys. Files SHALL be UTF-8 with a header row,
dates in ISO 8601 UTC, and quoting as RFC 4180. The system SHALL NOT overwrite existing files
without asking.

#### Scenario: Export
- **WHEN** the vendor exports to an empty folder and accepts the warning
- **THEN** six CSV files SHALL be written, and the issued keys file SHALL have one row per recorded key including its key string

#### Scenario: Features exported
- **WHEN** the vendor exports a key with features `ecommerce` and `max-orders: 500`
- **THEN** the issued key features file SHALL have two rows for that key identifier, one per feature

#### Scenario: Existing files
- **WHEN** the export folder already holds files with the export's names
- **THEN** the system SHALL ask before overwriting and write nothing if refused

## Purpose

Holds what the vendor sells: products, the license types offered for each, and each product's
signing keys, so licenses can be issued from a template with the right key.

Serves the **vendor**. Product decisions: PDR-0023, PDR-0024, PDR-0007, PDR-0011, PDR-0017, PDR-0031, PDR-0032,
PDR-0033; signing keys follow the `signing-key-management` capability.

## ADDED Requirements

### Requirement: Create a product
The vendor SHALL be able to create a product with a product ID and a display name. The product
ID SHALL be pre-filled with the vendor's prefix, SHALL be checked with the library's product ID
rule, and SHALL be unique in the records. Creating a product SHALL create its first signing key.

#### Scenario: Product created
- **WHEN** the vendor creates product `acme.commerce` named `Acme Commerce`
- **THEN** the product SHALL be recorded with one current signing key, and its private key file SHALL exist in the signing keys folder

#### Scenario: Invalid product ID
- **WHEN** the vendor enters `Acme.Commerce`
- **THEN** the system SHALL reject it with the library's reason and create nothing

#### Scenario: Duplicate product ID
- **WHEN** the vendor enters a product ID already recorded
- **THEN** the system SHALL reject it and create nothing

### Requirement: Product ID is permanent once used
The display name SHALL be editable at any time. The product ID SHALL be editable only while no
license has been issued for the product. A product with issued licenses SHALL NOT be deleted.

#### Scenario: Rename used product ID
- **WHEN** the vendor tries to change the product ID of a product with issued licenses
- **THEN** the system SHALL refuse and say why

#### Scenario: Delete unused product
- **WHEN** the vendor deletes a product with no issued licenses
- **THEN** the product, its license types and its signing key records SHALL be removed, and the system SHALL tell the vendor which private key files to delete by hand

### Requirement: License types
The vendor SHALL be able to define license types for a product, each with a name unique within
the product ignoring case, a role (`base` or `add-on`), a term of 1 to 120 whole months or
perpetual, and zero or more default features. Default features SHALL be checked with the
library's feature rules (PDR-0024).

#### Scenario: Base type
- **WHEN** the vendor defines `Commerce Pro` for `acme.commerce`, role base, 12 months, features `ecommerce` and `max-orders: 500`
- **THEN** the type SHALL be recorded and offered when issuing a new sale for that product

#### Scenario: Add-on type
- **WHEN** the vendor defines `Extra 1,000 orders`, role add-on, perpetual, feature `max-orders: 1000`
- **THEN** the type SHALL be recorded and offered when issuing an add-on for that product

#### Scenario: Invalid feature
- **WHEN** the vendor adds a feature `Max Orders` or `max-orders: -5`
- **THEN** the system SHALL reject it with the library's reason

### Requirement: Editing and retiring license types
Editing a license type SHALL affect only keys issued afterwards; a renewal that keeps its type
takes features from the license's current key (PDR-0025). Its role SHALL NOT change once a
license has been issued from it. A type with issued licenses SHALL NOT be deleted; it SHALL be
retirable, which hides it from new sales, add-ons and renewals but keeps it as the record of
existing licenses (PDR-0024).

#### Scenario: Retired type
- **WHEN** the vendor retires `Commerce Pro`
- **THEN** it SHALL not be offered for a new sale or a renewal, existing `Commerce Pro` licenses SHALL still show it as their type, and they SHALL be renewable by switching to an active base type

#### Scenario: Role change refused
- **WHEN** the vendor changes the role of a type with issued licenses
- **THEN** the system SHALL refuse and say why

### Requirement: Signing keys per product
Each product SHALL have exactly one current signing key used for issuing, at most one pending
signing key that signs nothing, and any number of retired ones (PDR-0031). For each, the records
SHALL hold the signing key ID, the public key, the private key file's name, its state, and
when it was created, made current and retired. The file name SHALL be made from the product ID
and the signing key ID, and the file SHALL be looked up in the signing keys folder in use; no
folder or path SHALL be recorded (PDR-0032). Wherever signing keys are listed, each SHALL show
its state, and whether its file is usable. The private key SHALL be written only to a file in
the signing keys folder, readable only by the current user where the operating system supports
it, and SHALL NOT be written to the database, the log or the records export (PDR-0007,
PDR-0028).

#### Scenario: Private key not in records
- **WHEN** a product's signing key is created
- **THEN** the database SHALL hold its signing key ID, public key and file name, and no private key material or path

#### Scenario: Key file found after a move
- **WHEN** the private key files are copied to another folder and the signing keys folder is changed to it
- **THEN** issuing SHALL read each key file from the new folder by its file name

### Requirement: Export the public key
The vendor SHALL be able to show any of a product's public keys as PEM and write it to a file,
labelled with its signing key ID and its state (pending, current or retired), for shipping
inside the product.

#### Scenario: Export pending public key
- **WHEN** the vendor exports the pending public key of `acme.commerce`
- **THEN** the system SHALL show and, if asked, write the PEM labelled pending

#### Scenario: Export current public key
- **WHEN** the vendor exports the current public key of `acme.commerce`
- **THEN** the system SHALL show and, if asked, write the PEM, which the library's trusted key set SHALL accept

### Requirement: Rotate the signing key
The vendor SHALL be able to rotate a product's signing key. Rotation SHALL create a new key pair,
write its private key file and record the new key as pending; the current key SHALL keep signing
(PDR-0031). The system SHALL then show the new public key for export and the rotation steps:
export the new public key, ship a release trusting both keys, make the new key current once
sites are likely to run that release, and keep trusting the old public key while any license a
site runs depends on it, until the earliest safe withdrawal shown for it (PDR-0033). Rotation SHALL be refused while the product has a pending key.

#### Scenario: Rotation
- **WHEN** the vendor rotates the signing key of `acme.commerce`
- **THEN** a pending signing key SHALL be recorded with its private key file in the signing keys folder, and keys issued afterwards SHALL still be signed with the current key

#### Scenario: Second rotation refused
- **WHEN** the vendor rotates the signing key of a product that has a pending key
- **THEN** the system SHALL refuse, name the pending key, and offer to make it current or discard it

### Requirement: Make the pending key current
The vendor SHALL be able to make a product's pending signing key current. Before doing so, the
system SHALL ask the vendor to confirm that a release trusting the new public key has shipped,
and SHALL say that keys signed by it fail on sites running an older release. On confirmation the
pending key SHALL become current and the previous current key retired. Before confirmation the
system SHALL show the dependants report for the key being retired (PDR-0033).

#### Scenario: Made current
- **WHEN** the vendor makes the pending key of `acme.commerce` current and confirms
- **THEN** keys issued afterwards SHALL be signed with it, the previous key SHALL be retired, and earlier keys SHALL remain recorded against the key that signed them

#### Scenario: Not confirmed
- **WHEN** the vendor declines the confirmation
- **THEN** the pending key SHALL stay pending and the current key SHALL keep signing

#### Scenario: Dependants shown before confirming
- **WHEN** the vendor makes the pending key of `acme.commerce` current and licenses depend on the current key
- **THEN** the dependants report for the current key SHALL be shown before the confirmation is asked

### Requirement: Make a new key current now
For a compromised key or a private key file that cannot be restored, the vendor SHALL be able to
rotate and make the new key current in one action. The system SHALL warn that every key issued
afterwards fails on sites that have not installed a release trusting the new public key, SHALL
show the dependants report for the key being retired, and SHALL act only on confirmation
(PDR-0031, PDR-0033).

#### Scenario: Emergency switch
- **WHEN** the vendor chooses make current now for `acme.commerce` and confirms the warning
- **THEN** a new signing key SHALL be current, the previous key retired, and the new public key shown for export

### Requirement: Licenses depending on a retired signing key
A recorded key SHALL depend on the signing key that signed it while it is perpetual or its expiry
is on or after the current UTC date, whether it is its license's current key or superseded. A
license SHALL depend on a signing key when any of its keys does. For a retired signing key, and
for the key being retired at make current and make current now, the system SHALL show a
dependants report: the number of depending licenses, split into perpetual (a perpetual
depending key), dated (the current key depends, dated) and superseded only (only superseded keys
depend); and the earliest safe withdrawal, which SHALL be the day after the latest expiry among
the depending keys, "not while perpetual licenses depend on it" when any depending key is
perpetual, and "now" when nothing depends on it. On request it SHALL list each depending license
with its product, reference, and each depending key's identifier, expiry and whether it is
current or superseded. The report SHALL NOT block any action (PDR-0033).

#### Scenario: Superseded key still depends
- **WHEN** on 2027-03-01 key K1 of `acme.commerce` is retired, and license A's key a1, signed by K1 and expiring 2027-06-30, was renewed by a2, signed by K2 and expiring 2028-06-30
- **THEN** license A SHALL be counted as superseded only, and with no other dependants the earliest safe withdrawal SHALL be 2027-07-01

#### Scenario: Perpetual key depends for good
- **WHEN** license B's only key is perpetual and signed by retired key K1
- **THEN** license B SHALL be counted as perpetual, and the earliest safe withdrawal for K1 SHALL be "not while perpetual licenses depend on it"

#### Scenario: Expired key does not depend
- **WHEN** on 2027-03-01 license C's only key is signed by K1 and expired on 2026-12-31
- **THEN** license C SHALL NOT be counted or listed as depending on K1

#### Scenario: Nothing depends
- **WHEN** no recorded key signed by retired key K1 is perpetual or unexpired
- **THEN** the report SHALL show 0 licenses and earliest safe withdrawal "now"

#### Scenario: Expiry today still depends
- **WHEN** on 2027-06-30 a key signed by K1 expires 2027-06-30
- **THEN** its license SHALL still depend on K1 and the earliest safe withdrawal SHALL be no earlier than 2027-07-01

#### Scenario: List on request
- **WHEN** the vendor asks for the list of licenses depending on K1
- **THEN** each depending license SHALL be shown with its product and reference, and each depending key with its identifier, expiry and whether it is current or superseded

### Requirement: Discard a pending key
The vendor SHALL be able to discard a pending signing key. The system SHALL remove its record
and tell the vendor which private key file to delete by hand. A current or retired key SHALL NOT
be discardable.

#### Scenario: Pending key discarded
- **WHEN** the vendor discards the pending key of `acme.commerce`
- **THEN** the product SHALL have no pending key, the current key SHALL be unchanged, and the system SHALL name the private key file to delete

### Requirement: Export signing keys
The vendor SHALL be able to copy every signing key's private key file to a folder it chooses, as
a backup, and SHALL be shown each file's product, signing key ID and state. The system SHALL
refuse a folder that is the data folder, inside it or contains it (PDR-0007), SHALL warn that the
files can sign keys for every product, and SHALL ask before overwriting an existing file. This
export SHALL be separate from the records export (PDR-0028, PDR-0032).

#### Scenario: Keys exported
- **WHEN** the vendor exports signing keys to an empty folder outside the data folder and accepts the warning
- **THEN** every private key file SHALL be copied there, and the system SHALL list each with its product, signing key ID and state

#### Scenario: Data folder refused
- **WHEN** the vendor chooses a folder inside the data folder
- **THEN** the system SHALL refuse, naming the rule, and copy nothing

#### Scenario: Missing file
- **WHEN** a retired key's file is missing at export
- **THEN** the system SHALL copy the others and name the missing file

### Requirement: Missing private key file
When issuing needs a signing key whose private key file is missing, unreadable, or does not
match the recorded public key, the system SHALL refuse to issue and name the file and the folder
searched. It SHALL then offer, in this order: change the signing keys folder; restore the file
from backup and try again; make the product's pending key current, if one exists; make a new key
current now, with its warning (PDR-0031, PDR-0032).

#### Scenario: File missing
- **WHEN** the vendor issues a key and the current signing key's file has been deleted
- **THEN** no key SHALL be issued or recorded, the message SHALL name the file, and the remedies SHALL be offered with changing the signing keys folder first and making a new key current now last

#### Scenario: Pending key offered
- **WHEN** the current signing key's file is missing and the product has a pending key
- **THEN** the system SHALL offer to make the pending key current, with the warning for keys issued before sites upgrade

## Purpose

Holds what the vendor sells: products, the license types offered for each, and each product's
signing keys, so licenses can be issued from a template with the right key.

Serves the **vendor**. Product decisions: PDR-0023, PDR-0024, PDR-0007, PDR-0011, PDR-0017;
signing keys follow the `signing-key-management` capability.

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
Editing a license type SHALL affect only keys issued afterwards. Its role SHALL NOT change once a
license has been issued from it. A type with issued licenses SHALL NOT be deleted; it SHALL be
retirable, which hides it from new sales and add-ons but keeps it for existing licenses and
their renewals.

#### Scenario: Retired type
- **WHEN** the vendor retires `Commerce Pro`
- **THEN** it SHALL not be offered for a new sale, and existing `Commerce Pro` licenses SHALL still be renewable

#### Scenario: Role change refused
- **WHEN** the vendor changes the role of a type with issued licenses
- **THEN** the system SHALL refuse and say why

### Requirement: Signing keys per product
Each product SHALL have exactly one current signing key used for issuing, and any number of
earlier ones. For each, the records SHALL hold the signing key ID, the public key, the private
key file's location and when it was created and retired. The private key SHALL be written only
to a file in the signing keys folder, readable only by the current user where the operating
system supports it, and SHALL NOT be written to the database, the log or an export (PDR-0007).

#### Scenario: Private key not in records
- **WHEN** a product's signing key is created
- **THEN** the database SHALL hold its signing key ID, public key and file location, and no private key material

### Requirement: Export the public key
The vendor SHALL be able to show any of a product's public keys as PEM and write it to a file,
labelled with its signing key ID and whether it is current, for shipping inside the product.

#### Scenario: Export current public key
- **WHEN** the vendor exports the current public key of `acme.commerce`
- **THEN** the system SHALL show and, if asked, write the PEM, which the library's trusted key set SHALL accept

### Requirement: Rotate the signing key
The vendor SHALL be able to rotate a product's signing key. Rotation SHALL create a new key pair,
write its private key file, make it current for issuing, and mark the previous key retired. It
SHALL then show the new public key for export and the rotation steps: ship the new public key
alongside the old, and withdraw the old one only once no valid license depends on it. For each
earlier signing key the system SHALL show how many licenses whose current key it signed are not
expired.

#### Scenario: Rotation
- **WHEN** the vendor rotates the signing key of `acme.commerce`
- **THEN** keys issued afterwards SHALL be signed with the new key, and earlier keys SHALL remain recorded against the old one

#### Scenario: Dependent licenses shown
- **WHEN** two unexpired licenses' current keys were signed by the retired key
- **THEN** the system SHALL report 2 licenses still depending on it

### Requirement: Missing private key file
When issuing needs a signing key whose private key file is missing, unreadable, or does not
match the recorded public key, the system SHALL refuse to issue, name the file, and tell the
vendor to restore it from backup or rotate the signing key.

#### Scenario: File missing
- **WHEN** the vendor issues a key and the current signing key's file has been deleted
- **THEN** no key SHALL be issued or recorded, and the message SHALL name the file

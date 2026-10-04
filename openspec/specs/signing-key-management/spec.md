# signing-key-management Specification

## Purpose

Provides the issuer-side API for creating signing key pairs identified by a signing key ID, and the consumer-side set of trusted public keys that makes rotating a signing key possible.

Serves the **vendor**, who owns one private signing key per product and must rotate it without breaking issued licenses, and the **site owner**, whose licenses keep working through a rotation. Personas are defined in `docs/personas.md`.

Terms: the **signing key ID** names a signing key pair. It is not the **key identifier** (`LIC-XXXXX-XXXXX-XXXX`, PDR-0020), which names one license key.

## Requirements

### Requirement: Create a signing key pair
The system SHALL create a new signing key pair and assign it a signing key ID, and SHALL make the private key and the public key separately exportable, so the private key can be kept in custody and only the public key shipped inside a product.

#### Scenario: Key pair created
- **WHEN** a caller requests a new signing key pair
- **THEN** the system SHALL return a private key, the matching public key and a signing key ID, each obtainable without the others

#### Scenario: Public key carries its signing key ID
- **WHEN** a public key is exported
- **THEN** its signing key ID SHALL be exported with it, so a validator can trust it without further input

### Requirement: Trusted key set
The system SHALL let a consumer hold a set of trusted public keys addressed by signing key ID, and SHALL resolve a license key's signing key ID against that set during evaluation.

#### Scenario: Several keys trusted at once
- **WHEN** a consumer trusts two public keys with different signing key IDs
- **THEN** a license signed with either key SHALL verify

### Requirement: Rotation
Adding a public key to the trusted set SHALL NOT affect licenses signed with a key already trusted. Removing a public key from the trusted set SHALL cause licenses signed with it to be reported as signing key not recognised (PDR-0019).

#### Scenario: New key added
- **WHEN** a license was signed under signing key ID A and the consumer adds signing key ID B to the trusted set
- **THEN** the license SHALL still verify

#### Scenario: Old key withdrawn
- **WHEN** a license was signed under signing key ID A and the consumer removes A from the trusted set
- **THEN** the license SHALL be reported as signing key not recognised

### Requirement: Signing key ID unique within a trusted set
The system SHALL reject adding a public key to a trusted set under a signing key ID already held for a different public key.

#### Scenario: Duplicate signing key ID
- **WHEN** a consumer adds a public key under a signing key ID already held for a different public key
- **THEN** the system SHALL raise an error and the trusted set SHALL be unchanged

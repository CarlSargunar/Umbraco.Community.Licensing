## Purpose

Provides the issuer-side API for creating signing key pairs, identifying them by key ID, and the consumer-side trusted key set that makes rotation possible.

Serves the **vendor**, who owns one private key per product and must rotate it without breaking issued licenses, and the **site owner**, whose licenses keep working through a rotation. Personas are defined in `docs/personas.md`.

## ADDED Requirements

### Requirement: Create a signing key pair
The system SHALL create a new signing key pair and assign it a key ID, and SHALL make the private key and the public key separately exportable so the private key can be kept in custody and only the public key shipped inside a product.

#### Scenario: Key pair created
- **WHEN** a caller requests a new signing key pair
- **THEN** the system SHALL return a private key, the matching public key and a key ID, each obtainable without the others

#### Scenario: Public key carries its key ID
- **WHEN** a public key is exported
- **THEN** its key ID SHALL be exported with it, so a validator can register it as a trusted key without further input

### Requirement: Trusted key set
The system SHALL let a consumer hold a set of trusted public keys addressed by key ID, and SHALL resolve a license key's embedded key ID against that set during validation.

#### Scenario: Several keys trusted at once
- **WHEN** a consumer trusts two public keys with different key IDs
- **THEN** a license signed with either key SHALL validate

### Requirement: Rotation
Adding a new trusted key SHALL NOT invalidate licenses signed with an existing trusted key, and removing a key from the trusted set SHALL cause licenses signed with it to fail validation with an untrusted key ID result.

#### Scenario: New key added
- **WHEN** a license was signed under key ID A and the consumer adds key ID B to the trusted set
- **THEN** the license SHALL still validate

#### Scenario: Old key withdrawn
- **WHEN** a license was signed under key ID A and the consumer removes key ID A from the trusted set
- **THEN** validation SHALL fail with an untrusted key ID result

### Requirement: Key ID uniqueness within a trusted set
The system SHALL reject adding a public key to a trusted set under a key ID that is already present with a different public key.

#### Scenario: Duplicate key ID
- **WHEN** a consumer adds a public key under a key ID already held for a different public key
- **THEN** the system SHALL raise an error and the trusted set SHALL be unchanged

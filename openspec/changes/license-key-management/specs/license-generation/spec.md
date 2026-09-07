## Purpose

Provides the issuer-side API for constructing and cryptographically signing license keys that encode a mandatory product ID and optional expiry / supported-version claims.

## ADDED Requirements

### Requirement: Generate a license key from claims
The system SHALL accept a mandatory product ID, an optional expiry date, and an optional supported Umbraco core version range, and produce a signed license key string encoding those claims.

#### Scenario: Generate minimal license
- **WHEN** a caller requests a license key with only a product ID
- **THEN** the system SHALL produce a signed license key encoding that product ID with no expiry and no version constraint

#### Scenario: Generate license with expiry
- **WHEN** a caller requests a license key with a product ID and an expiry date
- **THEN** the system SHALL produce a signed license key encoding both the product ID and the expiry date

#### Scenario: Generate license with version range
- **WHEN** a caller requests a license key with a product ID and a supported Umbraco core version range
- **THEN** the system SHALL produce a signed license key encoding the product ID and the version range

### Requirement: Reject invalid generation input
The system SHALL reject a generation request that is missing a product ID, or whose optional claims are invalid (for example, a malformed expiry date, or a version range whose minimum exceeds its maximum).

#### Scenario: Missing product ID
- **WHEN** a caller requests a license key without a product ID
- **THEN** the system SHALL raise an error and SHALL NOT produce a key

#### Scenario: Invalid version range
- **WHEN** a caller requests a license key with a version range whose minimum exceeds its maximum
- **THEN** the system SHALL raise an error and SHALL NOT produce a key

### Requirement: Key ID embedding for rotation
Every generated license key SHALL embed an identifier of the signing key used to produce it, so that a validator can later resolve which trusted public key to verify against without first trusting the signature.

#### Scenario: Key ID present in output
- **WHEN** a license key is generated using a specific signing key
- **THEN** the resulting license key SHALL contain an identifier that resolves to the corresponding public key before signature verification occurs

### Requirement: Private key isolation
The generation API SHALL require the caller to supply the private signing key explicitly for each call, and SHALL NOT embed, cache, or persist private key material anywhere accessible after generation completes.

#### Scenario: No private key retained
- **WHEN** a license key has been generated
- **THEN** no private key material SHALL be stored or exposed by the system after the call returns

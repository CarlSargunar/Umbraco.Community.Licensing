## Purpose

Provides the consumer-side API for verifying a license key's signature and evaluating its claims against the running product and Umbraco core version.

## ADDED Requirements

### Requirement: Verify signature against trusted public keys
The system SHALL verify a license key's signature using the trusted public key matching its embedded key ID, and SHALL reject the key as invalid if no matching trusted public key is configured or if the signature does not verify.

#### Scenario: Valid signature
- **WHEN** a license key is validated whose key ID matches a configured trusted public key and whose signature verifies against that key
- **THEN** validation SHALL proceed to evaluate the key's claims

#### Scenario: Unknown key ID
- **WHEN** a license key's embedded key ID does not match any configured trusted public key
- **THEN** the system SHALL reject the key as invalid without evaluating its claims

#### Scenario: Tampered payload
- **WHEN** a license key's payload has been altered after signing
- **THEN** signature verification SHALL fail and the system SHALL reject the key as invalid

### Requirement: Product ID match
The system SHALL reject a license key whose encoded product ID does not match the product ID being validated against.

#### Scenario: Product mismatch
- **WHEN** a validly signed license key encodes a product ID different from the product being validated
- **THEN** validation SHALL fail with a product-mismatch result

### Requirement: Expiry check
If a license key encodes an expiry date, the system SHALL reject it once the current date/time is after that expiry. If no expiry is encoded, the key SHALL be treated as perpetual with respect to this check.

#### Scenario: Expired key
- **WHEN** a license key's encoded expiry date is before the current date/time
- **THEN** validation SHALL fail with an expired result

#### Scenario: Unexpired or perpetual key
- **WHEN** a license key's encoded expiry date is after the current date/time, or no expiry is encoded
- **THEN** validation SHALL NOT fail on expiry grounds

### Requirement: Supported version range check
If a license key encodes a supported Umbraco core version range, the system SHALL reject it when the running Umbraco core version falls outside that range. If no version range is encoded, the key SHALL be treated as compatible with any Umbraco core version.

#### Scenario: Version within range
- **WHEN** a license key encodes a supported version range that includes the running Umbraco core version
- **THEN** validation SHALL NOT fail on version grounds

#### Scenario: Version outside range
- **WHEN** a license key encodes a supported version range that excludes the running Umbraco core version
- **THEN** validation SHALL fail with a version-unsupported result

#### Scenario: No version range encoded
- **WHEN** a license key encodes no supported version range
- **THEN** validation SHALL NOT fail on version grounds regardless of the running Umbraco core version

### Requirement: Distinct validation result reasons
The system SHALL return a validation result that distinguishes, at minimum: valid, invalid signature, untrusted key ID, product mismatch, expired, and unsupported version, so that a host application can present an accurate outcome.

#### Scenario: Distinct failure reasons
- **WHEN** validation fails for any of the defined reasons
- **THEN** the returned result SHALL identify which specific reason caused the failure

## Purpose

Provides the consumer-side API for verifying a license key's signature and evaluating its claims against the product and the current time.

Serves the **vendor**, whose product checks its license at runtime, and the **implementor** and **site owner**, who act on the result. Personas are defined in `docs/personas.md`.

Scope note (2026-10-01): evaluating a set of keys for one product (superseding, base and add-on combining, feature combining; design.md R12) is decided in PDRs and `docs/license-examples.md` and is not yet reflected here. The Umbraco version range check was removed from this change (`docs/deferred-scope.md` D1). The caller supplies the key strings; where they come from is outside this change (D2).

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

### Requirement: Distinct validation result reasons
The system SHALL return a validation result that distinguishes, at minimum: valid, malformed key, invalid signature, untrusted key ID, product mismatch, and expired, so that a caller can present an accurate outcome.

#### Scenario: Distinct failure reasons
- **WHEN** validation fails for any of the defined reasons
- **THEN** the returned result SHALL identify which specific reason caused the failure

### Requirement: Validation never throws on a bad key
The system SHALL return a result for any input string, including empty, truncated, malformed or tampered keys, and SHALL NOT throw an exception because of the key's content.

#### Scenario: Malformed key
- **WHEN** a string that is not a well-formed license key is validated
- **THEN** the system SHALL return a result with a malformed-key reason and SHALL NOT throw

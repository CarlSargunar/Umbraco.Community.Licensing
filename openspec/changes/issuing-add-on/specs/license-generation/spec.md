## ADDED Requirements

### Requirement: Check a request without signing
The system SHALL check a license request against every rule applied at issue and SHALL return
every problem found, without signing, generating a reference or needing a signing key. A request
that passes the check SHALL be issued by the same rules, given the same current UTC date. This
lets vendor tooling validate input with the library's own rules (PDR-0023).

#### Scenario: Problems listed
- **WHEN** a caller checks a request with product ID `Acme.Commerce` and feature `max-orders: -5`
- **THEN** the system SHALL return both problems, naming the fields, and produce no key

#### Scenario: Valid request
- **WHEN** a caller checks a request that issuing would accept
- **THEN** the system SHALL return no problems

### Requirement: Read a license reference
The system SHALL read a license reference or key identifier supplied as text, ignoring case,
hyphens and spaces, and SHALL return its displayed form (`LIC-XXXXX-XXXXX` or
`LIC-XXXXX-XXXXX-XXXX`), or report that the text is neither.

#### Scenario: Reference read
- **WHEN** a caller reads `lic 8f3ak m7rxb`
- **THEN** the system SHALL return reference `LIC-8F3AK-M7RXB`

#### Scenario: Key identifier read
- **WHEN** a caller reads `lic-8f3akm7rxb-7q2d`
- **THEN** the system SHALL return key identifier `LIC-8F3AK-M7RXB-7Q2D` and its reference `LIC-8F3AK-M7RXB`

#### Scenario: Neither
- **WHEN** a caller reads `LIC-8F3AK-M7RXO`
- **THEN** the system SHALL report it is not a reference or key identifier

## MODIFIED Requirements

### Requirement: Generate a license key from its contents
The system SHALL accept a product ID, a role, an optional license reference, an optional vendor tag, an expiry stated as a date or as perpetual (PDR-0029), zero or more features and a private signing key, and SHALL produce a signed key string. The signing key ID SHALL be derived from the private key, not supplied by the caller. It SHALL also return the license reference, the key identifier and the issue time of the key it produced. It SHALL keep no record of the key (PDR-0005).

#### Scenario: Minimal base license
- **WHEN** a caller issues a key with product `acme.seo-toolkit`, role `base` and perpetual only
- **THEN** the system SHALL produce a key with a new license reference, no expiry and no features

#### Scenario: Full contents
- **WHEN** a caller issues a key with product, role, expiry and features
- **THEN** a validator trusting the signing key SHALL evaluate the key as verified and report exactly those contents

#### Scenario: Nothing retained
- **WHEN** a key has been issued
- **THEN** the system SHALL hold no record of the product, the contents or the key

### Requirement: Expiry date
Every request SHALL state its expiry as exactly one of: an expiry date, or perpetual (PDR-0029). The system SHALL reject a request that states neither or both. An expiry date SHALL be a date; the system SHALL reject an expiry before the current UTC date, and SHALL accept an expiry of the current UTC date. A perpetual request SHALL produce a key with no expiry.

#### Scenario: Past expiry
- **WHEN** a caller issues on 2026-10-01 with expiry 2026-09-30
- **THEN** the system SHALL reject the request and produce no key

#### Scenario: Expiry today
- **WHEN** a caller issues on 2026-10-01 with expiry 2026-10-01
- **THEN** the system SHALL produce the key

#### Scenario: Perpetual
- **WHEN** a caller issues a key stated as perpetual
- **THEN** the system SHALL produce a key with no expiry, which never expires when evaluated

#### Scenario: Expiry not stated
- **WHEN** a caller issues a key with neither an expiry date nor perpetual
- **THEN** the system SHALL reject the request, naming the expiry, and produce no key

#### Scenario: Both stated
- **WHEN** a caller issues a key with an expiry date and perpetual
- **THEN** the system SHALL reject the request, naming the expiry, and produce no key

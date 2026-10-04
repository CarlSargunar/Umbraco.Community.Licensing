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

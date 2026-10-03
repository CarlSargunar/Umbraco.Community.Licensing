## Purpose

Provides the issuer-side API that checks a license key's contents and signs them into a key string a site owner can paste anywhere, without keeping any record of what it issued.

Serves the **vendor**, who issues keys when a site owner buys a license, and the **site owner**, who must never receive a key that is wrong or dead on arrival. Personas are defined in `docs/personas.md`. Product decisions: PDR-0005, PDR-0006, PDR-0009, PDR-0010, PDR-0011, PDR-0013, PDR-0016, PDR-0017, PDR-0018, PDR-0020; key schema and the issue-time rejections in `docs/license-examples.md` (schema, example 19).

The Umbraco version range claim was removed from this change (`docs/deferred-scope.md` D1). Records of issued keys belong to the optional issuing add-on, a separate change (PDR-0006).

## ADDED Requirements

### Requirement: Generate a license key from its contents
The system SHALL accept a product ID, a role, an optional license reference, an optional expiry date, zero or more features and a private signing key, and SHALL produce a signed key string. The signing key ID SHALL be derived from the private key, not supplied by the caller. It SHALL also return the license reference, the key identifier and the issue time of the key it produced. It SHALL keep no record of the key (PDR-0005).

#### Scenario: Minimal base license
- **WHEN** a caller issues a key with product `acme.seo-toolkit` and role `base` only
- **THEN** the system SHALL produce a key with a new license reference, no expiry and no features

#### Scenario: Full contents
- **WHEN** a caller issues a key with product, role, expiry and features
- **THEN** a validator trusting the signing key SHALL evaluate the key as verified and report exactly those contents

#### Scenario: Nothing retained
- **WHEN** a key has been issued
- **THEN** the system SHALL hold no record of the product, the contents or the key

### Requirement: Role is required
The caller SHALL supply a role of `base` or `add-on`. The system SHALL reject a request with no role or any other value; there is no default.

#### Scenario: No role
- **WHEN** a caller issues a key without a role
- **THEN** the system SHALL reject the request and produce no key

### Requirement: License reference, new or kept
When no license reference is supplied, the system SHALL generate one: 10 random characters from uppercase letters and digits excluding `0 O 1 I L`, written `LIC-XXXXX-XXXXX`. When a reference is supplied (a reissue), the system SHALL keep it, accepting it ignoring case, hyphens and spaces, and SHALL reject one that is not a valid reference.

#### Scenario: First issue
- **WHEN** a caller issues a key without a reference
- **THEN** the key SHALL carry a newly generated reference in the required format

#### Scenario: Reissue
- **WHEN** a caller issues a key with reference `lic-8f3ak m7rxb`
- **THEN** the key SHALL carry reference `LIC-8F3AK-M7RXB`

#### Scenario: Invalid reference
- **WHEN** a caller supplies a reference containing `O` or of the wrong length
- **THEN** the system SHALL reject the request and produce no key

### Requirement: Key part and issue time are set by the system
At every issue, including a reissue, the system SHALL generate a key part of 4 random characters from the reference's alphabet, and SHALL set the issue time to the current UTC time to the second, from the clock supplied to it. Neither SHALL be accepted as caller input.

#### Scenario: Reissue gets a new key part and time
- **WHEN** a caller issues two keys under the same reference
- **THEN** each key SHALL carry its own key part and its own issue time

#### Scenario: Issue time to the second
- **WHEN** a key is issued at 2026-09-28T14:30:22.734Z
- **THEN** its issue time SHALL be 2026-09-28T14:30:22Z

#### Scenario: No caller-supplied issue time or key part
- **WHEN** the caller attempts to supply an issue time or a key part
- **THEN** the system SHALL offer no way to do so

### Requirement: Key identifier at the start of the key
Every key string SHALL start with its key identifier, `LIC-XXXXX-XXXXX-XXXX` (reference plus key part), written as displayed, followed by the rest of the key. The identifier SHALL be covered by the signature.

#### Scenario: Visible identifier
- **WHEN** a key is issued under reference `LIC-8F3AK-M7RXB` with key part `7Q2D`
- **THEN** the key string SHALL start with `LIC-8F3AK-M7RXB-7Q2D`

#### Scenario: Edited identifier
- **WHEN** the identifier at the start of an issued key is changed
- **THEN** a validator SHALL report the key as not verified

### Requirement: Product ID format
The system SHALL reject a product ID that is not two dot-separated parts of lowercase `a`-`z`, digits and hyphens, each starting with a letter.

#### Scenario: Valid product ID
- **WHEN** a caller issues a key for `acme.commerce`
- **THEN** the system SHALL accept the product ID

#### Scenario: Invalid product ID
- **WHEN** a caller issues a key for `commerce` or `Acme.Commerce`
- **THEN** the system SHALL reject the request and produce no key

### Requirement: Expiry date
An expiry, when supplied, SHALL be a date. The system SHALL reject an expiry before the current UTC date. An expiry of the current UTC date SHALL be accepted.

#### Scenario: Past expiry
- **WHEN** a caller issues on 2026-10-01 with expiry 2026-09-30
- **THEN** the system SHALL reject the request and produce no key

#### Scenario: Expiry today
- **WHEN** a caller issues on 2026-10-01 with expiry 2026-10-01
- **THEN** the system SHALL produce the key

### Requirement: Feature rules
Each feature SHALL have a name of lowercase `a`-`z`, digits and hyphens starting with a letter, appearing at most once in the key, and a value that is a switch, a number or text. The system SHALL reject:
- a name breaking the name rule, or repeated in one key;
- an explicit `false`;
- a number that is negative, malformed, has more than 4 decimal places or more than 15 digits;
- text that is empty, longer than 256 characters, has leading or trailing whitespace, or contains a line break or other control character.

The value's type SHALL be the type the caller issued; text SHALL never be read as a number.

#### Scenario: Valid features
- **WHEN** a caller issues `ecommerce` (switch), `storage-gb: 2.5` (number) and `licensed-domain: example.com` (text)
- **THEN** the system SHALL produce the key with those features and types

#### Scenario: Invalid feature
- **WHEN** a caller issues any of `pro: false`, `max-orders: -200`, `storage-gb: 2.12345`, `Max Orders: 500`, `licensed-domain: " example.com"`, or `max-orders` twice
- **THEN** the system SHALL reject the request and produce no key

#### Scenario: Numeric-looking text
- **WHEN** a caller issues `max-orders` as the text `500`
- **THEN** the key SHALL carry it as text

### Requirement: Rejection reports every problem
A rejected request SHALL raise an error that identifies every rule the request breaks, not only the first, and SHALL produce no key.

#### Scenario: Several problems
- **WHEN** a request has no role and a negative number
- **THEN** the error SHALL identify both

### Requirement: Signing key ID embedded
Every key SHALL carry the signing key ID of the signing key that produced it, readable before the signature is checked, so a validator can choose which trusted public key to verify against.

#### Scenario: Signing key ID present
- **WHEN** a key is issued with the private key of the signing key pair with signing key ID A
- **THEN** a validator SHALL be able to read signing key ID A from the key before verifying it

### Requirement: Private key isolation
The caller SHALL supply the private signing key for each issue. The system SHALL NOT embed, cache or persist private key material anywhere accessible after the call returns.

#### Scenario: No private key retained
- **WHEN** a key has been issued
- **THEN** no private key material SHALL be stored or exposed by the system after the call returns

### Requirement: Single-line transmissible key
A key string SHALL be a single line of printable characters with no whitespace, so that it survives email and pasting into a configuration value, an environment variable or a vault secret.

#### Scenario: Key survives paste
- **WHEN** an issued key is copied through an email and pasted back
- **THEN** a validator SHALL read it as the key issued, including when the email client wrapped it (PDR-0021)

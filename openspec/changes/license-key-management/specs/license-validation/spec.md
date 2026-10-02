## Purpose

Provides the consumer-side API that evaluates a set of license keys for one product: verifies each key, decides each key's state, and reports the combined entitlement for the product.

Serves the **vendor**, whose product checks its licenses at runtime, the **implementor**, who fixes the key a row names, and the **site owner**, who needs to know what they hold. Personas are defined in `docs/personas.md`. Product decisions: PDR-0009, PDR-0010, PDR-0011, PDR-0013, PDR-0016, PDR-0018, PDR-0019, PDR-0020, PDR-0021; worked examples in `docs/license-examples.md`.

The caller supplies the key strings; where they come from is outside this change (`docs/deferred-scope.md` D2). The Umbraco version range check was removed from this change (D1).

## ADDED Requirements

### Requirement: Evaluate a set of keys for one product
The system SHALL accept an ordered list of zero or more key strings, one product ID, a set of trusted signing keys and a clock, and SHALL return one row per supplied string, in the order supplied, plus one product result. The product result SHALL NOT depend on the order of the strings.

#### Scenario: One row per string, in order
- **WHEN** three key strings are evaluated
- **THEN** the result SHALL contain three rows, the first for the first string supplied, and so on

#### Scenario: No keys
- **WHEN** an empty list is evaluated
- **THEN** the result SHALL contain no rows and the product SHALL be reported as not licensed with no features

#### Scenario: Order does not change the product result
- **WHEN** the same set of keys is evaluated in two different orders
- **THEN** the product result SHALL be the same both times

### Requirement: Whitespace in a key string is ignored
The system SHALL remove all whitespace (spaces, tabs, line breaks), at either end or anywhere inside, from each supplied string before reading it. Two strings that differ only in whitespace SHALL be treated as the same key.

#### Scenario: Key wrapped by an email client
- **WHEN** a valid key is supplied with a line break and indentation inside it and a trailing newline
- **THEN** its row SHALL be evaluated as the key without the whitespace

#### Scenario: Same key with and without whitespace
- **WHEN** a key is supplied once as issued and once with a trailing newline
- **THEN** the second row SHALL be duplicate of the first

### Requirement: Rows name keys by key identifier
Each row SHALL name its key by the key identifier at the start of the key string (`LIC-XXXXX-XXXXX-XXXX`), when that start is readable. A row whose string has no readable identifier SHALL be identified by its position only. No row and no product result SHALL contain any part of a key string other than its identifier.

#### Scenario: Key cut off at the end
- **WHEN** a key string is truncated after its identifier
- **THEN** its row SHALL name the identifier and report the key as unreadable

#### Scenario: String with no identifier
- **WHEN** a string that does not start with a key identifier is evaluated
- **THEN** its row SHALL carry no identifier and SHALL be identifiable by position

#### Scenario: No key material in the result
- **WHEN** any set of keys is evaluated
- **THEN** no row and no product result SHALL contain any part of a key string other than a key identifier

### Requirement: One state per key, first failing check wins
The system SHALL give each row exactly one state, decided by the first check that applies, in this order: unreadable, wrong product, signing key not recognised, not verified, duplicate, superseded, expired, inactive, valid. The first four are failure reasons; a key reaching the fifth check is verified.

#### Scenario: Unreadable
- **WHEN** a string, with whitespace removed, cannot be decoded as a license key
- **THEN** its row SHALL be unreadable

#### Scenario: Verified contents that break the schema
- **WHEN** a key verifies against a trusted signing key but its contents break a rule checked at issue, such as a missing role, a negative number or a repeated feature name
- **THEN** its row SHALL be unreadable, SHALL report its identifier, and the key SHALL take no part in superseding or combining

#### Scenario: Wrong product before signature
- **WHEN** a readable key claims a product other than the one being evaluated, and its signing key is not trusted
- **THEN** its row SHALL be wrong product, not signing key not recognised

#### Scenario: Signing key not recognised
- **WHEN** a readable key for the product names a signing key that is not in the trusted set
- **THEN** its row SHALL be signing key not recognised

#### Scenario: Not verified
- **WHEN** a readable key for the product names a trusted signing key and its signature does not verify, including when its visible identifier or any claim was altered
- **THEN** its row SHALL be not verified

#### Scenario: Valid
- **WHEN** a key verifies, is not a duplicate, is not superseded, has not expired, and is a base or an add-on with a valid base present
- **THEN** its row SHALL be valid

### Requirement: A failed key reports its claimed identifiers only
A row whose state is unreadable, wrong product, signing key not recognised or not verified SHALL report its reason, its claimed product ID when readable, and its claimed key identifier when readable, each marked as a claim. It SHALL NOT report the key's role, issue time, expiry or features. A failed key SHALL take no part in superseding or combining.

#### Scenario: Edited key
- **WHEN** a key edited to claim `expires 2099-12-31` and `max-orders 999999` is evaluated
- **THEN** its row SHALL be not verified, SHALL report the claimed product and identifier, and SHALL NOT report the expiry or the feature

#### Scenario: Another product's key
- **WHEN** a key for `zenith.commerce-shipping` is evaluated for `acme.commerce`
- **THEN** its row SHALL be wrong product and SHALL report `zenith.commerce-shipping` and its identifier as claims

### Requirement: A verified key reports its claims
A row for a verified key SHALL report its product ID, role, key identifier, issue time, expiry (or that it never expires) and features, as facts.

#### Scenario: Verified key row
- **WHEN** a verified base key with expiry 2027-03-01 and features `ecommerce` and `max-orders: 500` is evaluated
- **THEN** its row SHALL report all of those claims, whatever its state

### Requirement: Duplicates count once
When verified keys with identical signed contents are supplied more than once, the first SHALL be evaluated normally and each further copy SHALL be reported as duplicate, naming the row it copies. A duplicate SHALL take no part in superseding or combining.

#### Scenario: Same key in two places
- **WHEN** a key granting `max-orders: 500` is supplied twice
- **THEN** the first row SHALL be valid, the second duplicate of the first, and the product SHALL have `max-orders` 500

### Requirement: Later reissue supersedes
Among verified, non-duplicate keys for the product, a key SHALL be superseded when another key with the same license reference was issued strictly later. Expiry and role SHALL play no part: an expired key still supersedes, and a later key supersedes whatever its role. A superseded row SHALL note when its role differed from the key that superseded it. A superseded key SHALL take no part in combining.

#### Scenario: Renewal left beside the old key
- **WHEN** two keys share a reference, issued 2025-03-01 and 2026-03-01, each with `max-orders: 500`
- **THEN** the older SHALL be superseded and the product SHALL have `max-orders` 500

#### Scenario: Expired reissue still supersedes
- **WHEN** a key expiring 2099-03-01 is superseded by a reissue under the same reference that has since expired
- **THEN** the older key SHALL stay superseded, the reissue SHALL be expired, and neither SHALL count

#### Scenario: Reissue changes the role
- **WHEN** a base key is superseded by a later add-on key under the same reference
- **THEN** the base row SHALL be superseded with a note that its role was base

#### Scenario: Failed key does not supersede
- **WHEN** a valid key and a not-verified key claiming the same reference are evaluated
- **THEN** the valid key SHALL stay valid

### Requirement: Keys tied for latest all count
When two or more different verified, non-duplicate keys share a reference and the latest issue time under it, each SHALL be evaluated as if not superseded and SHALL carry a vendor-error flag naming the identifiers of the others. The flag SHALL NOT apply to keys that are superseded.

#### Scenario: Reissue submitted twice
- **WHEN** two different keys under one reference, both issued 2026-09-28T14:30:22Z with `max-orders: 2000`, are the latest under it
- **THEN** both rows SHALL be valid with a vendor-error flag naming each other, and the product SHALL have `max-orders` 4000

#### Scenario: Tie resolved by a later reissue
- **WHEN** a third key under the same reference, issued later, is added
- **THEN** the two tied keys SHALL be superseded without a vendor-error flag

### Requirement: Expiry
A verified key with an expiry date SHALL be expired once the current UTC time is after the end of that date (23:59:59 UTC). A key with no expiry SHALL never expire. The current time SHALL come from the clock supplied to the evaluation.

#### Scenario: Last day
- **WHEN** a key expiring 2027-03-01 is evaluated at 2027-03-01T23:59:59Z
- **THEN** it SHALL NOT be expired

#### Scenario: Day after
- **WHEN** the same key is evaluated at 2027-03-02T00:00:00Z
- **THEN** it SHALL be expired

#### Scenario: No expiry
- **WHEN** a key with no expiry is evaluated at any time
- **THEN** it SHALL NOT be expired

### Requirement: Base and add-on
The product SHALL be licensed when at least one row is a valid base. An add-on that would otherwise be valid SHALL be inactive when no row is a valid base. The product result SHALL report how many valid base licenses count.

#### Scenario: Add-on alone
- **WHEN** only a valid add-on key is evaluated
- **THEN** its row SHALL be inactive and the product SHALL be not licensed

#### Scenario: Base expires, add-ons have not
- **WHEN** the base has expired and two add-ons have not
- **THEN** the base row SHALL be expired, both add-on rows inactive, and the product not licensed

#### Scenario: Two bases
- **WHEN** two valid base keys with different references are evaluated
- **THEN** both SHALL count and the product result SHALL report two valid base licenses

### Requirement: Combined features
The product's features SHALL combine the features of every valid row (valid bases and active add-ons). A switch SHALL be granted when any counted key grants it. Numbers under one name SHALL be summed exactly at the precision written. Text values under one name that are exactly equal (same characters, same case) SHALL count as one value. Different text values, or text alongside a switch or number under one name, SHALL be a conflict: the feature SHALL answer nothing at product level, and the product result and every counted row carrying that name SHALL be flagged with the conflict. A conflict SHALL NOT change any row's state.

#### Scenario: Base plus add-ons
- **WHEN** a valid base with `ecommerce, max-orders: 500` and active add-ons with `max-orders: 1000`, `max-orders: 500` and `ai-assist` are evaluated
- **THEN** the product SHALL have `ecommerce`, `ai-assist` and `max-orders` 2000

#### Scenario: Exact decimal sum
- **WHEN** counted keys carry `storage-gb` 10, 2.5 and 2.5
- **THEN** the product SHALL have `storage-gb` 15 exactly

#### Scenario: Equal text
- **WHEN** a base and an add-on both carry `licensed-domain: example.com`
- **THEN** the product SHALL have `licensed-domain` `example.com`

#### Scenario: Conflicting text
- **WHEN** a base carries `licensed-domain: example.com` and an add-on carries `licensed-domain: exmaple.com`
- **THEN** `licensed-domain` SHALL answer nothing, both rows SHALL stay valid and carry a conflict flag, and the product's other features SHALL be unaffected

#### Scenario: Not licensed
- **WHEN** the product is not licensed
- **THEN** every feature SHALL answer not granted

### Requirement: Feature lookup
A feature looked up by name SHALL be found ignoring case of the letters `a`-`z`, with the same answer on every server whatever its language settings. A name no counted key carries SHALL answer not granted.

#### Scenario: Case-insensitive lookup
- **WHEN** a product holding `max-orders` is asked for `Max-Orders`
- **THEN** the lookup SHALL find `max-orders`

#### Scenario: Unknown name
- **WHEN** a product is asked for a feature no counted key carries
- **THEN** the lookup SHALL answer not granted

### Requirement: Evaluation never throws on a bad key
The system SHALL return a result for any key string, including empty, truncated, malformed or altered strings, and SHALL NOT throw an exception because of a key's content.

#### Scenario: Garbage input
- **WHEN** a list containing an empty string and a pasted password is evaluated
- **THEN** the system SHALL return a row for each, both unreadable, and SHALL NOT throw

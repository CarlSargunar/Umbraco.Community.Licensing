## ADDED Requirements

### Requirement: Licenses counted
The product SHALL be licensed when at least one row is valid. The product result SHALL report how many licenses count: the number of different license references among valid rows. Keys tied for latest under one reference SHALL count as one license (PDR-0038).

#### Scenario: One license
- **WHEN** one valid key is evaluated
- **THEN** the product SHALL be licensed and the product result SHALL report one license

#### Scenario: Only an expired key
- **WHEN** the only verified key has expired
- **THEN** the product SHALL be not licensed and the product result SHALL report no licenses

#### Scenario: Two licenses
- **WHEN** two valid keys with different references are evaluated
- **THEN** both SHALL count and the product result SHALL report two licenses

#### Scenario: Tied keys are one license
- **WHEN** two different keys under one reference, tied for latest, are both valid
- **THEN** the product result SHALL report one license

## MODIFIED Requirements

### Requirement: One state per key, first failing check wins
The system SHALL give each row exactly one state, decided by the first check that applies, in this order: unreadable, wrong product, signing key not recognised, not verified, duplicate, superseded, expired, valid. The first four are failure reasons; a key reaching the fifth check is verified.

#### Scenario: Unreadable
- **WHEN** a string, with whitespace removed, cannot be decoded as a license key
- **THEN** its row SHALL be unreadable

#### Scenario: Verified contents that break the schema
- **WHEN** a key verifies against a trusted signing key but its contents break a rule checked at issue, such as no base purchase, a purchase with an unknown kind, a negative number, a feature name repeated in one purchase, or different text under one name in two purchases
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
- **WHEN** a key verifies, is not a duplicate, is not superseded and has not expired
- **THEN** its row SHALL be valid

### Requirement: A failed key reports its claimed identifiers only
A row whose state is unreadable, wrong product, signing key not recognised or not verified SHALL report its reason, its claimed product ID when readable, and its claimed key identifier when readable, each marked as a claim. It SHALL NOT report the key's issue time, expiry or purchases, nor any purchase's kind, name, purchase date, vendor tag or features. A failed key SHALL take no part in superseding or combining.

#### Scenario: Edited key
- **WHEN** a key edited to claim `expires 2099-12-31` and `max-orders 999999` is evaluated
- **THEN** its row SHALL be not verified, SHALL report the claimed product and identifier, and SHALL NOT report the expiry or the feature

#### Scenario: Failed key with a vendor tag
- **WHEN** a key whose purchase carries vendor tag `SHOP-2026-000123` fails verification
- **THEN** its row SHALL NOT report the vendor tag or any purchase

#### Scenario: Another product's key
- **WHEN** a key for `zenith.commerce-shipping` is evaluated for `acme.commerce`
- **THEN** its row SHALL be wrong product and SHALL report `zenith.commerce-shipping` and its identifier as claims

### Requirement: A verified key reports its claims
A row for a verified key SHALL report, as facts: its product ID, key identifier, issue time, expiry (or that it never expires), its purchases in the order issued, each with its kind, name, purchase date, vendor tag (or that it has none) and features, and the license's features combined across its purchases. Vendor tags and purchase names SHALL take no part in superseding, combining or feature lookup (PDR-0022, PDR-0037).

#### Scenario: Verified key row
- **WHEN** a verified key expiring 2027-03-01 with purchases `base Commerce Standard` (`ecommerce`, `max-orders: 500`) and `add-on Extra 1,000 orders` (`max-orders: 1000`) is evaluated
- **THEN** its row SHALL report the expiry, both purchases with all their fields in that order, and the license's features `ecommerce` and `max-orders` 1500, whatever its state

#### Scenario: List of purchases
- **WHEN** a verified key holding one base and three add-on purchases is evaluated
- **THEN** its row SHALL list four purchases, the base and the add-ons in the order issued

#### Scenario: Vendor tag reported, not used
- **WHEN** two verified keys with different references both carry a purchase with vendor tag `SHOP-2026-000123`
- **THEN** each row SHALL report the tag, and the keys SHALL combine as keys with different references

### Requirement: Later reissue supersedes
Among verified, non-duplicate keys for the product, a key SHALL be superseded when another key with the same license reference was issued strictly later. Expiry and purchases SHALL play no part: an expired key still supersedes, and a later key supersedes whatever purchases it holds. A superseded key SHALL take no part in combining.

#### Scenario: Renewal left beside the old key
- **WHEN** two keys share a reference, issued 2025-03-01 and 2026-03-01, each with `max-orders: 500`
- **THEN** the older SHALL be superseded and the product SHALL have `max-orders` 500

#### Scenario: Expired reissue still supersedes
- **WHEN** a key expiring 2099-03-01 is superseded by a reissue under the same reference that has since expired
- **THEN** the older key SHALL stay superseded, the reissue SHALL be expired, and neither SHALL count

#### Scenario: Add-on purchase added
- **WHEN** a key holding a base purchase is superseded by a later key under the same reference holding the base and an add-on purchase
- **THEN** the older key SHALL be superseded and the product SHALL have the features of both purchases in the later key

#### Scenario: Reissue changes the role
- **WHEN** a key whose base purchase is `Commerce Standard` is superseded by a later key under the same reference whose base purchase is `Commerce Pro`
- **THEN** the older key SHALL be superseded with no role note, and the row for the later key SHALL report `Commerce Pro` as its base purchase

#### Scenario: Reissue removes a purchase
- **WHEN** a key holding a base and an `ai-assist` add-on is superseded by a later key under the same reference holding only the base
- **THEN** the product SHALL NOT grant `ai-assist`

#### Scenario: Failed key does not supersede
- **WHEN** a valid key and a not-verified key claiming the same reference are evaluated
- **THEN** the valid key SHALL stay valid

### Requirement: Combined features
The product's features SHALL combine the features of every purchase in every valid row. A switch SHALL be granted when any counted purchase grants it. Numbers under one name SHALL be summed exactly at the precision written. Text values under one name that are exactly equal (same characters, same case) SHALL count as one value. Different text values, or text alongside a switch or number under one name, SHALL be a conflict: the feature SHALL answer nothing at product level, and the product result and every counted row carrying that name SHALL be flagged with the conflict. A conflict SHALL NOT change any row's state.

#### Scenario: Base plus add-ons
- **WHEN** a valid key with purchases `ecommerce, max-orders: 500`, `max-orders: 1000`, `max-orders: 500` and `ai-assist` is evaluated
- **THEN** the product SHALL have `ecommerce`, `ai-assist` and `max-orders` 2000

#### Scenario: Two licenses
- **WHEN** two valid keys with different references carry `max-orders: 500` each
- **THEN** the product SHALL have `max-orders` 1000

#### Scenario: Exact decimal sum
- **WHEN** counted purchases carry `storage-gb` 10, 2.5 and 2.5
- **THEN** the product SHALL have `storage-gb` 15 exactly

#### Scenario: Equal text
- **WHEN** two valid keys with different references both carry `licensed-domain: example.com`
- **THEN** the product SHALL have `licensed-domain` `example.com`

#### Scenario: Conflicting text
- **WHEN** one valid key carries `licensed-domain: example.com` and another valid key with a different reference carries `licensed-domain: exmaple.com`
- **THEN** `licensed-domain` SHALL answer nothing, both rows SHALL stay valid and carry a conflict flag, and the product's other features SHALL be unaffected

#### Scenario: Not licensed
- **WHEN** the product is not licensed
- **THEN** every feature SHALL answer not granted

## REMOVED Requirements

### Requirement: Base and add-on
**Reason**: Keys no longer carry a role; every license holds exactly one base purchase (PDR-0037, PDR-0038). The *inactive* state no longer exists.
**Migration**: The product is licensed when at least one row is valid (requirement "Licenses counted" reports how many). Code reading the inactive state or the valid base count reads the licenses-counted figure instead.

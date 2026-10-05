## ADDED Requirements

### Requirement: Purchases
A request SHALL hold one or more purchases, kept in the order supplied. Each purchase SHALL have a kind of `base` or `add-on`, a name, a purchase date, an optional vendor tag and zero or more features (PDR-0037). The system SHALL reject a request that has no purchases, a number of `base` purchases other than one, or a purchase with no kind or any other kind value; there is no default kind. Add-on purchases MAY repeat, including with the same name and features.

#### Scenario: Base and repeated add-ons
- **WHEN** a caller issues a key with purchases `base Commerce Pro`, `add-on Extra 1,000 orders` and `add-on Extra 1,000 orders`
- **THEN** the system SHALL produce the key, and a validator SHALL report the three purchases in that order

#### Scenario: No purchases
- **WHEN** a caller issues a key with no purchases
- **THEN** the system SHALL reject the request and produce no key

#### Scenario: No base purchase
- **WHEN** a caller issues a key whose only purchase is an add-on
- **THEN** the system SHALL reject the request and produce no key

#### Scenario: Two base purchases
- **WHEN** a caller issues a key with two base purchases
- **THEN** the system SHALL reject the request and produce no key

#### Scenario: Purchase with no kind
- **WHEN** a caller issues a key with a purchase that has no kind
- **THEN** the system SHALL reject the request and produce no key

### Requirement: Purchase name
Each purchase SHALL have a name of 1 to 64 characters with no leading or trailing whitespace, no line breaks and no other control characters. The system SHALL sign the name exactly as supplied and SHALL reject a name that breaks these rules, without altering or trimming it.

#### Scenario: Name signed unchanged
- **WHEN** a caller issues a purchase named `Extra 1,000 orders`
- **THEN** a validator SHALL report the purchase name `Extra 1,000 orders`

#### Scenario: Invalid name
- **WHEN** a caller issues a purchase with no name, an empty name, the name ` Pro`, a name containing a line break, or a name of 65 characters
- **THEN** the system SHALL reject the request and produce no key

### Requirement: Purchase date
Each purchase SHALL have a purchase date, a date with no time. The system SHALL reject a purchase date after the current UTC date, and SHALL accept the current UTC date and any earlier date. The purchase date SHALL have no effect on validity, superseding or combining.

#### Scenario: Purchase date today
- **WHEN** a caller issues on 2026-10-01 a purchase dated 2026-10-01
- **THEN** the system SHALL produce the key

#### Scenario: Earlier purchase carried forward
- **WHEN** a caller issues on 2026-10-01 a purchase dated 2025-03-01
- **THEN** the system SHALL produce the key, and a validator SHALL report the purchase date 2025-03-01

#### Scenario: Future purchase date
- **WHEN** a caller issues on 2026-10-01 a purchase dated 2026-10-02
- **THEN** the system SHALL reject the request and produce no key

#### Scenario: No purchase date
- **WHEN** a caller issues a purchase with no purchase date
- **THEN** the system SHALL reject the request and produce no key

### Requirement: Features combine across purchases at issue
Features under one name in different purchases of one request SHALL combine as they do between licenses: switches and numbers MAY repeat across purchases, and text values SHALL be exactly equal. The system SHALL reject a request in which two purchases carry different text under one name, or text beside a switch or number under one name.

#### Scenario: Numbers in two purchases
- **WHEN** a caller issues a base with `max-orders: 500` and an add-on with `max-orders: 1000`
- **THEN** the system SHALL produce the key

#### Scenario: Equal text in two purchases
- **WHEN** a caller issues a base and an add-on that both carry `licensed-domain: example.com`
- **THEN** the system SHALL produce the key

#### Scenario: Different text in two purchases
- **WHEN** a caller issues a base with `licensed-domain: example.com` and an add-on with `licensed-domain: exmaple.com`
- **THEN** the system SHALL reject the request, naming the feature, and produce no key

#### Scenario: Text beside a number
- **WHEN** a caller issues a base with `max-orders: 500` and an add-on with `max-orders` as the text `500`
- **THEN** the system SHALL reject the request, naming the feature, and produce no key

### Requirement: Reissue request from a verified license
The system SHALL build a reissue request from a verified license as reported by evaluation: the same product, the same license reference, the same expiry (or none), and every purchase with its kind, name, purchase date, vendor tag and features, in the same order. The caller MAY change any of these before issuing, except the product. The system SHALL NOT build a reissue request from a key that failed verification (PDR-0036).

#### Scenario: Add a purchase to the current key
- **WHEN** a caller builds a reissue request from a verified key holding `base Commerce Standard`, appends `add-on AI Assist` and issues it
- **THEN** the new key SHALL carry the same reference and expiry and both purchases, and SHALL supersede the earlier key when both are evaluated

#### Scenario: Unchanged reissue
- **WHEN** a caller builds a reissue request from a verified key and issues it unchanged
- **THEN** a validator SHALL report the same reference, expiry and purchases as the earlier key, with a new key part and issue time

#### Scenario: Failed key
- **WHEN** a caller evaluates a key that is not verified
- **THEN** the evaluation SHALL offer no license from which to build a reissue request

## MODIFIED Requirements

### Requirement: Generate a license key from its contents
The system SHALL accept a product ID, an optional license reference, an optional expiry date, one or more purchases (PDR-0037) and a private signing key, and SHALL produce a signed key string. The signing key ID SHALL be derived from the private key, not supplied by the caller. It SHALL also return the license reference, the key identifier and the issue time of the key it produced. It SHALL keep no record of the key (PDR-0005).

#### Scenario: Minimal base license
- **WHEN** a caller issues a key with product `acme.seo-toolkit` and one base purchase named `SEO Toolkit` with no features
- **THEN** the system SHALL produce a key with a new license reference, no expiry and that one purchase

#### Scenario: Full contents
- **WHEN** a caller issues a key with product, expiry and purchases with names, purchase dates, vendor tags and features
- **THEN** a validator trusting the signing key SHALL evaluate the key as verified and report exactly those contents

#### Scenario: Nothing retained
- **WHEN** a key has been issued
- **THEN** the system SHALL hold no record of the product, the contents or the key

### Requirement: Optional vendor tag
Each purchase SHALL accept an optional vendor tag: 1 to 64 characters from `A`-`Z`, `a`-`z`, digits and `- _ . # /`. The system SHALL sign a supplied tag into its purchase exactly as supplied, SHALL reject a tag that breaks these rules, and SHALL NOT alter, clean or trim it. It SHALL NOT check tags for uniqueness, within a key or across keys. The system SHALL carry nothing forward on reissue: a purchase carries a tag only when the request supplies it (PDR-0022, PDR-0037).

#### Scenario: Tag signed unchanged
- **WHEN** a caller issues a purchase with vendor tag `SHOP-2026-000123`
- **THEN** a validator trusting the signing key SHALL report vendor tag `SHOP-2026-000123` for that purchase

#### Scenario: Same tag on two keys
- **WHEN** a caller issues two keys, or two purchases in one key, with vendor tag `#1001`
- **THEN** the system SHALL produce the keys

#### Scenario: Invalid tag rejected, not cleaned
- **WHEN** a caller issues a purchase with vendor tag `INV 2026 04`, `jane@acme.com`, an empty tag, or a tag of 65 characters
- **THEN** the system SHALL reject the request and produce no key

#### Scenario: No tag
- **WHEN** a caller issues a purchase without a vendor tag
- **THEN** the purchase SHALL carry no vendor tag

#### Scenario: Reissue without a tag
- **WHEN** a caller reissues under a reference whose earlier key's base purchase carried a vendor tag, and supplies the base purchase without a tag
- **THEN** the new key's base purchase SHALL carry no vendor tag

### Requirement: Feature rules
Each feature SHALL have a name of lowercase `a`-`z`, digits and hyphens starting with a letter, appearing at most once in its purchase, and a value that is a switch, a number or text. The system SHALL reject:
- a name breaking the name rule, or repeated in one purchase;
- an explicit `false`;
- a number that is negative, malformed, has more than 4 decimal places or more than 15 digits;
- text that is empty, longer than 256 characters, has leading or trailing whitespace, or contains a line break or other control character.

The value's type SHALL be the type the caller issued; text SHALL never be read as a number.

#### Scenario: Valid features
- **WHEN** a caller issues a purchase with `ecommerce` (switch), `storage-gb: 2.5` (number) and `licensed-domain: example.com` (text)
- **THEN** the system SHALL produce the key with those features and types

#### Scenario: Invalid feature
- **WHEN** a caller issues a purchase with any of `pro: false`, `max-orders: -200`, `storage-gb: 2.12345`, `Max Orders: 500`, `licensed-domain: " example.com"`, or `max-orders` twice
- **THEN** the system SHALL reject the request and produce no key

#### Scenario: Same name in two purchases
- **WHEN** a caller issues `max-orders: 500` in one purchase and `max-orders: 1000` in another
- **THEN** the system SHALL NOT reject the request for a repeated name

#### Scenario: Numeric-looking text
- **WHEN** a caller issues `max-orders` as the text `500`
- **THEN** the key SHALL carry it as text

### Requirement: Rejection reports every problem
A rejected request SHALL raise an error that identifies every rule the request breaks, not only the first, naming the purchase each problem belongs to by its position, and SHALL produce no key.

#### Scenario: Several problems
- **WHEN** a request has no base purchase and a negative number in its first purchase
- **THEN** the error SHALL identify both, the second naming purchase 1

## REMOVED Requirements

### Requirement: Role is required
**Reason**: Keys no longer carry a role (PDR-0038). The kind of each purchase is required instead, under "Purchases".
**Migration**: Supply a `base` purchase in place of role `base`. An add-on is an `add-on` purchase appended to the license it extends, reissued under that license's reference.

## Purpose

Issues license keys for new sales, add-ons and renewals with the library, from the vendor's
license types, and keeps a record of every key so it can be re-sent and renewed.

Serves the **vendor** and the **site owner**, who must receive a correct key and must be able to
get it re-sent or renewed. Product decisions: PDR-0006, PDR-0008, PDR-0009, PDR-0011, PDR-0017,
PDR-0022, PDR-0024, PDR-0025, PDR-0026, PDR-0027, PDR-0029.

## ADDED Requirements

### Requirement: Issuing uses the library
Every key SHALL be produced by the library's issuing API with the product's current signing key.
Before signing, the system SHALL check the request with the library and show every problem the
library reports; it SHALL NOT apply license rules of its own. The system SHALL calculate the
expiry from the license type and pass it to the library as a date, or as perpetual (PDR-0029).
Nothing SHALL be signed or recorded until the vendor confirms a summary of the key's contents.

#### Scenario: Library rejects the request
- **WHEN** the vendor enters an expiry before today
- **THEN** the system SHALL show the library's reason and let the vendor correct it, and no key SHALL be signed

#### Scenario: Vendor cancels at the summary
- **WHEN** the vendor declines the summary
- **THEN** no key SHALL be signed or recorded

### Requirement: Issue for a new sale
The vendor SHALL be able to issue a base license by choosing a product and one of its active
base license types. The expiry SHALL be pre-filled as today plus the type's term (PDR-0025), or
none for a perpetual type, and the features from the type's defaults; both SHALL be editable for
this sale. The vendor MAY enter an order reference. The key SHALL get a new license reference.

#### Scenario: New sale from a type
- **WHEN** on 2026-10-04 the vendor issues `Commerce Pro` (12 months, `ecommerce`, `max-orders: 500`) with order reference `SHOP-1001`
- **THEN** the key SHALL be for `acme.commerce`, role base, expiring 2027-10-03, with those features and vendor tag `SHOP-1001`, and the system SHALL show the key string and its license reference

#### Scenario: Edited at issue
- **WHEN** the vendor changes `max-orders` to `750` for one sale
- **THEN** the key SHALL carry `max-orders: 750` and the license type SHALL be unchanged

#### Scenario: No base types
- **WHEN** a product has no active base license type
- **THEN** the system SHALL say so and offer to define one

### Requirement: Issue an add-on
The vendor SHALL be able to issue an add-on license by choosing a product and one of its active
add-on license types, with the same pre-filling and editing as a new sale. The vendor MAY link the
license to a base license of the same product found in the records, whatever its status
(PDR-0026). The key SHALL get a new license reference and carry no link.

#### Scenario: Linked add-on
- **WHEN** the vendor issues `Extra 1,000 orders` linked to base license `LIC-8F3AK-M7RXB`
- **THEN** the add-on license SHALL hold the link, and showing `LIC-8F3AK-M7RXB` SHALL list the add-on

#### Scenario: Unlinked add-on
- **WHEN** the vendor issues an add-on for a base bought elsewhere
- **THEN** the key SHALL be issued with no link recorded

#### Scenario: Link to another product refused
- **WHEN** the vendor tries to link an add-on for `acme.commerce` to a base license of `acme.shipping`
- **THEN** the system SHALL not offer it

### Requirement: Renew a license
The vendor SHALL be able to renew a license found in the records by reissuing it under its
license reference. The expiry SHALL be calculated by PDR-0025: from the day after the current
expiry when the current key has not expired; when it has, the vendor SHALL choose between the
day after the old expiry and today, with no default. A license whose current key has no expiry
is perpetual and SHALL NOT be renewable, whatever its license type says. The new term SHALL come
from the license type chosen at renewal; no term is stored on the license. Only active license
types of the same product and role SHALL be offered: the license's own type if it is still
active, or another (PDR-0024). When none is active, the system SHALL say so and offer to define
one, and SHALL issue nothing. Features SHALL be pre-filled from the current key when the license
keeps its type, and from the chosen type's defaults when it switches; the summary SHALL show
where they differ from the chosen type's defaults. The vendor MAY edit the expiry and features. When
the renewal would produce a key with no expiry, the summary SHALL say so and that the license
cannot be renewed again. The order reference SHALL be entered afresh, not carried forward.

#### Scenario: Early renewal
- **WHEN** on 2027-03-10 the vendor renews a 12-month license expiring 2027-03-31
- **THEN** the new key SHALL carry the same license reference and expire 2028-03-31

#### Scenario: Lapsed, continue from old expiry
- **WHEN** on 2027-05-20 the vendor renews a 12-month license that expired 2027-03-31 and chooses the old expiry
- **THEN** the new key SHALL expire 2028-03-31

#### Scenario: Lapsed, from today
- **WHEN** on 2027-05-20 the vendor renews the same license and chooses today
- **THEN** the new key SHALL expire 2028-05-19

#### Scenario: Perpetual
- **WHEN** the vendor tries to renew a license whose current key has no expiry
- **THEN** the system SHALL say there is nothing to renew and issue nothing

#### Scenario: Perpetual key from a term type
- **WHEN** a `Commerce Pro` (12 months) license was issued with the expiry removed, and the vendor tries to renew it
- **THEN** the system SHALL say there is nothing to renew and issue nothing

#### Scenario: Dated key from a perpetual type
- **WHEN** on 2026-12-20 the vendor renews an `Extra 1,000 orders` (perpetual) license whose current key expires 2027-01-03, keeping that type
- **THEN** the summary SHALL show no expiry and say the license cannot be renewed again, and on confirmation the new key SHALL have no expiry

#### Scenario: Dated key from a perpetual type, switched to a term type
- **WHEN** on 2026-12-20 the vendor renews the same license choosing an add-on type with a 12-month term
- **THEN** the new key SHALL expire 2028-01-03

#### Scenario: Upgrade on renewal
- **WHEN** the vendor renews a `Commerce Pro` license choosing base type `Commerce Enterprise`
- **THEN** the new key SHALL use the `Commerce Enterprise` term and default features, keep the license reference, and the record SHALL show the new type

#### Scenario: Role cannot change
- **WHEN** the vendor renews a base license
- **THEN** only active base types of the same product SHALL be offered

#### Scenario: Negotiated features kept
- **WHEN** the vendor renews a `Commerce Pro` license whose current key has `max-orders: 750`, keeping `Commerce Pro`, whose default is now `max-orders: 600`
- **THEN** the features SHALL be pre-filled with `max-orders: 750`, and the summary SHALL show that the type's default is `600`

#### Scenario: Retired type must switch
- **WHEN** the vendor renews a license whose type `Commerce Pro` has been retired
- **THEN** `Commerce Pro` SHALL NOT be offered, and the features SHALL be pre-filled from the active type chosen, with the summary showing where the current key differed

#### Scenario: No active type
- **WHEN** the vendor renews a base license and the product has no active base type
- **THEN** the system SHALL say so, offer to define one, and issue nothing

### Requirement: Order reference
The order reference SHALL be optional, SHALL be signed as the vendor tag and SHALL follow the
library's vendor tag rule (PDR-0027). The prompt SHALL say it is for an order or invoice number
and must never hold personal data.

#### Scenario: Invalid order reference
- **WHEN** the vendor enters `Order 1001`
- **THEN** the system SHALL show the library's reason and ask again

### Requirement: Record of every issued key
For every issued key the system SHALL record: product, license type, role, license reference,
key identifier, issue time, expiry, features, order reference, signing key ID, the key string,
and the kind of issue (new sale, add-on or renewal). A license reference's records SHALL be kept
together as one license; for an add-on, the license holds any link to a base license
(PDR-0026). No name, email address, company or other personal data SHALL be recorded
(PDR-0006).

#### Scenario: Renewal recorded under the license
- **WHEN** a license is renewed twice
- **THEN** showing the license SHALL list three keys in issue order, the latest marked current

### Requirement: Change an add-on's link
The vendor SHALL be able to add, change or remove the link from an add-on license to a base
license at any time after issue. Only base licenses of the same product SHALL be offered,
whatever their status. The link belongs to the license, so renewing either license SHALL keep
it. A link change SHALL NOT issue a key (PDR-0026).

#### Scenario: Link added later
- **WHEN** the vendor links an unlinked add-on license to base license `LIC-8F3AK-M7RXB`
- **THEN** showing `LIC-8F3AK-M7RXB` SHALL list the add-on, and no key SHALL be issued

#### Scenario: Renewal keeps the link
- **WHEN** a linked add-on license is renewed
- **THEN** the add-on license SHALL still be linked to the same base license

#### Scenario: Expired base
- **WHEN** the vendor links an add-on to a base license whose current key has expired
- **THEN** the system SHALL record the link

#### Scenario: Link removed
- **WHEN** the vendor removes an add-on's link
- **THEN** neither license SHALL show the other

### Requirement: No two keys in the same second
The system SHALL NOT record two keys under one license reference with the same issue time
(PDR-0009). When a renewal would do so, the system SHALL discard that key unseen, wait until the
next second and issue again.

#### Scenario: Renewal in the same second
- **WHEN** a renewal is signed in the same second as the license's latest key
- **THEN** the recorded key SHALL have a later issue time, and only that key SHALL be shown

### Requirement: New references are unique per product
When the library generates a license reference already recorded for the same product, the
system SHALL discard that key unseen and issue again (PDR-0017).

#### Scenario: Reference collision
- **WHEN** a new sale's generated reference matches an existing license of the product
- **THEN** the system SHALL issue again and record only the key with a new reference

### Requirement: Key shown once issued
After recording a key, the system SHALL show the key string on one line with the license
reference, key identifier, expiry and order reference, ready to copy and send to the site owner.
The key string SHALL be retrievable later from the records.

#### Scenario: Key shown
- **WHEN** a key has been issued
- **THEN** the system SHALL show the full key string and its license reference

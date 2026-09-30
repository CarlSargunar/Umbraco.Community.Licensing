# PDR-0010: Product features are switches and whole numbers

- **Status:** Decided, 2026-09-26; text type dropped 2026-09-30
- **Source:** `openspec/changes/license-key-management/design.md` third pass (R7, Q9, Q10); eighth pass (text dropped)
- **Serves:** vendor (gate capabilities and sell limits), site owner (later additions reach existing licenses)

## Decision

A key carries 0..N features, scoped to its product. Each is a name (PDR-0013) and a typed value:

| Type | Example |
|---|---|
| Switch | `pro` (a plain name means granted) |
| Whole number | `max-orders: 500` |

- Present means granted. An explicit `false` is invalid.
- Malformed values are rejected when the key is issued.
- A feature missing from the key is not granted. A name the package does not recognise is ignored.
- "Tier" and "feature" are one concept. Whether `pro` is a bundle or a single capability is a
  vendor convention.
- Features do not affect validity. "Does this license grant X?" is asked after validation and
  always answers no for an invalid license.
- The library reports values; the package enforces them (counting orders, per period or per site).

## Why

- A package maps its own capabilities to feature names, so a capability added in a later
  release can be gated on a name existing site owners already hold. Site owners get later
  additions without new keys.
- One way to say "not granted" (absence) avoids ambiguity.
- A typo like `"5OO"` fails at the vendor, not months later on a site owner's site.

## Rejected

| Option | Why rejected |
|---|---|
| Text values | No natural way to combine across licenses (PDR-0011): concatenate, or latest wins so a cheap add-on overrides the base? Obvious uses have better forms: an edition is a switch, a support level is a switch such as `priority-support`, links and names belong to registration (PDR-0004) |
| Separate tier model | Forces a commercial question ("does Pro include everything added later?") the library cannot answer; switches cover tiers by convention |
| Features overlapping the Umbraco version range | They are orthogonal: features gate entitlement within a product; the range sets which Umbraco versions a license covers (PDR-0012) |

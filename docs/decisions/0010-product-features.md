# PDR-0010: Product features are switches and numbers

- **Status:** Decided, 2026-09-26; text type dropped 2026-09-30 (eighth pass); numbers widened
  to decimals and duplicate names rejected 2026-09-30 (tenth pass)
- **Source:** `openspec/changes/license-key-management/design.md` third pass (R7, Q9, Q10); eighth pass; tenth pass
- **Serves:** vendor (gate capabilities and sell limits), site owner (later additions reach existing licenses; adding a key never reduces what they hold)

## Decision

A key carries 0..N features, scoped to its product. Each is a name (PDR-0013) and a typed value:

| Type | Example |
|---|---|
| Switch | `pro` (a plain name means granted) |
| Number | `max-orders: 500`, `storage-gb: 2.5` |

- Present means granted. An explicit `false` is invalid.
- Numbers are **zero or positive**, with at most **4 decimal places** and at most **15 digits**
  in total. They are **additive quantities** only: counts, limits or amounts that make sense
  summed across licenses (PDR-0011). Rates, ratios and factors belong in package settings, not
  in keys.
- A feature name appears at most once in a key, whatever its type.
- Malformed values, and anything breaking the rules above, are rejected when the key is issued.
- A feature missing from the key is not granted. A name the package does not recognise is ignored.
- "Tier" and "feature" are one concept. Whether `pro` is a bundle or a single capability is a
  vendor convention.
- Features do not affect validity. "Does this license grant X?" is asked after validation and
  always answers no for an invalid license.
- The library reports values; the package enforces them (counting orders, per period or per site).
  Sums are exact at the precision written: 0.1 + 0.2 is 0.3.

## Why

- A package maps its own capabilities to feature names, so a capability added in a later
  release can be gated on a name existing site owners already hold. Site owners get later
  additions without new keys.
- One way to say "not granted" (absence) avoids ambiguity.
- A typo like `"5OO"` fails at the vendor, not months later on a site owner's site.
- Non-negative: with summing, a negative value is the only way that holding more licenses gives
  a site owner less, and it can drive a total below zero. Corrections and downgrades are a
  reissue under the same reference, which supersedes (PDR-0009).
- Additive only: summing is right for `storage-gb` (2.5 + 10 = 12.5) and wrong for a rate
  (`discount-rate` 0.15 + 0.15 = 0.30). Decimals invite rates, so the limit is stated.
- A precision limit gives "exact" a fixed meaning, so every site sums the same way; 4 places
  covers money-like amounts and fractional units. 15 digits is far above anything sold.
- Duplicate names in one key are almost always a mistake (copy-paste, two form rows).

## Rejected

| Option | Why rejected |
|---|---|
| Text values | No natural way to combine across licenses (PDR-0011): concatenate, or latest wins so a cheap add-on overrides the base? Obvious uses have better forms: an edition is a switch, a support level is a switch such as `priority-support`, links and names belong to registration (PDR-0004) |
| Whole numbers only | Excludes fractional quantities such as `storage-gb: 2.5` |
| Negative numbers | See Why; reissue covers corrections |
| Decimals with no precision limit | "Exact" has no fixed meaning; values like `2.50000000001` reach the inventory |
| 2 decimal places | Rejects values such as `0.125` |
| Duplicate names summed within the key | Consistent with combining, but hides mistakes; a vendor wanting 1500 writes 1500 |
| Duplicate names, last wins | Silent and order-dependent |
| Separate tier model | Forces a commercial question ("does Pro include everything added later?") the library cannot answer; switches cover tiers by convention |
| Features overlapping the Umbraco version range | They are orthogonal: features gate entitlement within a product; the range sets which Umbraco versions a license covers (PDR-0012) |

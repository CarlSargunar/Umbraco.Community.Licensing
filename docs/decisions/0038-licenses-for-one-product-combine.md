# PDR-0038: Licenses for one product combine; there are no roles between keys

- **Status:** Decided, 2026-10-05. Supersedes PDR-0011
- **Source:** `openspec/changes/one-key-per-product/design.md` Q4
- **Serves:** site owner (never holds less than they paid for), implementor (states say which
  key to fix); **Cost to:** none

## Decision

Every license holds a base purchase (PDR-0037), so every license licenses its product on its
own. Keys no longer carry a role.

**Per key, then per product.**

```
  per key                                    per product
  duplicate (exact copy)        -> ignored
  superseded (same ref, older)  -> ignored   licensed = at least one VALID key
  invalid (unreadable,                       combined = features of VALID keys
    wrong product, signing key                  switches: any grants
    not recognised, not verified,               numbers:  summed
    expired)                    -> drops out    text:     equal counts once;
  otherwise                     -> VALID                  different conflicts
```

The order of these checks and what an invalid key reports are in PDR-0019. Text conflicts
between keys keep PDR-0018's rule: the feature answers nothing, the keys stay valid.

**Two licenses for one product.** Keys with different references combine like any others. The
result reports how many licenses count: the number of different references among valid keys.
Keys tied for latest under one reference (PDR-0009) are one license. "2 licenses" prompts the
site owner to ask the vendor for a refund or a merge into one key.

## Why

- **No roles needed.** PDR-0011's role existed so an add-on key could not license a product on
  its own. A key now always holds its base, so the check moves to issue (PDR-0037).
- **Combine, do not choose.** A site owner who bought twice, or bought an add-on through a
  channel that could not reissue the license (PDR-0036), gets what they paid for. Choosing one
  license by a rule silently discards a paid purchase.
- **One combining rule.** Purchases within a key and licenses between keys combine the same
  way, so a feature's value does not depend on how purchases were grouped into keys.

## Rejected

| Option | Why rejected |
|---|---|
| Only one license counts, chosen by a rule | Silently drops a paid purchase; needs a selection rule |
| Two licenses for one product are a conflict | Stops a paid product until a key is removed |
| Keep a role on keys for add-ons sold separately | Rejected with per-purchase keys (PDR-0036) |

## Consequences

- The *inactive* state is gone: no key depends on another.
- The *role changed* note on a superseded key is gone; a reissue that removes or replaces the
  base purchase is visible in the new key's purchase list.
- PDR-0011's "add-on license vs add-on product" consequence stands: a separately installed
  package is a product of its own and never combines.

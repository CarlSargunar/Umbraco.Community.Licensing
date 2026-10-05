# PDR-0011: Base and add-on licenses, and how licenses combine

- **Status:** Superseded by PDR-0038, 2026-10-05: keys carry no role; every license holds its base purchase (PDR-0037), so the *inactive* state is gone. Combining between licenses is restated in PDR-0038. Was: Decided, 2026-09-30. Amended, 2026-10-01: *base out of Umbraco range* is no
  longer a reason a license drops out (PDR-0012 deferred, [`docs/deferred-scope.md`](../deferred-scope.md)
  D1); "the inventory lists" reads as "the evaluation result reports" (D5). Amended,
  2026-10-02: the reasons a license is invalid are those in PDR-0019; "tampered" and
  "malformed" are replaced, and "signing key not recognised" is added
- **Source:** `openspec/changes/license-key-management/design.md` Q16, Q13
- **Serves:** site owner (gets what they paid for; one place to see why something stopped), vendor (one combining rule; mistakes fail at issue), implementor (states say which key to fix)

## Decision

**Role.** Every key carries a signed role, `base` or `add-on`, required at issue. An add-on
counts only while at least one valid base license for the same product is present. Capacity
packs are add-ons; there is no subtype.

**Per license, then per product.**

```
  per license                                per product
  duplicate (exact copy)        -> ignored
  superseded (same ref, older)  -> ignored   licensed = at least one VALID base
  invalid (unreadable,                       combined = features of VALID bases
    wrong product, signing key                          + ACTIVE add-ons
    not recognised, not verified,              switches: any grants
    expired)                    -> drops out   numbers:  summed
  VALID add-on, no VALID base   -> INACTIVE
  otherwise                     -> counts
```

The order in which these are decided for one key, and what an invalid key reports, are in
PDR-0019.

- Two valid base licenses with different references combine like any others; the inventory
  notes "2 base licenses".
- An add-on outliving its base is **inactive**, distinct from expired, and reactivates when the
  base is renewed, with no reissue.
- The inventory lists every license with its role and state, plus the combined result.

The role is limited so it does not become a kind of license (PDR-0014): two values only; used
only for combining; shown in the inventory; packages never branch on it (anything needing that
is a feature).

## Why

- An add-on valid without its base makes no commercial sense.
- Required at issue: an unmarked add-on defaulting to base silently gives the product away; an
  unmarked base defaulting to add-on breaks a paid license on the site owner's site. Required
  means the mistake fails at the vendor.
- Combining two bases gives a site owner who bought twice what they paid for, with a visible
  note to seek a refund. An upgrade should be a reissue under the existing reference, which
  supersedes.

## Rejected

| Option | Why rejected |
|---|---|
| Add-on names the base license reference it extends | Keys are bearer tokens with no site binding, so it prevents no sharing. Adds failures (mistyped reference; base re-bought under a new reference orphans the add-on) and makes resellers collect the reference |
| No marker; package code declares "`ai-assist` needs `ecommerce`" | The inventory cannot show "inactive, base expired", and every vendor re-implements a commercial rule |
| Any valid license, base or add-on, licenses the product | An add-on or capacity pack alone would act as a full license |
| Infer role from contents (no features = add-on) | A base may carry no features; a capacity pack carries one |
| Default role when unmarked | See Why |
| Only one base counts, chosen by a rule | Silently discards capacity the site owner paid for; needs a selection rule |

## Consequences

- Add-on license vs add-on product: an add-on license extends a product and carries its product
  ID. A separately installed package, including a third party's, is a product of its own and
  never combines. Whether the library should model dependencies between products is open
  (design.md Q17).

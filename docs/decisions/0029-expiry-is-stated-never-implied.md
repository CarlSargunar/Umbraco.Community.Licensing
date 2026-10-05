# PDR-0029: A key's expiry is stated when issuing, never implied

- **Status:** Decided, 2026-10-04
- **Source:** `openspec/changes/issuing-add-on/design.md` Q10
- **Serves:** vendor (a forgotten date cannot become a lifetime key), site owner (receives the
  term that was sold); **Cost to:** vendor (every request names an expiry or perpetual)

## Decision

Every request to issue a key states its expiry as exactly one of:

| Request states | Key gets |
|---|---|
| An expiry date | That date. Rejected if before the current UTC date (PDR-0016) |
| Perpetual | No expiry: the key never expires |
| Neither, or both | Rejected |

The library accepts only a date. Terms, start dates and period arithmetic are the vendor's
tooling: the issuing add-on calculates a date from a license type's term (PDR-0024, PDR-0025)
and passes the result.

The key's contents are unchanged: a perpetual key carries no expiry, as before.

## Why

- **Stated, not implied.** When an omitted expiry meant perpetual, a caller that forgot the
  date issued a key that never expires. A key cannot be withdrawn, so the vendor lost every
  renewal from that sale. Requiring the choice turns a silent mistake into a rejection.
- **Date only in the library.** How a vendor arrives at a date (months, years, a negotiated
  end, an anniversary) varies by vendor. The library checks the result; one input keeps it
  simple to call and to explain.

## Rejected

| Option | Why rejected |
|---|---|
| Optional expiry; omitted means perpetual | The behaviour replaced. A forgotten date becomes a lifetime key |
| Library also accepts a term in months | Moves one vendor convention into the library. A vendor can calculate a date |
| Term and date both accepted, cross-checked | Two inputs for one value, plus a rule for when they disagree |
| Term or date, mutually exclusive | Simpler than the cross-check, but still a second input for what a date already says |

## Consequences

- A caller written against the earlier optional expiry is rejected until it states one. The
  library had not been released when this was decided.
- The add-on's renewal and term arithmetic (PDR-0025) is an issuing convention, not a license
  rule (PDR-0023 amendment).

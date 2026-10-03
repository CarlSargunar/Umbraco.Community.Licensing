# PDR-0022: Optional vendor tag: the vendor's own label, signed into the key

- **Status:** Decided, 2026-10-03
- **Source:** `openspec/changes/license-key-management/design.md` Q21
- **Serves:** vendor (a key leads straight to its own order record, without the add-on or a
  stored mapping), site owner (can match a key to the order number on their receipt);
  **Cost to:** none, if the vendor follows the guidance (see Consequences)

## Decision

A vendor may pass one optional **vendor tag** when issuing a key: a string of its choice, such
as its shop's order ID or a payment-provider ID. The core signs it into the key unchanged.

| Aspect | Rule |
|---|---|
| Presence | Optional; at most one per key. Omitted means the key has none |
| Characters | `A-Z a-z 0-9` and `- _ . # /`. No spaces |
| Length | 1 to 64 characters |
| Case | Kept as supplied |
| Invalid value | Rejected when passed in, with an error naming the rule, like every other input. Never cleaned or altered |
| Unique | Not required, not checked |
| On reissue | Not carried forward. The vendor passes it again, or a new one (e.g. the renewal order) |
| Effect | None. A label only: never used for superseding, combining, gating or matching |
| Reported | In the evaluation result for **verified** keys only, beside the key identifier |

```
  INPUT                        RESULT     WHY
  #1001                        accepted
  SHOP-2026-000123             accepted
  pi_3NkX9a2eZvKYlo2C1         accepted   payment-provider ID
  INV/2026/04                  accepted
  INV 2026 04                  rejected   space
  jane@acme.com                rejected   @ not allowed
  (empty)                      rejected   omit it instead
  65 or more characters        rejected   length
```

## Why

- **The vendor's own lookup.** The license reference (PDR-0009) and key identifier (PDR-0020)
  already find a key in the vendor's records, provided the vendor recorded them at issue. A
  vendor tag lets a vendor go from a key to its shop's order using the shop's own number.
- **No more identifying than the reference.** An order ID is linkable to a customer only
  through the vendor's records, as the license reference already is. The risk is what a free
  string *could* hold, the same risk PDR-0006 accepted for the add-on's order reference field.
- **Not unique, because it cannot be and need not be.** The core keeps no records (PDR-0005),
  so it has nothing to check against. Nothing depends on the tag, so a duplicate harms nothing.
  Duplicates are also normal: one order can buy several licenses (PDR-0008), and each key then
  carries the same order number.
- **Reject, not clean.** Issuing rejects every other invalid input (PDR-0010, PDR-0017,
  PDR-0018). Cleaning would make the signed value differ from the vendor's own record (`#1001`
  becomes `1001`, which the shop cannot find) and could map two orders to one tag (`A/1` and
  `A 1` both becoming `A1`) without anyone noticing.
- **The character set.** Covers common order and payment IDs. Leaving out spaces and `@` rejects
  names and email addresses in their usual form. That does not police personal data
  (`JaneSmith` passes); it catches the most common mistake.
- **64 characters.** Every character lengthens a key that is pasted and emailed. Order IDs are
  far shorter.
- **Reported for verified keys only.** Otherwise only someone holding the key string, a bearer
  token, could read it. With it in the result, the site owner quotes "order SHOP-2026-000123"
  and nobody handles a key. A key that fails verification could carry anything, so its tag is
  not shown; its key identifier already lets the vendor trace it through the reference.
- **Name.** "Order reference" is the add-on's field (PDR-0006) and is too narrow. "Vendor
  reference" is confused with the license reference. "Vendor note" suggests prose, which the
  rules do not allow.

## Rejected

| Option | Why rejected |
|---|---|
| No vendor string in the key | Simplest, but a vendor without the add-on or a stored mapping cannot get from a key to its order |
| The order ID as the license reference | A renewal is a new order under the same license, and one order can buy several licenses. Also not unique per product (PDR-0017) |
| Uniqueness required | Cannot be enforced (PDR-0005) and contradicts one order buying several licenses |
| Clean the value to legal characters | See Why: the signed value would differ from the vendor's record, and two values could become one |
| Clean and return the value signed | As above; the vendor must store the returned value instead of its own, and collisions still go unnoticed |
| PDR-0018 text rules (spaces allowed, 256 characters) | Names and emails pass in their usual form; longer keys for no gain |
| Reported for every readable key, marked as a claim | A forged or damaged key could show an order number the vendor never issued |
| Never reported; readable only by the vendor's product | Support must ask for the key itself, a bearer token, to read it |
| Named order reference, vendor reference or vendor note | See Why |

## Consequences

- The key's contents gain an optional field: ADR-0001 must be revised in the propose phase.
- PDR-0006 is amended: its rule that customer personal details never go into a key covers the
  vendor tag. The tag cannot be policed; the issuing API's documentation must say it is for an
  order or reference number and must never hold a name, email, company or other personal data.
  The responsibility is the vendor's. Unlike the add-on's field, a key cannot be withdrawn once
  issued, so a mistake here cannot be corrected by deleting a record.
- PDR-0017 is amended with the vendor tag's format.
- The issuing add-on (PDR-0006) may fill the tag from its order reference field. That belongs
  to the add-on's own change.
- A verified key whose tag breaks these rules can only come from a faulty vendor tool and is
  unreadable (PDR-0021).
- Worked examples: `docs/license-examples.md` examples 19 and 20.

# PDR-0024: License types are templates, editable at issue

- **Status:** Decided, 2026-10-04
- **Source:** `openspec/changes/issuing-add-on/design.md` Q4
- **Serves:** vendor (one definition per thing it sells; exceptions without a new type)

## Decision

A product has one or more **license types**: what the vendor sells for it. A license type is a
template for issuing, held only in the add-on. It is never part of a key.

| Field | Rule |
|---|---|
| Name | Required; unique within the product, ignoring case. E.g. `Commerce Pro`, `Extra 1,000 orders` |
| Role | `base` or `add-on` (PDR-0011). Fixed once a license has been issued from the type |
| Term | A whole number of months from 1 to 120, or perpetual (no expiry) |
| Default features | Zero or more features, checked by the library's rules (PDR-0010, PDR-0013, PDR-0018) |
| Retired | A type can be retired: kept for its existing licenses and their renewals, not offered for new sales |

At issue, the type pre-fills the expiry and features. The vendor may change either for that
one sale; the type is unchanged. A license records the type it was issued from.

Editing a type affects only keys issued afterwards. A type with issued licenses cannot be
deleted, only retired.

## Why

- A vendor sells a few things repeatedly. Typing features on every sale invites mistakes, and a
  key cannot be withdrawn.
- Exceptions happen (a negotiated limit, a trial extended). Editable at issue avoids a new type
  for each.
- The role is fixed once used, so every license from one type has the role the type shows.

## Rejected

| Option | Why rejected |
|---|---|
| Fixed templates | Every exception becomes a new type; the catalogue fills with one-off types |
| No templates | Role, term and features typed every time |
| Term in days | Commercial terms are months or years; days make "1 year" ambiguous across leap years |

## Consequences

- The term's expiry rule is PDR-0025.
- The license type is the add-on's record, not a fact about the key. A key's contents are what
  was signed, whatever its type now says.

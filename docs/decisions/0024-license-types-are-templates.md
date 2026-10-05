# PDR-0024: License types are templates, editable at issue

- **Status:** Decided, 2026-10-04. Amended, 2026-10-05: a retired type is not offered for
  renewals either; a license on a retired type renews by switching to an active type; a renewal
  that keeps its type takes features from the current key (PDR-0025)
- **Source:** `openspec/changes/issuing-add-on/design.md` Q4; amendment: Q12, Q13
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
| Retired | A type can be retired: kept as the record of its existing licenses, not offered for new sales, add-ons or renewals. A license on a retired type renews by switching to an active type (PDR-0025) |

At issue, the type pre-fills the expiry and features. The vendor may change either for that
one sale; the type is unchanged. A license records the type it was issued from.

Editing a type affects only keys issued afterwards. Its term applies to every renewal on it; its
default features apply to new sales, add-ons and renewals that switch to it. A renewal that
keeps its type takes features from the license's current key, and the summary shows where they
differ from the type's defaults (PDR-0025). A type with issued licenses cannot be deleted, only
retired.

## Why

- A vendor sells a few things repeatedly. Typing features on every sale invites mistakes, and a
  key cannot be withdrawn.
- Exceptions happen (a negotiated limit, a trial extended). Editable at issue avoids a new type
  for each.
- The role is fixed once used, so every license from one type has the role the type shows.
- **Retired means no longer sold, renewals included.** A vendor retires a type to stop selling
  it. Renewing on it would keep selling it indefinitely. The renewal switches to an active type,
  whose defaults pre-fill the features; the summary shows what changed.

## Rejected

| Option | Why rejected |
|---|---|
| Fixed templates | Every exception becomes a new type; the catalogue fills with one-off types |
| No templates | Role, term and features typed every time |
| Term in days | Commercial terms are months or years; days make "1 year" ambiguous across leap years |
| Retired types stay renewable on their own type | A retired offer would be sold indefinitely through renewals |

## Consequences

- The term's expiry rule is PDR-0025.
- The license type is the add-on's record, not a fact about the key. A key's contents are what
  was signed, whatever its type now says.

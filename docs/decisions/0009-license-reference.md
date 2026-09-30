# PDR-0009: License reference: same reference supersedes, different ones combine

- **Status:** Decided, 2026-09-28
- **Source:** `openspec/changes/license-key-management/design.md` seventh pass (Q8's license-reference claim)
- **Serves:** site owner, implementor, vendor

## Decision

Every key carries a short, random license reference, signed into the key, generated when a
license is first issued and kept when it is reissued. It names one license: one entitlement to
one product. Not a key, an order or a customer.

- Keys with the **same** reference: the latest issued supersedes the others.
- Keys with **different** references: separate purchases; they combine.

The reference is unique per product (format: PDR-0017), reveals nothing, is readable before
the signature is trusted, and is **not secret and not proof of ownership**.

## Why

- Resolves renewal vs capacity pack without an "add" marker: a renewal left beside the old key
  (`max 500` twice, same reference) stays 500; a capacity pack (different reference) adds.
- An old key left in place at renewal is superseded, not a conflict.
- Site owners and implementors can quote "LIC-8F3A" for support or renewal without handling the
  key; vendors find one license's history.
- Safe in the backoffice, emails, order forms and the renewal link, because it grants nothing.

## Rejected

| Option | Why rejected |
|---|---|
| Using the reference as proof of ownership for reissue | Anyone who sees it (screenshot, support email, agency handover) could buy a cheap add-on quoting it and, under an entitlement key, receive the whole entitlement |

## Consequences

"Latest issued" requires the key to record when it was issued: a UTC date and time set by the
core (PDR-0016).

# PDR-0034: The add-on reissues a license to correct it or re-sign it, keeping its terms

- **Status:** Decided, 2026-10-05
- **Source:** `openspec/changes/issuing-add-on/design.md` Q22. Amends PDR-0026
- **Serves:** site owner (a key issued wrongly is fixed with one new key, without losing the term
  paid for), implementor (one key to add; the old key is superseded, not a conflict), vendor
  (fixes its own mistakes and moves licenses off a retired signing key inside the tool);
  **Cost to:** none

## Decision

The add-on offers **Reissue** for a license found in the records. It issues a new key under the
same license reference, signed by the product's current signing key. The new key supersedes the
old one (PDR-0009). It is for when the current key is wrong or must be re-signed, not for selling
more time.

| Aspect | Rule |
|---|---|
| Offered for | A license whose current key has not expired, perpetual included. An expired license is renewed instead: the library rejects a past expiry (PDR-0016) |
| Expiry | Pre-filled from the current key, a date or perpetual. No period is calculated. Editable |
| Features | Pre-filled from the current key when the type is kept, from the chosen type's defaults when it switches. Editable |
| License type | Kept by default, even if retired. May switch to any active type of the same product, of either role |
| Role | Follows the type, so a wrong role can be corrected (`license-examples.md` example 5) |
| Order reference | Carried forward from the current key: the same purchase. Editable or removable |
| Recorded as | Kind of issue *reissue*. Logged |
| Product | Never changes. A key for the wrong product is fixed by a new sale for the right one |

**Summary before confirming.** It shows every difference from the current key, so all mistakes
are fixed in one key. When nothing differs, it says the reissue re-signs the same claims under
the current signing key. When the reissue shortens the expiry or removes or lowers a feature, it
says: *takes effect only if the site installs this key; the current key keeps granting the old
claims.* When the expiry and the chosen type's term disagree (a dated key from a perpetual type,
or the reverse), it says so. It never blocks.

**Role change and add-on links** (amends PDR-0026). A link joins an add-on to a base, so a role
change removes links that no longer join an add-on to a base:

| Reissue | Links removed | Summary names |
|---|---|---|
| Add-on becomes base | Its own link to a base | The base it was linked to |
| Base becomes add-on | Every link from add-ons to it | Each add-on whose link is removed |

Removed links are logged, as any link change is (PDR-0026).

## Why

- **The likely cause is the vendor.** A wrong type, role, feature or expiry at issue, or wrong
  type defaults, or a signing key the vendor retired or lost control of. The site owner paid and
  did nothing wrong, so the fix must cost them as little as possible.
- **Same reference is the least effort for the site owner.** The new key supersedes the old, so
  the implementor adds one key and removes nothing; until then the site keeps running on the old
  key. Under a new reference the two keys would combine (`max-orders` 50 plus 500, or two bases)
  and the old key would have to be found and removed in every environment.
- **Terms kept by default.** A renewal moves the expiry; a correction must not. Pre-filling from
  the current key means the vendor changes only what was wrong.
- **One key per correction.** The summary lists every difference, so a second mistake is caught
  before the key goes out, not after.
- **Same purchase, same order reference.** The corrected key carries the order number on the site
  owner's invoice.
- **Reductions warned, not blocked.** A key that grants less only takes effect if the site
  installs it; a site owner has no reason to. Correcting an over-grant usually costs the site
  owner effort for a loss, so the vendor should know before sending it.
- **Re-signing.** PDR-0033: perpetual licenses on a retired signing key, and every license after
  an emergency switch, need a key signed by the new key with the same claims. A dated license can
  usually wait for its next renewal, which re-signs it at no extra cost to the site owner.
- **Role.** A wrong role is the vendor mistake that leaves the site owner with a key that does not
  work at all. The library already lets a reissue change it (PDR-0009).

## Rejected

| Option | Why rejected |
|---|---|
| Out of scope; fix through Renew | Renew moves the expiry: the vendor gives away a term or the site owner loses the original date. Perpetual licenses cannot be renewed, so PDR-0033's perpetual dependants could never move |
| Reissue under a new reference | The old and new keys combine; the implementor must remove the old key everywhere |
| Separate Correct and Re-sign actions | Same mechanics. The summary's *nothing differs* already shows a re-sign |
| Role fixed on reissue | A base issued as an add-on cannot be corrected in the tool |
| Order reference entered afresh, as for a renewal | A correction is the same purchase |
| Block reissues that reduce expiry or features | The vendor may have a reason; the add-on cannot know. A warning is enough |
| Bulk re-sign every license depending on a retired key | Each key is still sent to its site owner one by one. May be added later |
| Reissue an expired license with its old expiry | The library rejects a past expiry |

## Consequences

- Kinds of issue in the records: new sale, add-on, renewal, reissue.
- PDR-0033's site owners to contact after an emergency switch, and its perpetual dependants, are
  moved to the new signing key by Reissue.
- Out of scope remains taking anything back: refunds and revoking keys are not possible offline.
  A reissue corrects; it cannot make a site stop using the key it has.
- A site owner who bought the wrong tier and asks to swap is a commercial decision for the
  vendor; Reissue is the mechanism if the vendor agrees.

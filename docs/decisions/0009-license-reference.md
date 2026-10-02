# PDR-0009: License reference: same reference supersedes, different ones combine

- **Status:** Decided, 2026-09-28. Amended, 2026-10-01: superseding is scoped to one product;
  only a key that verifies supersedes; the latest issued wins even when the reissue changes
  the role. Amended, 2026-10-02: a key supersedes only keys issued strictly earlier; keys tied
  for latest both count and are flagged as a vendor error; exact copies of one key count once
- **Source:** `openspec/changes/license-key-management/design.md` Q8; amendments: Q18, Q19
- **Serves:** site owner, implementor, vendor

## Decision

Every key carries a short, random license reference, signed into the key, generated when a
license is first issued and kept when it is reissued. It names one license: one entitlement to
one product. Not a key, an order or a customer.

- Keys with the **same** reference: the latest issued supersedes the others.
- Keys with **different** references: separate purchases; they combine.

The reference is unique per product (format: PDR-0017), reveals nothing, is readable before
the signature is trusted, and is **not secret and not proof of ownership**.

**Which keys take part in superseding** (amended 2026-10-01 and 2026-10-02).

| Rule | Detail |
|---|---|
| Same product only | Superseding considers only keys for the product being evaluated. A key for another product is reported as wrong product (PDR-0011) and neither supersedes nor is superseded, even with an identical reference |
| Verified keys only | A key supersedes only if it verifies: readable and signed by a trusted signing key. A key that is unreadable or fails verification (PDR-0019) never supersedes. Its reference and issue time are unverified assertions |
| Expiry plays no part | A verified key that has expired still supersedes older keys under its reference. The older key does not count again |
| Role plays no part | The latest issued wins even when its role differs from the key it supersedes. The evaluation result reports that the superseded key carried a different role |
| Strictly earlier only | A key supersedes only keys issued strictly earlier under its reference. Issue times are recorded to the second (PDR-0016) |
| Tied for latest | Different keys under one reference with the same issue time, and none later, **all count** and combine like any other keys (PDR-0011). Each is flagged as a *vendor error* naming the others. The flag applies only while the keys are tied for latest; once a later key exists they are superseded like any others |
| Exact copies | The same key supplied more than once counts once. The first copy is reported normally; each further copy is reported as *duplicate* |

```
  REFERENCE   KEY          ISSUE TIME   MEANING                        RESULT
  different   -            -            two purchases                  combine
  same        identical    same         one key supplied twice         duplicate, counts once
  same        different    different    reissue                        later supersedes
  same        different    same         two versions, order unknown    both count, vendor error
```

## Why

- Resolves renewal vs capacity pack without an "add" marker: a renewal left beside the old key
  (`max 500` twice, same reference) stays 500; a capacity pack (different reference) adds.
- An old key left in place at renewal is superseded, not a conflict.
- Site owners and implementors can quote "LIC-8F3A" for support or renewal without handling the
  key; vendors find one license's history.
- Safe in the backoffice, emails, order forms and the renewal link, because it grants nothing.

For the amendment:

- **Same product.** References are random per product (PDR-0017), so two vendors can generate
  the same one. Without the scope, one vendor's key could switch off another vendor's license.
- **Verified keys.** The reference is readable before the signature is trusted, but an
  unverified reference proves nothing. If it could supersede, a mangled paste of a reissued key
  would take down the working key beside it, and a forged key with a later issue time would
  switch off a paid license.
- **Expiry.** A reissue can shorten or correct an expiry date. If an expired key stopped
  superseding, that correction would undo itself on the day it expired: the older key left in
  place would count again. Old keys would also reappear as *expired* instead of *superseded*,
  giving the site owner several rows to act on instead of one.
- **Role.** The role is a claim like any other, and a reissue is how a vendor corrects a wrong
  claim (`docs/license-examples.md` example 4). A vendor who issued a base as an add-on by
  mistake fixes it by reissuing under the same reference, and the old key left in place is
  superseded as usual. One rule for every claim.
- **Tied for latest.** Two different keys under one reference with one issue time are two
  versions of one license, and the library cannot tell which is current. The tie is the
  vendor's mistake: it reissued one license twice in the same second. So the cost falls on the
  vendor, not on the site owner or implementor. One of the tied keys is the correct one, so
  when both count the site owner never has less than they paid for, and the site continues to
  operate without any issue. It is also the simplest rule: nothing new, the keys combine as any
  others do.
- **Exact copies.** A key present in both a settings file and an environment variable is one
  key. Counting it twice would double its numbers. It is not a vendor error and harms nothing.

## Rejected

| Option | Why rejected |
|---|---|
| Using the reference as proof of ownership for reissue | Anyone who sees it (screenshot, support email, agency handover) could buy a cheap add-on quoting it and, under an entitlement key, receive the whole entitlement |
| A reissue that changes the role is a conflict | A paid license stops until the implementor removes the old key by hand, which contradicts "an old key left in place is superseded, not a conflict". A wrong role could then only be corrected under a new reference, and the wrong key left in place would combine with the corrected one (`max-orders 500` twice gives 1000) |
| A role change is refused at the vendor only, and left undefined in the core | The core keeps no records (PDR-0005) and cannot enforce it, vendors with their own issuing systems may not follow it, and it blocks a legitimate correction. The core's behaviour must be defined either way |
| Any readable key supersedes, verified or not | See Why: a mangled or forged key could switch off a valid license |
| Only a key that is currently valid supersedes, so an expired key stops superseding | See Why: a corrected or shortened expiry would undo itself |
| On a tie, neither key counts | A paid product stops because of the vendor's timing accident. The greatest impact falls on the site owner, who did nothing wrong |
| On a tie, one key counts, chosen by a fixed rule such as the higher key part | The rule is arbitrary and can pick the wrong key, leaving the site owner with less than they paid for |
| On a tie, the first key supplied counts | The result would change with the order of the keys, so one site could differ between environments |
| Exact copies each count | Supplying a key twice would double its numbers |

## Consequences

- "Latest issued" requires the key to record when it was issued: a UTC date and time set by the
  core (PDR-0016).
- A wrong role on a reissue changes what the site has, like any other reissue mistake. A base
  reissued as an add-on leaves the product unlicensed (the add-on is *inactive: no valid
  base*); an add-on reissued as a base licenses the product. Both are fixed by another reissue.
  The role-change note in the evaluation result gives the site owner and implementor the cause.
- The issuing add-on (PDR-0006) should warn, not refuse, when a reissue changes the role. That
  belongs to the issuing add-on's own change.
- **A tie grants more than was sold**, and that is accepted as the vendor's cost. A reissue
  submitted twice gives `max-orders 2000` twice, so 4000. If the tied keys carry different text
  for one feature, that feature answers nothing until the vendor reissues (PDR-0018).
- **The vendor may never hear of a tie.** The flag appears at the site, and the core keeps no
  records (PDR-0005). The vendor's protection is at issue: the issuing add-on (PDR-0006) should
  refuse a second issue under one reference in the same second, and vendors with their own
  systems should do the same. That belongs to the issuing add-on's own change.
- A tie is fixed by reissuing once more: the new key is later than both and supersedes them.
- Worked examples: `docs/license-examples.md` examples 3 to 7.
- How a key that fails verification is itself reported is in PDR-0019. It never supersedes.
- A renewal and the key it replaces share a reference. The key identifier (PDR-0020) tells
  the two key strings apart.

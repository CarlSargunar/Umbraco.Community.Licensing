# PDR-0019: A failed key reports its reason and its claimed identifiers only

- **Status:** Decided, 2026-10-02. Amended the same day: the *duplicate* state and the flags
  were added to the list (design.md Q19)
- **Source:** `openspec/changes/license-key-management/design.md` Q5
- **Serves:** site owner (never shown a false entitlement; can quote which purchase is broken), implementor (each failure names a key and a first action), vendor (nothing in a failed key can be read as a feature); **Cost to:** site owner (a failed key does not say what it would have granted)

## Decision

**The rule.** An unverified claim may identify a key. It may never describe or grant an
entitlement.

```
  TIER                    WHAT THE LIBRARY KNOWS          THE RESULT REPORTS
  unreadable              nothing but the string itself   reason; identifier if its start survived
  readable, not verified  claims as ASSERTIONS            reason; claimed product and identifier
  verified                claims as FACTS                 state and every claim
```

A key that fails verification reports its **reason**, its **claimed product ID** and its
**claimed key identifier** (PDR-0020), worded as claims ("claims to be"). It never reports its
role, issue time, expiry or features.

**One state per key, first failing check wins.**

```
  1  cannot be read                              -> unreadable
  2  claims another product                      -> wrong product
  3  signed with a signing key this product      -> signing key not recognised
     does not trust
  4  signature does not verify                   -> not verified
  ---- verified from here: claims are facts ----
  5  exact copy of a key earlier in the list     -> duplicate      PDR-0009
  6  older key under the same reference          -> superseded     PDR-0009
  7  past its expiry date                        -> expired        PDR-0016
  8  add-on with no valid base                   -> inactive       PDR-0011
     otherwise                                   -> valid
```

A row may also carry a flag. A flag is not a state and does not stop a key counting: *vendor
error* on keys tied for latest and *role changed* on a superseded key (both PDR-0009), and
*conflict* on a text feature (PDR-0018).

A wrong-product key is always reported with claimed identifiers only. Whether it would verify
is not this product's question.

**Reasons are named for what the library observed**, not for a presumed cause. "Tampered" is
not a reason.

| Reason | First action | Who acts |
|---|---|---|
| Unreadable | Paste the key again | Implementor |
| Wrong product | Move the key to the product it names | Implementor |
| Signing key not recognised | Update the product. If that fails, ask the vendor | Implementor, then vendor |
| Not verified | Paste the key again. If that fails, ask the vendor | Implementor, then vendor |
| Expired | Renew | Site owner |

## Why

- **Identifiers are needed, and safe.** Each product has its own signing key
  (`docs/personas.md`), so another product's key pasted by mistake never verifies here. The
  only way to say "this is a key for zenith.forms" is to read its unverified product claim.
  With the reason alone the implementor sees "signing key not recognised", which is true and
  useless. Product ID and key identifier grant nothing and are not secret (PDR-0009, PDR-0020).
- **Entitlement claims serve nobody when unverified.** "Expires 2099" on an edited key must
  never be shown as truth. Role, expiry and features of a failed key answer no persona's
  question; the vendor's support can look the license up by its reference.
- **One reason per key.** After verification fails, every later check (superseding, expiry,
  role) would rest on a claim the result may not report.
- **Wrong product before the signature.** Checking the signature first would hide every
  mis-paste behind "signing key not recognised".
- **"Signing key not recognised" is separate from "not verified".** It is the only failure an
  implementor can fix without a new key, and it is what a site owner meets after a vendor
  changes signing keys.
- **Observed, not presumed.** The usual cause of a signature that does not verify is a paste
  error, not an attack. "Tampered" beside a key the site owner paid for alarms them for no
  reason.

## Rejected

| Option | Why rejected |
|---|---|
| Reason only, no claims | The implementor cannot tell a mis-paste from an outdated product, and the site owner cannot tell which purchase is broken |
| Every readable claim, marked unverified | Relies on every host and every product respecting the marker. A careless host shows "expires 2099" as fact; product code could read a feature from a failed key |
| Signature checked before product | See Why: mis-pastes are reported as an unrecognised signing key |
| "Signing key not recognised" merged into "not verified" | Different first action: update the product, not paste again |
| "Tampered" as a reason | Names a cause the library cannot know |
| Every reason that applies to a key, reported together | An unverified key's further reasons rest on claims that may not be reported |

## Consequences

- **The cost.** A site owner who paid for `ai-assist` and whose add-on key fails sees the
  key's identifier, not "ai-assist is missing". They match the reference to their purchase.
- The library cannot tell a paste error from a forgery. Both are *not verified*, and neither
  affects verified keys that share their reference (PDR-0009).
- The library cannot tell an outdated product from a withdrawn signing key. Wording shown to
  people must not promise that an update will fix it.
- An edited key that claims another product is labelled *wrong product*. Harmless: it drops out
  either way.
- PDR-0011's list of license states is amended to these reasons.
- Worked example: `docs/license-examples.md` example 18.

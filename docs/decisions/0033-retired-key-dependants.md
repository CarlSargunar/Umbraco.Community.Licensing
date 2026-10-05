# PDR-0033: A retired signing key's dependants are its unexpired and perpetual keys

- **Status:** Decided, 2026-10-05
- **Source:** `openspec/changes/issuing-add-on/design.md` Q21. Amends PDR-0031
- **Serves:** site owner (a paid key is not dropped from trust before its term ends),
  implementor (a product upgrade does not turn a valid key into *signing key not recognised*),
  vendor (a date to plan the release that withdraws an old public key, and a list of whom to
  contact); **Cost to:** vendor (a perpetual license keeps the old public key trusted until it is
  reissued)

## Decision

A recorded key **depends on** the signing key that signed it while the key is perpetual or not
yet expired (its expiry is on or after the current UTC date, PDR-0016). Current and superseded
keys count alike. Expired keys do not count.

A license depends on a signing key when any of its keys does. The add-on reports, for a retired
signing key:

| Shown | Content |
|---|---|
| Counts | Licenses depending on it, split: perpetual (a perpetual dependant key), dated (current key is a dated dependant), superseded only (only superseded keys depend on it) |
| Earliest safe withdrawal | The day after the latest expiry among its dependant keys. *Not while perpetual licenses depend on it* when any dependant key is perpetual. *Now* when nothing depends on it |
| List, on request | Each depending license: product, reference, depending key identifiers, their expiry, whether the key is current or superseded |

The report is shown at Make current and Make current now (for the key being retired) and on a
retired signing key's details. It informs; it never blocks an action. Withdrawing a public key
remains a release of the vendor's product (PDR-0031).

Worked example. `acme.commerce` key K1 is retired on 2027-03-01 when K2 is made current.

```
 K1 retired 2027-03-01
 |
 +-- License A: a1 (K1, exp 2027-06-30) --renewed--> a2 (K2, exp 2028-06-30)
 |                 superseded; a site may still run it until 2027-06-30
 +-- License B: b1 (K1, perpetual)
 +-- License C: c1 (K1, expired 2026-12-31)
```

| License | Depends on K1? | Counted as |
|---|---|---|
| A | Yes, through a1 until 2027-06-30 | Superseded only |
| B | Yes, for good | Perpetual |
| C | No: c1 has expired and fails anyway | Not counted |

Report: 2 licenses (1 perpetual, 0 dated, 1 superseded only); earliest safe withdrawal: not
while perpetual licenses depend on it. Without license B: from 2027-07-01.

## Why

- **The add-on cannot see which key a site runs.** A site owner may not yet have passed a
  renewal to the implementor, so a superseded key can still be the one installed. Counting only
  current keys would date withdrawal before that key's term ends, and the site owner who paid
  loses the license on the next product upgrade.
- **The implementor's worst case is an upgrade that breaks licensing.** A key that verified
  yesterday and reports *signing key not recognised* after a product upgrade has no cause the
  implementor can fix on the site. A date that covers every key still in its term avoids it.
- **Perpetual means perpetual.** A perpetual key was sold without an end. Only reissuing it under
  the new signing key frees the old one.
- **A date, not just a count.** The vendor plans releases; "safe from 2027-07-01" is actionable
  where "2 licenses" is not.
- **Information, not a gate.** Withdrawal happens outside the add-on, and an emergency switch
  for a compromised key must not wait. The vendor decides; the add-on shows the consequence.

## Rejected

| Option | Why rejected |
|---|---|
| Count current keys only | Undercounts. A site still running a superseded key breaks before that key expires if trust is withdrawn on the reported date |
| Count expired keys too | They fail either way; withdrawal only changes the message from *expired* to *signing key not recognised*. Pushes the date back by keys that no longer work |
| Block retiring, or warn at every issue, while dependants exist | Withdrawal is not an add-on action, and blocking would stop an emergency switch |
| Stop counting perpetual keys after a fixed number of years | Guesses that a site is abandoned; the site owner bears the risk of a wrong guess |
| Count keys instead of licenses | The site owner bought a license; the vendor contacts per license. Keys are shown in the list |

## Consequences

- Rotation step 5 (PDR-0031) now has a date: keep trusting the old public key until the earliest
  safe withdrawal shown for it.
- **Vendor:** a perpetual license signed by a retired key keeps that public key in every release
  until the license is reissued under the new key (PDR-0034).
- **After an emergency switch** the report is the list of site owners to contact. Withdrawing a
  compromised key at once is still the vendor's right call; those site owners' keys stop working
  on the release that withdraws it until they receive a key signed by the new signing key, by
  Reissue (PDR-0034).
- **Implementor:** after withdrawal, an already expired key signed by the withdrawn key reports
  *signing key not recognised* instead of *expired*. The key had stopped working anyway. The
  vendor's "Inspect a key" uses retired public keys and still reports it as expired.
- Nothing enforces the date. A vendor that withdraws early breaks the listed licenses.

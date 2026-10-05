# PDR-0031: Rotating a signing key is two steps: create pending, then make current

- **Status:** Decided, 2026-10-05. Amended by PDR-0033, 2026-10-05: which licenses depend on a
  retired signing key, and the earliest safe withdrawal date
- **Source:** `openspec/changes/issuing-add-on/design.md` Q19
- **Serves:** site owner (a key bought after a rotation works on the site they run), implementor
  (no forced product upgrade to install a renewal), vendor (rotation fits its release cycle);
  **Cost to:** vendor (a second action after shipping a release)

## Decision

In the issuing add-on, a product's signing key has one of three states:

| State | Signs new keys | Public key | Per product |
|---|---|---|---|
| Pending | No | Shown and exportable, for shipping in a release | At most one |
| Current | Yes | Shown and exportable | Exactly one |
| Retired | No | Shown and exportable; used to inspect keys it signed | Any number |

Rotation is two separate actions:

```
  [K1 current] --rotate--> [K1 current, K2 pending] --make current--> [K2 current, K1 retired]
                                   |                        ^
                                   | export K2 public key   | vendor has shipped a release
                                   v                        | trusting K1 and K2
                           K2 signs nothing yet ------------+
```

| Action | Effect | Asks or shows |
|---|---|---|
| Rotate | Creates a new key pair. The private key file is written to the signing keys folder; the new key is **pending**. The current key keeps signing | The new public key for export, and the rotation steps below |
| Make current | The pending key becomes **current**; the previous current key becomes **retired**. Every key issued afterwards is signed by the new key | Asks the vendor to confirm that a release trusting the new public key has shipped, and that keys signed by it fail on sites running an older release |
| Make current now (emergency) | Rotate and make current in one action, for a compromised key or a private key file that is lost and cannot be restored | A warning that every key issued from now on fails on sites that have not installed a release trusting the new public key |
| Discard pending | Removes the pending key's record. It has signed nothing, so no license depends on it | The private key file to delete by hand |

The add-on records when each signing key was created, made current and retired.

**Rotation steps**, shown by the add-on after Rotate and the basis for vendor documentation:

1. Rotate. The new key is pending; the old key keeps signing.
2. Export the new public key and ship a product release that trusts both keys.
3. Wait until the sites that will renew or buy add-ons are likely to run that release. How long
   is the vendor's call.
4. Make the new key current. New sales, add-ons and renewals are signed by it from now on.
5. Keep trusting the old public key in later releases while any license a site still runs
   depends on it. Removing it is optional; a trusted key that signs nothing harms nothing unless
   it is compromised.

**Retired, not withdrawn.** Retired is the add-on's state: the key no longer signs. Withdrawing
a public key is a product release that stops trusting it; that happens in the vendor's product,
not in the add-on.

## Why

- **A site trusts only the public keys in the release it runs** (`signing-key-management`
  spec, Rotation). A key signed by a key the site does not yet trust is reported as *signing
  key not recognised* (PDR-0019; `license-examples.md` example 18, key 6).
- **One-step rotation breaks paid keys.** If rotating made the new key current at once, every
  renewal and add-on issued before the site upgraded would fail. The site owner has paid and
  holds a key that does not work; the implementor must upgrade the product to fix licensing,
  which may not be possible on that site (an older Umbraco version, a frozen release). The
  vendor cannot help: a retired key does not sign.
- **The vendor knows when a release has shipped; the add-on does not.** Splitting the action
  lets the vendor switch when its own release cycle says so, instead of rotating outside the
  tool to avoid the problem.
- **Emergency path.** A compromised key must stop signing at once, and a lost private key file
  cannot sign at all. In both cases the failure on older sites is unavoidable, so the add-on
  warns and does not block.
- **At most one pending key.** Two pending keys would leave the vendor guessing which one it
  shipped.
- **Discard pending.** A pending key has signed nothing, so removing it affects no license. A
  release that already trusts its public key trusts a key that never signs, which is harmless.

## Rejected

| Option | Why rejected |
|---|---|
| Rotate makes the new key current at once, with written guidance to ship the release first | The tool's own flow leads to the failure; guidance is read once |
| Rotate at once, warn on each key issued by a recently created signing key | The vendor cannot tell which release a given site runs, so every issue warns and the warning is soon ignored |
| Sign each key with both the old and the new key | Changes the key format and the library's verification to solve a vendor-side sequencing problem |
| Allow signing with a retired key on request | Repairs individual cases after a site owner has already received a broken key |
| No pending state; the vendor creates the key pair outside the add-on and imports it later | Moves private key handling outside the tool that enforces PDR-0007 |

## Consequences

- Between Rotate and Make current, the product has two signing keys on record and one signing.
  The add-on shows which is which wherever signing keys are listed.
- When the current key's private key file is missing (`product-catalogue` spec), the add-on
  offers, in order: change the signing keys folder, restore the file from backup, make a pending
  key current if one exists, and last, Make current now (PDR-0032).
- After an emergency switch, licenses whose current key was signed by the compromised key keep
  working on sites that still trust it. Moving those site owners to the new key means reissuing
  their keys, which the add-on does not yet offer outside a renewal.
- How many licenses still depend on a retired key, which ones, and the earliest date its public
  key can be withdrawn are shown at Make current, at Make current now and on the signing key's
  details. What counts as depending is PDR-0033.

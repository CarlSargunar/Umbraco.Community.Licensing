# PDR-0015: Signing keys, trusted set and rotation

- Status: Decided
- Date: 2026-10-09
- Source: `add-license-core` design.md R6
- Personas: vendor (keep one private key per product safe; rotate it), implementor (an outdated
  product names the cause), site owner.

## Decision

- Creating a key pair returns a private key, a public key and a signing key ID, each exportable
  separately. The ID is derived from the public key, never chosen, so it cannot be paired with
  the wrong key. The exported public key carries its ID, so a product can trust it with no other
  input. The private key stays with the vendor and is never shipped.
- A product holds a trusted set of public keys addressed by ID. Adding a different key under an
  ID already held raises an error (configuration, not key content) and leaves the set unchanged.
- Rotation: trust the new key alongside the old; switch issuing to the new key; withdraw the old
  ID once no valid licenses depend on it. Licenses signed by a withdrawn key report *signing key
  not recognised*. Adding a key never affects licenses signed with an already-trusted key.

## Reasons

Rotation without a flag day: old and new keys verify side by side. A derived ID removes the
mistake of labelling a key with the wrong ID.

## Rejected options

| Option | Why rejected |
|---|---|
| Vendor-chosen key IDs | Can be paired with the wrong key |
| One trusted key per product | Rotation would invalidate every key at once |

## Consequences

Private key compromise: withdrawing its ID also invalidates every legitimate key it signed
until reissued. Documented as an inherent limit.

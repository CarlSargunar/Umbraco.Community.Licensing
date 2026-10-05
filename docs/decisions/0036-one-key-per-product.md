# PDR-0036: One license key per product holds every purchase; a new purchase replaces the key

- **Status:** Decided, 2026-10-05. Supersedes PDR-0008
- **Source:** `openspec/changes/one-key-per-product/design.md` Q1, Q5, Q6
- **Serves:** implementor (one key per product, replaced in place), site owner (one thing to
  hand over; one renewal date); **Cost to:** site owner (an add-on bought through another
  channel needs the vendor to reissue the license), vendor (cannot sell an add-on on a term of
  its own)

## Decision

A license key stands for the site owner's whole license for one product: one base purchase and
every add-on bought for it (PDR-0037). Each new purchase reissues the license under the same
reference (PDR-0009) with the purchase appended. The new key supersedes the old.

```
  purchase           key issued                       purchases in the key
  Product A          LIC-8F3AK-M7RXB-7Q2D  (key 1)    A
  add-on B           LIC-8F3AK-M7RXB-K9PX  (key 2)    A, B           supersedes key 1
  add-on C           LIC-8F3AK-M7RXB-H8RC  (key 3)    A, B, C        supersedes key 2
```

The implementor replaces the key in place. A key left beside its replacement is superseded, not
a conflict, so a missed replacement in one environment never stops the product.

**One expiry per license.** Every purchase shares the license's expiry. An add-on bought
mid-term ends with its base; on a perpetual license it is perpetual. A renewal reissues the
license with a new expiry and every purchase kept.

```
  base, 12 months, bought 2027-01-10        expires 2028-01-09
  add-on bought 2027-07-01                  expires 2028-01-09   (the license's expiry)
  base perpetual, add-on bought any time    never expires
```

**Reissuing from the current key.** The core keeps no records (PDR-0005). A vendor reissues
from the current key the site owner presents: the library reads its purchases and expiry and
prefills a reissue request, to which the vendor appends the new purchase. A vendor with its own
records may reissue from them instead.

**Several products, several keys.** Each product on a site has its own key. A separately
installed add-on package is a product of its own, as before.

## Why

- **The implementor's job.** Every key is stored in every environment (settings, environment
  variables, vault secrets). Per-purchase keys add one entry per sale; one key per product is
  one entry for its lifetime, replaced in place.
- **The site owner hands over one thing.** "Here is the key for Product A" stays true after
  every purchase.
- **Co-terming without effort.** Add-ons end with their base, so the site owner has one renewal
  date and never holds an add-on that outlives its base. PDR-0035's alignment becomes the rule.
- **Proof of ownership.** PDR-0008 rejected an entitlement key because each reissue hands over
  the whole license, so the vendor must know the requester owns it. The current key is that
  proof: it is a bearer token, and whoever holds it already holds everything it grants. A
  reissue from it gives the holder only what they already have plus what they just bought. The
  reference alone is still not proof (PDR-0009).
- **Prior art.** Standard.Licensing issues one license per product and reissues it to add
  features. This keeps that shape and adds the purchase list (PDR-0037).

## Rejected

| Option | Why rejected |
|---|---|
| One key per purchase (PDR-0008) | One more stored key per sale in every environment |
| Both shapes: bundled key, plus separate add-on keys for other channels | Two combining paths to explain and test; the per-purchase path keeps every cost this decision removes |
| Add-ons with a term of their own inside the key | One key has one expiry. Per-purchase expiry would bring back *inactive* add-ons and several renewal dates |
| Reissue from the reference alone | The reference is not secret (PDR-0009); anyone quoting it would receive the whole license |

## Consequences

- PDR-0011's per-key role and the *inactive* state go; licenses for one product combine as in
  PDR-0038.
- An add-on cannot be bought for a license that has expired without renewing it: the library
  rejects a past expiry (PDR-0016).
- A marketplace or reseller sale of an add-on needs the vendor, or tooling holding the vendor's
  private key, to reissue the license. A sale that cannot do so issues a second license, which
  combines (PDR-0038) at the cost of a second key.
- Removing a purchase (a refund) is a reissue without it. It takes effect only if the site
  installs the new key; the old key keeps granting what it granted. Nothing can be revoked
  offline.
- The issuing add-on's PDR-0024, PDR-0025, PDR-0026, PDR-0027, PDR-0034 and PDR-0035 assume
  per-purchase keys and are revisited when the `issuing-add-on` change resumes.

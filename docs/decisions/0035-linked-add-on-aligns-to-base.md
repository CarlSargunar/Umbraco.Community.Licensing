# PDR-0035: A linked add-on's expiry defaults to its base's expiry

- **Status:** Decided, 2026-10-05
- **Source:** `openspec/changes/issuing-add-on/design.md` Q24
- **Serves:** site owner (one renewal date for a base and its add-ons; no paid add-on time spent
  inactive after the base lapses), vendor (co-terming in one step); **Cost to:** vendor (prices
  an aligned add-on's shorter or longer term itself; checks the default against what was sold)

## Decision

When the vendor issues or renews an add-on license that is linked to a base license (PDR-0026),
the add-on offers two expiries and pre-selects the base's:

| Option | Expiry | Pre-selected |
|---|---|---|
| Align to base | The base license's current key's expiry | Yes |
| Type's term | Calculated from the add-on type's term (PDR-0025) | No |

Either remains editable. The summary shows the add-on's expiry beside its base's, and, when
aligned, the expiry the type's term would have given.

Align to base is offered only when:

| Condition | Why |
|---|---|
| The add-on is linked to a base | The vendor has said which base it belongs to |
| The base's current key has an expiry | A perpetual base has no date to align to |
| The base's current key has not expired | The library rejects a past expiry (PDR-0016) |
| On renewal: the base's expiry is after the add-on's current expiry | A renewal must not cut time the site owner already has |

Otherwise only the type's term is offered, as before. Renewing a base does not change its
add-ons.

Worked example. Base `LIC-8F3AK-M7RXB` expires 2027-10-03; on 2027-04-15 the vendor issues
`Extra 1,000 orders` (12 months), linked to it.

```
 Base    |=========================|  2027-10-03
 Add-on            |===============|  2027-10-03   align to base (pre-selected)
 Add-on            |=========================================|  2028-04-14   type's term
```

| Linked add-on, today 2027-04-15 | Offered |
|---|---|
| Base expires 2027-10-03, type 12 months | **2027-10-03** (base, pre-selected) or 2028-04-14 (type) |
| Base expires 2027-10-03, type perpetual | **2027-10-03** (base, pre-selected) or perpetual (type) |
| Base perpetual | 2028-04-14 only |
| Base expired 2027-03-31 | 2028-04-14 only |
| Unlinked | 2028-04-14 only |
| Renewal on 2027-09-20 of an add-on expiring 2027-09-30; base expires 2028-10-03 | **2028-10-03** (base, pre-selected) or 2028-09-30 (type) |
| Renewal of an add-on expiring 2028-04-14; base expires 2027-10-03 | 2029-04-14 only: aligning would cut the current term |

## Why

- **Co-terming is the common practice.** A site owner thinks of a base and its add-ons as one
  purchase with one renewal date. Separate dates mean separate reminders and separate renewals.
- **No inactive paid time.** An add-on that outlives its base is inactive (PDR-0011). Aligned,
  it never outlives the base it was sold with.
- **Default to the common case.** The Product Owner chose align as the default: a vendor that
  links an add-on to a base usually means to co-term it. The type's term remains one choice
  away.
- **Visible either way.** Showing both expiries at the summary means a default that does not
  match what was sold is caught before the key goes out.
- **Pricing stays outside.** The add-on records no prices (PDR-0023); pro-rating an aligned term
  is the vendor's.
- **No cut on renewal.** Renewal continues the period so the site owner loses no days
  (PDR-0025); aligning must not undo that.

## Rejected

| Option | Why rejected |
|---|---|
| No alignment; the vendor edits the date by hand | The vendor must look up the base's expiry; a typo lands on the site owner |
| Offer alignment with the type's term pre-selected | Product Owner's call: linked add-ons are usually co-termed, so the default should be the common case |
| Always align, no choice | A site owner sold a full term would be short-changed with no way to issue what was sold |
| Renewing a base also renews its linked add-ons | Each add-on is its own sale the site owner may not renew. May be added later |
| Align on renewal even when it cuts the current term | Takes back days the site owner has paid for |

## Consequences

- An aligned add-on's term is usually not a whole number of months. Its next renewal calculates
  from the type's term (PDR-0025), or aligns again to the renewed base.
- An add-on aligned to a base that is later renewed early still expires on the base's old date;
  renewing the add-on then offers the base's new expiry.
- Unlinked add-ons, including those for bases sold elsewhere (PDR-0008), get the type's term
  only. An add-on linked after issue can align at its next renewal.
- The default can give more than the type's term when the base expires later than the term would
  end. The summary shows both dates.

# PDR-0016: Issue and expiry dates are UTC; the core sets the issue time

- **Status:** Decided, 2026-09-30
- **Source:** `openspec/changes/license-key-management/design.md` tenth pass
- **Serves:** implementor (one answer on every server), vendor (reissue ordering cannot break), site owner (no dead-on-arrival keys); **Cost to:** site owner west of UTC (up to 12 hours of the stated expiry day)

## Decision

- `issued` is a **date and time in UTC**, set by the core at the moment of signing. It is never a
  vendor input. It only orders reissues (PDR-0009).
- `expires` is a **date**. The license is valid until the end of that date in UTC
  (`expires 2027-03-01` means valid until 2027-03-01 23:59:59 UTC).
- The inventory shows the expiry moment in the viewer's local time, e.g. "expires 1 Mar 2027,
  15:59 your time".
- Issuing rejects an `expires` before the current UTC date. An expiry of today is allowed.

## Why

- A vendor can reissue twice in one day (a typo corrected an hour later). Only a time orders
  them, so the correction wins.
- Set by the core: a backdated or future-dated `issued` (entered, or from a wrong clock in vendor
  tooling) would let an old key supersede real reissues. `issued` is always "now", so the
  expiry rule also covers "expires before issued".
- UTC for both: one time basis for vendors, implementors and support. The site owner's cost is
  contained by showing the exact local moment in the inventory, by the inventory's advance
  warning of expiry, and by vendors being free to add a day.
- Rejecting a past expiry: a site owner never receives a key that is dead on arrival; a date
  typo (2025 for 2027) fails at the vendor.

## Rejected

| Option | Why rejected |
|---|---|
| `expires` valid until the date has ended everywhere on Earth | Kinder to site owners west of UTC, but a second, unusual convention to explain beside UTC `issued` |
| `expires` in the server's local time | Varies with hosting; one site on two servers can disagree; moving hosts changes the answer |
| `expires` as an exact UTC date and time | Inventory reads "expires 14:32 UTC"; precision nobody sells |
| `issued` as a date only | Same-day reissues cannot be ordered; a corrected key may lose to the typo |
| `issued` meaning "valid from" / an optional "valid from" date | Only matters for future-dated keys, which nobody asked for. Collides with superseding: a newer, not-yet-started key would supersede the current one and leave a gap. Early renewal already works: the renewed key is valid at once with a later expiry |
| `issued` supplied by the vendor | Breaks ordering (see Why). Cost accepted: a vendor migrating from another system cannot keep original issue dates in keys; its own records keep the history (PDR-0005, PDR-0006) |

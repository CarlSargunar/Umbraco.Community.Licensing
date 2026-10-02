# PDR-0016: Issue and expiry dates are UTC; the core sets the issue time

- **Status:** Decided, 2026-09-30. Amended, 2026-10-01: the local-time display and the
  advance expiry warning belong to the deferred inventory ([`docs/deferred-scope.md`](../deferred-scope.md)
  D5); the UTC rules and the core-set issue time stand. Amended, 2026-10-02: the issue time is
  recorded to the second
- **Source:** raised while writing the schema in [`docs/license-examples.md`](../license-examples.md);
  amendment: `openspec/changes/license-key-management/design.md` Q19
- **Serves:** implementor (one answer on every server), vendor (reissue ordering cannot break), site owner (no dead-on-arrival keys); **Cost to:** site owner west of UTC (up to 12 hours of the stated expiry day)

## Decision

- `issued` is a **date and time in UTC, to the second**, set by the core at the moment of
  signing. It is never a vendor input. It only orders reissues (PDR-0009).
- `expires` is a **date**. The license is valid until the end of that date in UTC
  (`expires 2027-03-01` means valid until 2027-03-01 23:59:59 UTC).
- The inventory shows the expiry moment in the viewer's local time, e.g. "expires 1 Mar 2027,
  15:59 your time".
- Issuing rejects an `expires` before the current UTC date. An expiry of today is allowed.

## Why

- A vendor can reissue twice in one day (a typo corrected an hour later). Only a time orders
  them, so the correction wins.
- To the second: two keys under one reference with the same issue time are treated as a vendor
  error (PDR-0009). At minute precision an honest correction made within the same minute would
  be flagged as one. At second precision a person cannot produce a tie; only a system issuing
  twice at once can.
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
| `issued` to the minute | A correction made within the same minute ties with the typo (see Why) |
| `issued` meaning "valid from" / an optional "valid from" date | Only matters for future-dated keys, which nobody asked for. Collides with superseding: a newer, not-yet-started key would supersede the current one and leave a gap. Early renewal already works: the renewed key is valid at once with a later expiry |
| `issued` supplied by the vendor | Breaks ordering (see Why). Cost accepted: a vendor migrating from another system cannot keep original issue dates in keys; its own records keep the history (PDR-0005, PDR-0006) |

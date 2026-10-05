# PDR-0025: A renewal continues the period; after a lapse the vendor chooses

- **Status:** Decided, 2026-10-04. Amended, 2026-10-04: whether a license is perpetual is read
  from its current key, not its license type; a renewal that produces a perpetual key is flagged
  at the summary; the add-on calculates the date and passes it to the library (PDR-0029).
  Amended, 2026-10-05: only active types are offered; features come from the current key when
  the type is kept, with differences from the type's defaults shown at the summary
- **Source:** `openspec/changes/issuing-add-on/design.md` Q5; amendment: Q10, Q11, Q12, Q13
- **Serves:** site owner (early renewal loses no days), vendor (decides how a lapse is charged)

## Decision

**Period.** A term of N months starting on day S runs to the day before S plus N months; the
key expires on that last day (valid to its end, UTC, PDR-0016). Today is the current UTC date.

**New sale.** The period starts today.

**Renewal.** A renewal reissues the license under its reference (PDR-0009) with a new period
from the license type's term:

| Current key | New period starts | Vendor choice |
|---|---|---|
| Not expired (expires today or later) | The day after the current expiry | None |
| Expired (a gap) | Either the day after the old expiry, or today | Required; no default |
| Perpetual (no expiry) | Not renewable: there is nothing to renew | |

The vendor may override the calculated expiry; it must not be before today (library rule).

```
  TERM  CURRENT EXPIRES  TODAY        CHOICE              NEW EXPIRES
  12    2027-03-31       2027-03-10   -                   2028-03-31
  12    2027-03-31       2027-05-20   from old expiry     2028-03-31
  12    2027-03-31       2027-05-20   from today          2028-05-19
  1     2027-01-31       2027-01-15   -                   2027-02-28
  12    (new sale)       2026-10-04   -                   2027-10-03
```

**Type and features.** When renewing, the vendor picks an active license type of the same
product and role: the license's own type if it is still active, or another (an upgrade on
renewal). The term comes from the chosen type.

| Renewal | Features pre-filled from |
|---|---|
| Keeps its type | The current key |
| Switches type | The chosen type's defaults |

Either way the features are editable, and the summary shows where they differ from the chosen
type's defaults. A license whose type is retired must switch to renew. With no active type of
its role, it cannot be renewed until one is defined (PDR-0024).

```
  Commerce Pro defaults: max-orders 600       current key: max-orders 750 (negotiated)
  keep Commerce Pro      --> pre-filled 750, summary notes the type default is 600
  switch to Enterprise   --> pre-filled from Enterprise, summary notes the key had 750
```

**Key, not type.** Whether a license is perpetual is read from its current key. A type and its
keys can disagree, because the expiry is editable at issue (PDR-0024) and a type can be edited
later. The new term always comes from the type chosen at renewal; no term is stored on the
license.

```
  TYPE TERM   CURRENT KEY        TYPE CHOSEN AT RENEWAL   NEW KEY
  12 months   no expiry          -                        not renewable
  perpetual   expires 2027-01-03 (same, perpetual)        no expiry
  perpetual   expires 2027-01-03 Commerce Pro, 12 months  expires 2028-01-03
```

When a renewal would produce a perpetual key, the confirmation summary says so and that the
license cannot be renewed again.

**Who calculates.** The add-on calculates the expiry date and passes the date, or perpetual, to
the library. The period rule is an issuing convention, not a license rule (PDR-0023, PDR-0029).

**Month ends.** Adding months to a date clamps to the month's last day. The period after
2027-01-31 starts 2027-02-01 and one month later ends 2027-02-28. A one-month period starting
2027-01-31 ends 2027-02-27 (2027-02-28 minus a day). The vendor can override the date.

## Why

- **Continue from the old expiry.** The site owner who renews early is not charged for days
  they already paid for.
- **Vendor chooses after a lapse.** Continuing from the old expiry keeps the anniversary but
  charges the site owner for the gap. Starting today gives a full term but moves the
  anniversary. Which is fair is a commercial decision, so it is the vendor's, made per renewal.
  No default, because a silent default is the one the vendor did not choose.
- **No perpetual renewal.** A perpetual key never expires; a new key would change nothing.
- **Key, not type.** The key is what the site has. Reading the type would let a vendor renew a
  key sold as perpetual from a term type, and the expiring renewal would supersede it (PDR-0009):
  the site owner's lifetime license would become a fixed term.
- **Flag a perpetual result.** It cannot be undone by a later renewal.
- **Features from the key when the type is kept.** A negotiated limit is part of what the site
  owner bought, not a one-off; it survives renewal. A changed type default reaches the renewal
  when the vendor sees the difference at the summary and accepts it.
- **Active types only.** A retired type is no longer sold (PDR-0024).
- **Same role only.** A renewal that changed the role would change what the site has without
  the vendor meaning it (PDR-0009 consequences). Changing a license's role is not a renewal.

## Rejected

| Option | Why rejected |
|---|---|
| Always from the old expiry | Takes the commercial choice from the vendor |
| Always from today | Early renewal loses the days left |
| Vendor enters the date every time | Error-prone; the common cases are mechanical |
| Period ends on the same day-of-month (S plus N months) | Gives one day more than the term, and the next renewal drifts |
| The license type decides whether a license is perpetual | Can turn a perpetual key into an expiring one; blocks renewing a fixed-term key issued from a perpetual type |
| Features always from the type's defaults | A negotiated limit silently reverts at renewal |
| A term stored on the license, repeated at renewal | A second source of truth beside the type; an edited date is not a whole number of months. A one-off term is one date edit at the summary |

## Consequences

- Renewal finds the license in the add-on's records. A license issued by other tooling cannot
  be renewed here.
- Because renewals share a reference, the renewed key supersedes the old one on the site
  (PDR-0009); the old key can stay installed.

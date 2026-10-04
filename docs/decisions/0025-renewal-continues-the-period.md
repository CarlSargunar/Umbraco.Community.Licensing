# PDR-0025: A renewal continues the period; after a lapse the vendor chooses

- **Status:** Decided, 2026-10-04
- **Source:** `openspec/changes/issuing-add-on/design.md` Q5
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
| Perpetual | Not renewable: there is nothing to renew | |

The vendor may override the calculated expiry; it must not be before today (library rule).

```
  TERM  CURRENT EXPIRES  TODAY        CHOICE              NEW EXPIRES
  12    2027-03-31       2027-03-10   -                   2028-03-31
  12    2027-03-31       2027-05-20   from old expiry     2028-03-31
  12    2027-03-31       2027-05-20   from today          2028-05-19
  1     2027-01-31       2027-01-15   -                   2027-02-28
  12    (new sale)       2026-10-04   -                   2027-10-03
```

When renewing, the vendor may pick another license type of the same product and role (an
upgrade on renewal); the term and features of the chosen type then apply. Features are
pre-filled from the current key, or from the newly chosen type, and editable.

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
- **Same role only.** A renewal that changed the role would change what the site has without
  the vendor meaning it (PDR-0009 consequences). Changing a license's role is not a renewal.

## Rejected

| Option | Why rejected |
|---|---|
| Always from the old expiry | Takes the commercial choice from the vendor |
| Always from today | Early renewal loses the days left |
| Vendor enters the date every time | Error-prone; the common cases are mechanical |
| Period ends on the same day-of-month (S plus N months) | Gives one day more than the term, and the next renewal drifts |

## Consequences

- Renewal finds the license in the add-on's records. A license issued by other tooling cannot
  be renewed here.
- Because renewals share a reference, the renewed key supersedes the old one on the site
  (PDR-0009); the old key can stay installed.

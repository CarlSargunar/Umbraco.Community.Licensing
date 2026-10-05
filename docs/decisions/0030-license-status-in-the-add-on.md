# PDR-0030: A license in the add-on has one of four statuses, read from its current key

- **Status:** Decided, 2026-10-05
- **Source:** `openspec/changes/issuing-add-on/design.md` Q15
- **Serves:** vendor (one answer to "which licenses need attention?"), site owner (renewals are
  offered before the key lapses)

## Decision

Each license in the add-on's records has exactly one status, read from its current key's expiry
against today (current UTC date, PDR-0016):

| Status | Current key |
|---|---|
| Perpetual | No expiry |
| Active | Expires more than 30 days after today |
| Expiring | Expires today or within the next 30 days |
| Expired | Expired before today |

Lists show the status, and filter by it. The statuses do not overlap: an expiring license is
not also listed as active, and a perpetual one is not active.

```
  TODAY 2027-03-10
  current key expires 2027-05-01   active
  current key expires 2027-03-31   expiring
  current key expires 2027-03-10   expiring (valid to the end of today)
  current key expires 2027-03-09   expired
  current key has no expiry        perpetual
```

The status is the add-on's view of its records. It is not the library's evaluation: a status
does not check a signature, and a site can hold a different key.

## Why

- **One status per license.** A list row shows one status, and a filter means one thing. With
  overlapping statuses, "active" would either hide the licenses that most need a renewal or
  repeat them.
- **Current key.** Renewals supersede earlier keys (PDR-0009); the current key is what the site
  should be running.
- **30 days.** Long enough to arrange a renewal before the key lapses.

## Rejected

| Option | Why rejected |
|---|---|
| Overlapping filters (active includes expiring and perpetual) | A row needs one status; counts by filter do not add up |
| A configurable warning window | A setting for a number that rarely matters; can be added later |

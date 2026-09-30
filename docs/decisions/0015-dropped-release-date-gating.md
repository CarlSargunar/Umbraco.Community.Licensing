# PDR-0015: No release-date gating

- **Status:** Dropped, 2026-09-26, by the Product Owner
- **Source:** `openspec/changes/license-key-management/design.md` R8

## Proposal

After a license expires, releases published before expiry keep working; only newer releases
are refused. Expiry would mean "end of updates" rather than "stop working".

## Why dropped

It adds complication, and the Umbraco version range (PDR-0012) already gives enough commercial
control. What it would have required:

- Expiry's meaning changes from "stop working" to "end of coverage".
- Each package needs a trustworthy, offline, vendor-declared release date.
- Trials would need a hard stop, or a lapsed trial licenses its release forever for free.
- A policy for security patches released after a license lapses.
- A new "lapsed" state; the site keeps working until an upgrade breaks it, so failure is tied
  to a deployment rather than a date.
- A second control overlapping the version range.

## Consequences

Expiry remains a hard stop. The clock-rollback risk in design.md Risks / Trade-offs stands.

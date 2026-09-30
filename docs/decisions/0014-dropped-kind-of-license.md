# PDR-0014: No kind of license (trial / standard)

- **Status:** Dropped, 2026-09-26, by the Product Owner
- **Source:** `openspec/changes/license-key-management/design.md` third pass (R6)

## Proposal

A signed license kind (`Trial`, `Standard`), to mark trials in the inventory, let packages vary
behaviour per kind, require an expiry on trials, and prefer a standard key over a trial when
resolving duplicates.

## Why dropped

Product Owner decision. No further reason was recorded.

## Consequences

- A trial is a key with a short expiry. If a vendor needs to detect a trial, a feature
  (PDR-0010) can carry it by convention.
- The base / add-on role (PDR-0011) is not a kind of license: it is limited to two values and
  used only for combining, so it does not reopen this.
- No "evaluation" registration state either (PDR-0004).

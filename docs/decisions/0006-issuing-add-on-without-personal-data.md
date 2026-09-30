# PDR-0006: Optional issuing add-on; no customer personal data

- **Status:** Decided, 2026-09-28
- **Source:** `openspec/changes/license-key-management/design.md` sixth pass, R10 and Q15
- **Serves:** vendor (small scope, no personal data duties), site owner (keys can be re-sent and renewed)

## Decision

An optional add-on keeps product definitions and issued keys, and calls the core to issue,
list, re-send and renew. The core does not depend on it.

Each issued key record holds: product, key contents, dates, which signing secret signed it, the
key, and an optional free-text order reference. **No name, email or company.** Lookup by order
reference is required.

Customer personal details never go into a key.

## Why

- Most Umbraco package authors are small and have no shop system. Losing issued keys hurts the
  site owner, who cannot get a key re-sent or renewed.
- No personal data by design means the add-on needs no access, deletion or retention features.
  The vendor's shop or payment provider already holds the customer and carries those duties.
- A vendor gets from "I lost my key" to the record by finding the order in their own system,
  then searching the add-on by order reference.
- Recording the signing secret per key tells the vendor which keys are affected if a secret leaks.
- A key is pasted into config and may be logged, so it must carry no personal data.

## Rejected

| Option | Why rejected |
|---|---|
| Optional name, email and company fields | Lowers the vendor's burden but not the add-on's: it cannot know whether personal data was entered, so it would still need to support finding and removing it |

## Consequences

The order reference field cannot be policed. Its label and guidance must say it is for an order
reference and must not hold personal data; the responsibility is the vendor's.

# PDR-0003: The inventory has one row per product; packages register

- **Status:** Decided, 2026-09-26
- **Source:** `openspec/changes/license-key-management/design.md` Q4, R9
- **Serves:** site owner, implementor; **Cost to:** vendor (must register the package)

## Decision

The inventory has one row per product that expects a license, so a missing license can be
reported. Keys that match no registered product are listed separately. Each licensed package
registers itself as installed and expecting a license (R9).

## Why

"Commerce: no license found" is the row a site owner most needs. A view of the store alone
cannot show it, because it does not know Commerce is installed.

## Rejected

| Option | Why rejected |
|---|---|
| Store view: one row per key supplied | Shows what is present, never what is missing |

## Consequences

Adopting the library means a package both asks for validation and declares itself. What it
declares: PDR-0004.

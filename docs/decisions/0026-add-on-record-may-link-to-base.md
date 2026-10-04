# PDR-0026: An add-on license record may link to a base license record

- **Status:** Decided, 2026-10-04
- **Source:** `openspec/changes/issuing-add-on/design.md` Q6
- **Serves:** vendor (sees what a customer bought together), site owner (support can find related
  licenses from one reference)

## Decision

When issuing an add-on license, the vendor may link it to a base license of the same product in
the add-on's records. The link is optional and exists only in the records: the key carries no
link (PDR-0011).

- Showing a base license lists its linked add-ons; showing an add-on names its base.
- The link does not change the add-on's term or features and is never checked at the site.
- An add-on can be issued with no link, e.g. for a base bought from a marketplace or reseller.

## Why

- A vendor answering "what does this customer have?" finds the add-ons from the base reference,
  without personal data to search on (PDR-0006).
- Required would block add-ons for bases sold elsewhere, which PDR-0008 allows.

## Rejected

| Option | Why rejected |
|---|---|
| Required link | Blocks add-ons for bases sold through other channels (PDR-0008) |
| No link | The vendor cannot see from its records what was sold together |
| Link in the key | Rejected by PDR-0011: prevents no sharing and adds failures |

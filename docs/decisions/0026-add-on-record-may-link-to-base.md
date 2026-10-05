# PDR-0026: An add-on license record may link to a base license record

- **Status:** Decided, 2026-10-04. Amended, 2026-10-05: the link belongs to the license and is
  kept across renewals; it can be added, changed or removed at any time, to a base of any
  status; link changes are logged
- **Source:** `openspec/changes/issuing-add-on/design.md` Q6; amendment: Q14
- **Serves:** vendor (sees what a customer bought together), site owner (support can find related
  licenses from one reference)

## Decision

An add-on license may be linked to a base license of the same product in the add-on's records.
The link is optional and exists only in the records: the key carries no link (PDR-0011).

- The link belongs to the license (its reference), not to a key, so renewing either license
  keeps it.
- It can be set at issue or at any time after, and changed or removed at any time.
- The base can have any status, including expired.
- Link changes are logged.
- Showing a base license lists its linked add-ons; showing an add-on names its base.
- The link does not change the add-on's term or features and is never checked at the site.
- An add-on can be issued with no link, e.g. for a base bought from a marketplace or reseller.

## Why

- A vendor answering "what does this customer have?" finds the add-ons from the base reference,
  without personal data to search on (PDR-0006).
- Required would block add-ons for bases sold elsewhere, which PDR-0008 allows.
- **Editable, any status.** A vendor often learns which base an add-on belongs to after selling
  it. A lapsed base still identifies what the customer bought. The link is never signed or
  checked, so being strict about it protects no one.

## Rejected

| Option | Why rejected |
|---|---|
| Required link | Blocks add-ons for bases sold through other channels (PDR-0008) |
| No link | The vendor cannot see from its records what was sold together |
| Link in the key | Rejected by PDR-0011: prevents no sharing and adds failures |
| Link per key, set at issue only | Lost or repeated at each renewal; a link missed at issue can never be added |
| Link only to a base that is not expired | A lapsed base still identifies the customer's purchase |

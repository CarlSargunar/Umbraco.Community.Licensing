## Why

Most Umbraco package authors are small and have no shop system. The core library signs keys
but keeps no records (PDR-0005), so a vendor without its own tooling loses issued keys and
cannot re-send or renew them, at the site owner's cost. PDR-0006 and PDR-0007 decided an
optional issuing add-on to fill that gap. This change builds it, and with it a worked example of
vendor tooling that leaves every license rule to the library.

## What Changes

- New **issuing add-on**: an interactive console tool for one vendor, distributed as a .NET
  tool, persisting to a small local database in a folder the vendor chooses. It never checks a
  license rule itself; the library decides.
- **First-run setup**: chooses the data folder, the signing keys folder (kept apart from the
  data, PDR-0007) and the vendor's product ID prefix. The data folder can be moved later.
- **Product catalogue**: products, and per product, **license types**: templates with a name, a
  role (base or add-on), a term in months or perpetual, and default features. Editable at issue
  (PDR-0024).
- **Signing keys per product**: created with the product, private key written to a PEM file in
  the signing keys folder, never to the database. Public key export. Rotation.
- **Issuing**: new sale (base), add-on (optionally linked to a base license record, PDR-0026)
  and renewal (continues the period; on a lapse the vendor chooses, PDR-0025). An optional order
  reference is signed into the key as the vendor tag (PDR-0027). Safeguards from PDR-0009 and
  PDR-0017: no two keys under one reference in the same second; a reference that already exists
  for the product is never used for a new license.
- **Records**: list and search licenses by license reference, key identifier or order
  reference; show a license's key history and current key string for re-sending; inspect a
  pasted key using the library's evaluator; export all data to CSV files, key strings included
  (PDR-0028).
- **Activity log**: every action written to a log file in the data folder; never a key string or
  private key (PDR-0028).
- **Library additions** (`license-generation`): check a license request without signing it, and
  read a license reference, so the add-on validates with the library's own rules.
- New product decisions PDR-0023 to PDR-0028; PDR-0006 amended by PDR-0027. New technology
  decisions ADR-0003 to ADR-0005.

Out of scope:

- Customer personal data of any kind (PDR-0006).
- Correcting a key outside a renewal, refunds, and revoking keys (not possible offline).
- Passphrase-encrypted private key files, multi-user or concurrent use, a network service.
- Integrating with a shop or payment provider; command verbs for scripting.
- Importing licenses issued by other tools.

## Capabilities

### New Capabilities
- `issuing-setup`: first-run setup, the settings that locate the data, moving the data folder.
- `product-catalogue`: products, license types, and each product's signing keys (create, export
  public key, rotate).
- `license-issuing`: issuing for a new sale, an add-on and a renewal, with the records kept and
  the safeguards applied.
- `license-records`: finding licenses, showing and re-sending keys, inspecting a pasted key,
  CSV export.
- `activity-logging`: what is logged, where, and what is never logged.

### Modified Capabilities
- `license-generation`: adds checking a request without signing, and reading a license
  reference. Its base spec is in the unarchived `license-key-management` change; archive that
  change first.

## Impact

- New project `src/Umbraco.Community.Licensing.Issuing` (console, packed as a .NET tool) and
  `tests/Umbraco.Community.Licensing.Issuing.Tests`; both added to the solution.
- New dependencies, in the add-on only: Spectre.Console, EF Core with SQLite, Serilog
  (ADR-0003 to ADR-0005). The core library stays BCL-only.
- Two public additions to the core library's issuing API; no change to the key format or to
  evaluation.
- Docs: PDR-0023 to PDR-0028, PDR-0006 amendment, ADR-0003 to ADR-0005, decision index,
  `docs/license-examples.md` "Last checked against" line, README status.

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
- **First-run setup**: chooses one location, default `<home>/.<toolname>`, holding a data folder
  and a signing keys folder kept apart (PDR-0007, PDR-0032), and the vendor's product ID prefix.
  Folders are machine settings, never in the database, so a restore on a new machine needs no
  path fixing; signing key files are checked at setup and at every start. The data folder can be
  moved later.
- **Product catalogue**: products, and per product, **license types**: templates with a name, a
  role (base or add-on), a term in months or perpetual, and default features. Editable at issue
  (PDR-0024).
- **Signing keys per product**: created with the product, private key written to a PEM file in
  the signing keys folder, never to the database. Public key export. Signing key export as a backup
  (PDR-0032). Two-step rotation: a new
  key is pending until the vendor makes it current, after shipping a release that trusts it
  (PDR-0031). A retired key shows the licenses still depending on it and the earliest date its
  public key can be withdrawn (PDR-0033).
- **Issuing**: new sale (base), add-on (optionally linked to a base license record, PDR-0026)
  and renewal (continues the period; on a lapse the vendor chooses, PDR-0025). A linked add-on's
  expiry defaults to its base's (PDR-0035). Reissue under the
  same reference corrects a key or re-signs it with the current signing key, keeping its terms
  by default (PDR-0034). An optional order
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
- **Library change** (`license-generation`): a request states its expiry as a date or perpetual,
  exactly one; an omitted expiry no longer means perpetual (PDR-0029). The add-on calculates
  dates from license type terms and passes the result.
- New product decisions PDR-0023 to PDR-0035. Amended: PDR-0006 (by PDR-0027), PDR-0023 (by
  PDR-0029, PDR-0032 and on 2026-10-05), PDR-0024, PDR-0025, PDR-0026 (on 2026-10-05 and by PDR-0034), PDR-0028 (on 2026-10-05
  and by PDR-0032), PDR-0031 (by PDR-0033). New technology decisions ADR-0003 to ADR-0005.

Out of scope:

- Customer personal data of any kind (PDR-0006).
- Refunds and revoking keys (not possible offline: a reissue corrects, it cannot make a site stop
  using the key it has).
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
  reference; requires the expiry to be stated as a date or perpetual.

## Impact

- New project `src/Umbraco.Community.Licensing.Issuing` (console, packed as a .NET tool) and
  `tests/Umbraco.Community.Licensing.Issuing.Tests`; both added to the solution.
- New dependencies, in the add-on only: Spectre.Console, EF Core with SQLite, Serilog
  (ADR-0003 to ADR-0005). The core library stays BCL-only.
- Two public additions to the core library's issuing API, and one breaking change: a request
  without a stated expiry is rejected. The library is unreleased. No change to the key format or
  to evaluation.
- Docs: PDR-0023 to PDR-0035 and the amendments above, ADR-0003 to ADR-0005, decision index,
  `docs/license-examples.md` "Last checked against" line, README status.

# PDR-0028: CSV export includes key strings; the log never does

- **Status:** Decided, 2026-10-04. Amended, 2026-10-05: features are exported in their own files,
  six files in all
- **Source:** `openspec/changes/issuing-add-on/design.md` Q8, Q9; amendment: Q18
- **Serves:** vendor (a full copy of its records in a portable form; a log of what it did)

## Decision

**Export.** The vendor can export all records to CSV files in a folder it chooses, one file per
table: products, license types, license type features, signing keys (public part only), issued
keys and issued key features. Each features file has one row per feature, naming the license
type or key identifier it belongs to. Issued keys include the key string. Before writing, the tool warns that the files hold working keys and should be kept like
the data folder. Private keys are never exported; the database does not hold them (PDR-0007).

**Log.** Every action is logged with when it happened and the identifiers it touched: setup,
data folder moves, product, license type and signing key changes, add-on link changes, issues (license reference,
key identifier, product, role, expiry), lookups that show a key string, exports, and errors.

Never logged: a key string, any part of a key after its key identifier, a private key, the
contents of a private key file. Key identifiers, license references, order references and
signing key IDs may be logged.

## Why

- **Keys in the export.** The database already holds them (PDR-0006); a copy without them could
  not re-send a key. The export is the vendor's own data, made on request.
- **Not in the log.** A log is read, shared with support and kept longer than intended. A key
  is a bearer token. The identifiers are enough to trace any action to its record.
- **Order references in the log.** They are not personal data (PDR-0027) and are how a vendor
  traces an action to an order.

## Rejected

| Option | Why rejected |
|---|---|
| Export without key strings | Not a full copy; re-sending needs the tool |
| Full key strings in the log | A log becomes a source of working keys |
| One combined CSV | Products, types and keys have different columns; one file per table is easier to open and re-import |
| Features in one cell of the keys or types file | Text feature values may contain any separator (PDR-0018), so the cell needs escaping, and it cannot be filtered in a spreadsheet |
| A column per feature name | The columns change from one export to the next |

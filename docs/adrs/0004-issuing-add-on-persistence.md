# 0004: Issuing Add-on Persistence: SQLite with EF Core, Settings in a Per-User File

## Status

Decided, 2026-10-04. Source: `openspec/changes/issuing-add-on/design.md`, Decisions.

## Context

The add-on keeps products, license types, signing key records and issued keys (PDR-0006) for one
vendor on one machine, in a folder the vendor chooses and can move (PDR-0023). Private keys are
stored apart, as files (PDR-0007). The tool must find its data folder on the next start, on
Windows, macOS and Linux. Volumes are small: hundreds to tens of thousands of keys.

## Decision

**Database.** SQLite, one file `issuing.db` in the data folder, through EF Core
(`Microsoft.EntityFrameworkCore.Sqlite`). Schema managed by EF Core migrations committed to the
repo and applied at start (`Database.Migrate`). A `SchemaInfo` row marks the file as this tool's
database.

**Model.**

```
  Settings      1 row: product ID prefix, signing keys folder
  Product       ProductId (unique), DisplayName
  LicenseType   Product, Name (unique per product, NOCASE), Role, TermMonths (null = perpetual),
                DefaultFeatures (JSON), Retired
  SigningKey    Product, SigningKeyId (unique), PublicKeyPem, PrivateKeyPath, Created, Retired
  License       Product, Reference (unique per product), LicenseType, Role, BaseLicense (nullable)
  IssuedKey     License, KeyIdentifier (unique), Kind, Issued, Expires, Features (JSON),
                OrderReference, SigningKey, KeyString
                unique (License, Issued)
```

- Features are stored as JSON in the shape `{name: true | number | "text"}`, numbers written as
  strings in the database to keep `decimal` exact, and rebuilt into `LicenseFeature` values for
  the library.
- References and key identifiers are stored in their displayed form, as the library returns
  them, so lookup normalises the input with the library (`license-generation`: read a license
  reference) and compares exactly. Order references compare `NOCASE`.
- `unique (License, Issued)` backs the same-second rule (PDR-0009) at the database.
- Times are stored as UTC text (`yyyy-MM-ddTHH:mm:ssZ`), dates as `yyyy-MM-dd`.

**Settings file.** `settings.json` in
`Environment.GetFolderPath(SpecialFolder.ApplicationData)/Umbraco.Community.Licensing/`
(`%APPDATA%` on Windows, `~/.config` on macOS and Linux). It holds only the data folder path.
Everything else lives in the database, so moving or copying the data folder carries it.
`--data <folder>` overrides the file for one run.

**Private key files.** `<signing keys folder>/<productId>.<signingKeyId>.private.pem`, PKCS#8
PEM from the library's export. On Unix, created with mode `0600`. The database stores the full
path.

**Moving the data folder.** SQLite online backup (`SqliteConnection.BackupDatabase`) into the
new folder, open the copy and compare row counts, update `settings.json`, then delete the old
file on confirmation.

## Alternatives Considered

- **JSON files per table:** no dependency and diff-friendly, but no transactions or unique
  constraints; the same-second and unique-reference rules would rely on code alone. Rejected.
- **LiteDB:** document store, simple, but smaller ecosystem and no relational constraints; the
  user's preference is EF Core. Rejected.
- **SQLite with Dapper:** lighter than EF Core, but hand-written schema and migrations. EF Core
  migrations handle schema upgrades for vendors on older versions. Rejected.
- **Settings inside the data folder only:** the tool could not find the data folder without
  asking every start. Rejected.

## Consequences

- Dependencies in the add-on only: `Microsoft.EntityFrameworkCore.Sqlite`,
  `Microsoft.EntityFrameworkCore.Design` (build-time, for migrations).
- One instance at a time is assumed; SQLite's locking makes a second instance fail on write
  rather than corrupt data.
- Every schema change ships as a migration; older databases upgrade at start.

## Reversal Cost

Moderate. Services depend on the `DbContext`; swapping the store means replacing the data layer
and migrating vendors' existing databases.

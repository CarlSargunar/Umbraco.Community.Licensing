# Feature requests

Requests for a future change. Not decided, not scheduled. A request that is taken up moves into
a change's design.md as a Q-number and is settled there by a PDR or ADR.

Cite as FR-N.

| FR | Title | Raised | Status |
|---|---|---|---|
| FR-1 | Vendor-chosen key identifier prefix | Carl, 2026-10-09 | Taken into `add-license-core` (design.md Q20, PDR-0023) |
| FR-2 | Vendor issuing console tool | Carl, 2026-10-09 | Open; a later change after `add-license-core` |
| FR-3 | Example product package | Carl, 2026-10-09 | Open; possible later change |

## FR-1: Vendor-chosen key identifier prefix

**Request.** A vendor may replace the `LIC-` prefix of the key identifier with its own (e.g.
`ACME-8F3AK-M7RXB-7Q2D`) or omit it (`8F3AK-M7RXB-7Q2D`). Optional; `LIC-` stays the default.

**Personas.** Vendor: branded keys. Site owner and implementor: a key that names its vendor may be
easier to match to a product; but keys from different vendors no longer share one recognisable
shape.

**Affected records.** PDR-0004 (`LIC-` display form; `LIC` ignored when a reference is passed
in), PDR-0020 (identifier format fixed for good), ADR-0001 (identifier pattern),
`docs/license-examples.md`.

**Issues to settle.**

1. **Older products read new keys as *unreadable*.** The identifier is checked before
   verification (PDR-0020). A product built on a library version that knows only `LIC-` reports a
   custom-prefix key as *unreadable*, first action "paste the key again", which cannot fix it.
   *Not supported* (PDR-0018) does not apply: it is reached only after verification. Avoidable
   only if the first released version already accepts any prefix within a prefix rule (see Q
   below).
2. **Prefix rule.** Characters, case and length must be fixed: no `.` (segment separator), no
   whitespace (removed on read), safe in email and environment variables, short enough to keep
   the identifier readable. The prefix is inside the signing input, so it is authentic on a
   verified key and a claim otherwise.
3. **Reference passed in on reissue.** PDR-0004 accepts `lic-8f3ak m7rxb`. `LIC` can be stripped
   unambiguously because `I` and `L` are not in the reference alphabet. A custom prefix made of
   alphabet characters (e.g. `ACME`) is resolved by length only (a reference is 10 characters);
   the rule must say which prefixes are accepted there. Reissue keeps the reference (PDR-0021);
   whether it also keeps the prefix must be decided.
4. **Recognising a key.** A fixed `LIC-` lets implementors, hosts and secret scanners spot a
   license key in settings or logs by one pattern. Custom prefixes weaken this; no prefix removes
   it.
5. **Where the prefix is set.** Per product or per issue; and whether evaluation must be told the
   expected prefix or accepts any prefix matching the rule.

**Open question for `add-license-core` (before keys ship).** Should the first release's reader
accept any prefix matching a prefix rule, while the issuer still writes only `LIC-`? Cheap now;
avoids issue 1 for every product version released before FR-1. Otherwise FR-1 later requires
products to update before custom-prefix keys are issued to their sites.

## FR-2: Vendor issuing console tool

**Request.** A console application, shipped alongside the library, that small vendors use to
issue keys. It uses the library for every signing and verification step and adds what the
library leaves out: records of products, feature definitions and issued keys, and a guided
first-run setup (signing keys, issuing prefix, configuration values).

**Constraints (Carl).** Shipped as a .NET global tool. Its own database: SQL Server or SQLite.
Runs entirely on the vendor's hardware. One vendor per installation: one database, one issuing
setup, one prefix. A vendor needing a second issuer runs the tool from another path with another
database.

**Personas.** Vendor: issue and reissue from records instead of from the key a site owner
presents. Site owner: unaffected; keys are the same.

**Personal data.** The tool may hold customer records; the library and keys never do
(PDR-0014). What the tool stores is settled in the tool's change.

**Affected records.** PDR-0013 (reissue from a presented key stays the path for vendors without
the tool), PDR-0014, PDR-0016 (definitions held by the tool), PDR-0023 (prefix set at setup).

## FR-3: Example product package

**Request.** A sample package showing a product using the library: trusted keys, evaluation,
feature lookups, and what to show for each state.

**Personas.** Implementor and vendor: a working reference.

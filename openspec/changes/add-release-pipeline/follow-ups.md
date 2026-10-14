# Follow-ups: add-release-pipeline

Minors and nits from reviews. Read at the start of each apply session.

| # | Source | Severity | Item | Status |
|---|---|---|---|---|
| 1 | Section 1 supervisor | nit | `RELEASES.md` intro is two sentences; `add-release-pipeline` task 1.2 says one. Content accurate. | Open; leave unless Carl wants it trimmed |
| 2 | Section 1 supervisor | risk | No `.gitattributes`: a Windows checkout with `core.autocrlf=true` gives `RELEASES.md` and `Version.props` CRLF. Section 3 parser/writer must accept `\r\n` and keep the file's line ending; tests cover it. Alternative (`.gitattributes` with `eol=lf`) needs a new ownership row and Carl's call. | Carl 2026-10-14: handled in code, no `.gitattributes`. Folded into section 3 brief |
| 3 | Section 2 supervisor | risk | `Version.props` holds `<Version>` and `<VersionLine>`; the `add-release-pipeline` task 3.4 writer must match `<Version>` exactly, not as a prefix. Round-trip test covers it. | Folded into section 3 brief |
| 4 | Section 2 supervisor | risk | First release writes an unchanged `Version.props` (already `0.1.0-beta.1`); the commit still holds the new `RELEASES.md` row. `add-release-pipeline` task 4.2 "Release started" tests cover the empty-log case. | Folded into section 4 brief |
| 5 | Section 2 supervisor | nit | Local packs carry a released version; `docs/releasing.md` (`add-release-pipeline` task 6.2) says never push a local pack. | Folded into task 6.2 |

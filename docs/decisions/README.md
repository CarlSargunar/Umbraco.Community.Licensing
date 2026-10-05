# Product decision records

Why the library behaves as it does. One file per decision: what was decided, why, what was
rejected and why, and who it serves. Written for vendors, implementors and site owners asking
"why does it work like this?", and as source material for the library's documentation.

Technology choices (algorithms, formats, packaging) are ADRs in [`../adrs/`](../adrs/). Product
decisions are PDRs here. Cite as PDR-0011, never a bare number.

**Deferred** means the decision stands but its feature was removed from the current change on
2026-10-01, when scope was cut to generation, verification and signing-key management. What
was removed, and why, is in [`../deferred-scope.md`](../deferred-scope.md).

A PDR is the full record of its decision. Its source line names the requirement or question
(R-number, Q-number) indexed in the *Requirements and open questions* section of the design.md
of the change that raised it: `license-key-management` (PDR-0001 to PDR-0022, archived under
`openspec/changes/archive/2026-10-04-license-key-management/`) or `issuing-add-on` (PDR-0023
to PDR-0035), `one-key-per-product` (PDR-0036 onwards), or, for scope that left the change, in
[`../deferred-scope.md`](../deferred-scope.md). The chronological write-up is in git history. A decided PDR
is not yet a spec requirement until the specs are revised.

| PDR | Decision | Status |
|---|---|---|
| [0001](0001-primary-customer-is-the-site-owner.md) | The site owner is the primary customer | Decided |
| [0002](0002-shared-license-store.md) | One license store per site, shared by all vendors | Deferred (host-side) |
| [0003](0003-inventory-is-a-product-view.md) | The inventory has one row per product; packages register | Deferred (host-side) |
| [0004](0004-package-registration-declarations.md) | What a package declares when it registers | Deferred (host-side) |
| [0005](0005-core-signs-only.md) | The core issuing API signs only and keeps no records | Decided |
| [0006](0006-issuing-add-on-without-personal-data.md) | Optional issuing add-on; no customer personal data | Decided; amended by PDR-0022 |
| [0007](0007-signing-secrets-stored-apart.md) | Signing secrets are stored apart from issued keys | Decided |
| [0008](0008-a-key-stands-for-one-purchase.md) | A license key stands for one purchase | Superseded by PDR-0036 |
| [0009](0009-license-reference.md) | License reference: same reference supersedes, different ones combine | Decided; amended 2026-10-01, 2026-10-02, amended by PDR-0038 |
| [0010](0010-product-features.md) | Product features are switches and numbers | Decided; amended by PDR-0018, amended by PDR-0037 |
| [0011](0011-base-and-add-on-licenses.md) | Base and add-on licenses, and how licenses combine | Superseded by PDR-0038 |
| [0012](0012-umbraco-version-range.md) | Umbraco version range: base licenses only, majors, inclusive | Deferred (host-side) |
| [0013](0013-feature-names.md) | Feature names are restricted; lookups ignore case | Decided |
| [0014](0014-dropped-kind-of-license.md) | No kind of license (trial / standard) | Dropped |
| [0015](0015-dropped-release-date-gating.md) | No release-date gating | Dropped |
| [0016](0016-dates-and-times.md) | Issue and expiry dates are UTC; the core sets the issue time | Decided; amended 2026-10-01, 2026-10-02 |
| [0017](0017-identifier-formats.md) | Identifier formats: `vendor.product` and `LIC-XXXXX-XXXXX` | Decided; amended 2026-10-02, 2026-10-03 |
| [0018](0018-text-feature-values.md) | Product features may carry text; text never combines | Decided; amended by PDR-0037 |
| [0019](0019-failed-key-reporting.md) | A failed key reports its reason and its claimed identifiers only | Decided; amended by PDR-0038 |
| [0020](0020-visible-key-identifier.md) | Every key starts with a visible key identifier | Decided |
| [0021](0021-reading-a-key-string.md) | Whitespace in a key string is ignored; contents that break the schema are unreadable | Decided; amended by PDR-0037 |
| [0022](0022-vendor-tag.md) | Optional vendor tag: the vendor's own label, signed into the key | Decided; amended by PDR-0037 |
| [0023](0023-issuing-add-on-is-a-local-tool.md) | The issuing add-on is a local, interactive tool for one vendor | Decided; amended by PDR-0029, amended 2026-10-05, amended by PDR-0032 |
| [0024](0024-license-types-are-templates.md) | License types are templates, editable at issue | Decided; amended 2026-10-05 |
| [0025](0025-renewal-continues-the-period.md) | A renewal continues the period; after a lapse the vendor chooses | Decided; amended 2026-10-04, 2026-10-05 (twice) |
| [0026](0026-add-on-record-may-link-to-base.md) | An add-on license record may link to a base license record | Decided; amended 2026-10-05, amended by PDR-0034 |
| [0027](0027-order-reference-is-the-vendor-tag.md) | The add-on's order reference is signed as the vendor tag | Decided |
| [0028](0028-export-and-log-contents.md) | CSV export includes key strings; the log never does | Decided; amended 2026-10-05 |
| [0029](0029-expiry-is-stated-never-implied.md) | A key's expiry is stated when issuing, never implied | Decided |
| [0030](0030-license-status-in-the-add-on.md) | A license in the add-on has one of four statuses, read from its current key | Decided |
| [0031](0031-signing-key-rotation-is-two-steps.md) | Rotating a signing key is two steps: create pending, then make current | Decided; amended by PDR-0033 |
| [0032](0032-one-location-per-machine.md) | Data and signing keys live under one location, chosen per machine | Decided |
| [0033](0033-retired-key-dependants.md) | A retired signing key's dependants are its unexpired and perpetual keys | Decided |
| [0034](0034-reissue-corrects-a-license.md) | The add-on reissues a license to correct it or re-sign it, keeping its terms | Decided |
| [0035](0035-linked-add-on-aligns-to-base.md) | A linked add-on's expiry defaults to its base's expiry | Decided |
| [0036](0036-one-key-per-product.md) | One license key per product holds every purchase; a new purchase replaces the key | Decided |
| [0037](0037-purchases-in-a-license.md) | A license lists its purchases: one base, any number of add-ons | Decided |
| [0038](0038-licenses-for-one-product-combine.md) | Licenses for one product combine; there are no roles between keys | Decided |

## Template

```
# PDR-NNNN: <decision as a statement>

- **Status:** Decided | Dropped | Superseded by PDR-NNNN, <date> | Decided, <date>. Amended by PDR-NNNN, <date>: <what changed>
- **Source:** design.md <R-number or Q-number>, or the document that raised it
- **Serves:** <personas>; **Cost to:** <personas, if any>

## Decision
## Why
## Rejected
## Consequences
```

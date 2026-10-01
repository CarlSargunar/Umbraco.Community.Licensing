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
(R-number, Q-number) indexed in the *Requirements and open questions* section of
`openspec/changes/license-key-management/design.md`, or, for scope that left the change, in
[`../deferred-scope.md`](../deferred-scope.md). The chronological write-up is in git history. A decided PDR
is not yet a spec requirement until the specs are revised.

| PDR | Decision | Status |
|---|---|---|
| [0001](0001-primary-customer-is-the-site-owner.md) | The site owner is the primary customer | Decided |
| [0002](0002-shared-license-store.md) | One license store per site, shared by all vendors | Deferred (host-side) |
| [0003](0003-inventory-is-a-product-view.md) | The inventory has one row per product; packages register | Deferred (host-side) |
| [0004](0004-package-registration-declarations.md) | What a package declares when it registers | Deferred (host-side) |
| [0005](0005-core-signs-only.md) | The core issuing API signs only and keeps no records | Decided |
| [0006](0006-issuing-add-on-without-personal-data.md) | Optional issuing add-on; no customer personal data | Decided |
| [0007](0007-signing-secrets-stored-apart.md) | Signing secrets are stored apart from issued keys | Decided |
| [0008](0008-a-key-stands-for-one-purchase.md) | A license key stands for one purchase | Decided |
| [0009](0009-license-reference.md) | License reference: same reference supersedes, different ones combine | Decided |
| [0010](0010-product-features.md) | Product features are switches and numbers | Decided; amended by PDR-0018 |
| [0011](0011-base-and-add-on-licenses.md) | Base and add-on licenses, and how licenses combine | Decided; amended 2026-10-01 |
| [0012](0012-umbraco-version-range.md) | Umbraco version range: base licenses only, majors, inclusive | Deferred (host-side) |
| [0013](0013-feature-names.md) | Feature names are restricted; lookups ignore case | Decided |
| [0014](0014-dropped-kind-of-license.md) | No kind of license (trial / standard) | Dropped |
| [0015](0015-dropped-release-date-gating.md) | No release-date gating | Dropped |
| [0016](0016-dates-and-times.md) | Issue and expiry dates are UTC; the core sets the issue time | Decided; amended 2026-10-01 |
| [0017](0017-identifier-formats.md) | Identifier formats: `vendor.product` and `LIC-XXXXX-XXXXX` | Decided |
| [0018](0018-text-feature-values.md) | Product features may carry text; text never combines | Decided |

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

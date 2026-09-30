# Product decision records

Why the library behaves as it does. One file per decision: what was decided, why, what was
rejected and why, and who it serves. Written for vendors, implementors and site owners asking
"why does it work like this?", and as source material for the library's documentation.

Technology choices (algorithms, formats, packaging) are ADRs in [`../adrs/`](../adrs/). Product
decisions are PDRs here. Cite as PDR-0011, never a bare number.

Each record names its source: the exploration pass and question in
`openspec/changes/license-key-management/design.md` where the discussion is written up in full.
A decided PDR is not yet a spec requirement until the specs are revised.

| PDR | Decision | Status |
|---|---|---|
| [0001](0001-primary-customer-is-the-site-owner.md) | The site owner is the primary customer | Decided |
| [0002](0002-shared-license-store.md) | One license store per site, shared by all vendors | Decided |
| [0003](0003-inventory-is-a-product-view.md) | The inventory has one row per product; packages register | Decided |
| [0004](0004-package-registration-declarations.md) | What a package declares when it registers | Decided |
| [0005](0005-core-signs-only.md) | The core issuing API signs only and keeps no records | Decided |
| [0006](0006-issuing-add-on-without-personal-data.md) | Optional issuing add-on; no customer personal data | Decided |
| [0007](0007-signing-secrets-stored-apart.md) | Signing secrets are stored apart from issued keys | Decided |
| [0008](0008-a-key-stands-for-one-purchase.md) | A license key stands for one purchase | Decided |
| [0009](0009-license-reference.md) | License reference: same reference supersedes, different ones combine | Decided |
| [0010](0010-product-features.md) | Product features are switches and whole numbers | Decided |
| [0011](0011-base-and-add-on-licenses.md) | Base and add-on licenses, and how licenses combine | Decided |
| [0012](0012-umbraco-version-range.md) | Umbraco version range: base licenses only, majors, inclusive | Decided |
| [0013](0013-feature-names.md) | Feature names are restricted; lookups ignore case | Decided |
| [0014](0014-dropped-kind-of-license.md) | No kind of license (trial / standard) | Dropped |
| [0015](0015-dropped-release-date-gating.md) | No release-date gating | Dropped |

## Template

```
# PDR-NNNN: <decision as a statement>

- **Status:** Decided | Dropped | Superseded by PDR-NNNN, <date>
- **Source:** design.md <pass>, <question>
- **Serves:** <personas>; **Cost to:** <personas, if any>

## Decision
## Why
## Rejected
## Consequences
```

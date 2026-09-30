# Umbraco Licensing

A shared licensing library for paid Umbraco marketplace packages.

Umbraco package vendors currently have no common way to license their work: each vendor either
rolls an ad-hoc key scheme or ships unprotected. This library aims to provide one that any
package can adopt - a product-scoped license key that is **verifiable entirely offline** (no
phone-home, no licensing server), carrying an optional expiry and an optional supported Umbraco
core version range, with key rotation supported from the start.

Targets .NET 10, for consumption by Umbraco 17+ packages.

Built primarily for the **site owner** who buys licenses, with the vendor, implementor,
backoffice editor and site visitor also considered. See [`docs/personas.md`](docs/personas.md).

## Status

**Design in progress - no implementation yet.**

The active change, `license-key-management`, has a complete proposal, design, delta specs and
task breakdown, but no code. The specs cover a single license key. Exploration has since added
further requirements, none of them yet reflected in the proposal or the specs:

| Requirement | State |
|---|---|
| R1 named collection of license keys | open |
| R2 inventory reporting across the collection | open |
| R3 backoffice UI for license keys | open |
| R4 per-key renewal / upgrade link | open |
| R7 product features: switches and whole numbers, e.g. `max-orders: 500`; text dropped (design.md eighth pass) | decided; names lowercase `a-z`, digits, hyphens (design.md Q11) |
| R6 kind of license (trial / standard) | dropped |
| R8 release-date gating | dropped |
| Primary customer | decided: site owner |
| Shared key store across vendors (design.md Q2) | decided |
| Inventory lists products, not just keys (design.md Q4) | decided |
| R9 package self-registration | decided, design.md Q14 |
| Licenses combining: a key stands for one purchase (design.md Q13) | decided; entitlement keys a future feature |
| License reference: stable, non-secret ID; same ref supersedes, different refs combine | agreed, design.md seventh pass |
| How values combine across licenses: base / add-on role, switches OR, numbers sum, per-license filter (design.md Q16) | decided, design.md eighth pass |
| Umbraco range on base licenses only; expiry on base and add-ons | decided, design.md eighth pass |
| Umbraco range bounds: optional min / max major, inclusive (design.md Q12) | decided, design.md ninth pass |
| Dependencies between products, e.g. Shipping needs Commerce (design.md Q17) | open, parked |
| Core issuing API signs only, keeps no records | decided, design.md sixth pass |
| R10 optional issuing add-on for smaller vendors; no customer personal data (design.md Q15) | decided |
| Signing secrets stored apart from issued keys | decided, design.md sixth pass |

Open questions Q1, Q3 and Q5-Q8 must be settled before the specs are revised; Q17 is parked. They
are written up in nine exploration passes at the end of
[`openspec/changes/license-key-management/design.md`](openspec/changes/license-key-management/design.md),
which also records why R6 and R8 were dropped, and a comparison with the Standard.Licensing
library. The *"Where to resume"* list at the end of that file says what to pick up next.

Read those sections before assuming the current specs are settled.

## Not yet specified

Work that needs a change of its own, not yet explored or proposed:

- **Sample app for testing the library.** A host application for exercising the library by
  hand across scenarios (valid, expired, tampered, wrong product, wrong Umbraco version,
  missing, duplicate keys) and across multiple products with different features. Scope,
  shape and relationship to the automated tests are undecided.

## How this repository works

This project is specification-driven. **OpenSpec artifacts are the source of truth**, not the
code - work is specified, discussed and agreed before it is implemented.

```
  openspec/
    config.yaml                     project-wide OpenSpec configuration
    changes/<change-name>/
      proposal.md                   why, what changes, scope boundaries
      design.md                     decisions, alternatives, risks, open questions
      tasks.md                      verifiable implementation steps
      specs/<capability>/spec.md    requirements and scenarios (behaviour, not design)
  docs/personas.md                  who the library serves; primary customer
  docs/adrs/                        architecture decision records
```

A change moves through phases, each driven by a slash command:

| Phase | Command | What happens |
|---|---|---|
| Explore | `/opsx:explore` | Think through the problem. No code, no implementation. |
| Propose | `/opsx:propose` | Create the change and generate proposal, design, specs and tasks. |
| Apply | `/opsx:apply` | Implement the tasks. |
| Archive | `/opsx:archive` | Finalise the change once it has shipped. |

Conventions worth knowing before contributing:

- **Specs describe behaviour, not implementation.** A requirement states what the system SHALL
  do, with scenarios; how it is built belongs in `design.md` or an ADR.
- **Decisions are recorded, not just made.** Anything architecturally significant gets an ADR in
  `docs/adrs/`, cross-referenced from the design that made the call.
- **Requirements come before technology.** Exploration deliberately stays off the subject of
  frameworks and libraries so the problem is understood on its own terms first.

## Current scope

One active change, `license-key-management`, covering three capabilities:

| Capability | What it does |
|---|---|
| `license-generation` | Issuer-side: mint and sign a license key from its claims. Used by vendors, never shipped inside a licensed product. |
| `license-validation` | Consumer-side: verify a key's signature and evaluate its claims against the running product and Umbraco version. |
| `license-key-sourcing` | Supply the raw key to a host application from .NET configuration, an environment variable, or Azure Key Vault. |

Explicitly out of scope for this change: machine or domain binding, revocation before expiry,
and any online or network-based validation.

## Architecture decisions

- [ADR-0001](docs/adrs/0001-license-token-signing-algorithm.md) - license token signing algorithm and format (draft)
- [ADR-0002](docs/adrs/0002-package-split-for-keyvault-dependency.md) - splitting Azure Key Vault sourcing into a separate package (draft)

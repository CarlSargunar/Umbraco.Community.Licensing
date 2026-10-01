# Umbraco Licensing

A shared licensing library for paid Umbraco marketplace packages.

Umbraco package vendors currently have no common way to license their work: each vendor either
rolls an ad-hoc key scheme or ships unprotected. This library aims to provide one that any
package can adopt - a product-scoped license key that is **verifiable entirely offline** (no
phone-home, no licensing server), carrying an optional expiry and product features, with key
rotation supported from the start.

Targets .NET 10. The core library has no dependency on Umbraco; Umbraco integration is a later
change (see below).

Built primarily for the **site owner** who buys licenses, with the vendor, implementor,
backoffice editor and site visitor also considered. See [`docs/personas.md`](docs/personas.md).

## Status

**Design in progress - no implementation yet.**

The active change, `license-key-management`, has a proposal, design, delta specs and tasks, but
no code. **On 2026-10-01 its scope was cut** to a library for license key generation,
verification and signing-key management. Everything Umbraco-specific or host-side (key
sourcing, shared store, package registration, inventory, backoffice screen, Umbraco version
range) was removed and is recorded in [`docs/deferred-scope.md`](docs/deferred-scope.md) for
later changes.

The specs and tasks still describe a single license key and predate the exploration that
followed. That exploration settled the key's contents and the product behaviour, recorded as
PDR-0001 to PDR-0018 in [`docs/decisions/`](docs/decisions/README.md), with the key schema and
worked examples in [`docs/license-examples.md`](docs/license-examples.md).

What remains open is indexed in the *Exploration since the proposal* section of
[`openspec/changes/license-key-management/design.md`](openspec/changes/license-key-management/design.md):
what the result reports for an unverifiable key (Q5) and superseding edge cases (Q18). Its
*"Where to resume"* list says what to pick up next. The specs and tasks will be revised against
the PDRs once those are settled.

Read those before assuming the current specs are settled.

## Not yet specified

Work that needs a change of its own, not yet explored or proposed:

- **Umbraco integration.** Everything in [`docs/deferred-scope.md`](docs/deferred-scope.md):
  key sourcing, the shared store, package registration, the inventory, the backoffice screen
  and the Umbraco version range.
- **Issuing add-on.** Product and issued-key records for vendors without a shop system
  (PDR-0005 to PDR-0007).
- **Sample app for testing the library.** A host application for exercising the library by
  hand across scenarios (valid, expired, tampered, wrong product, missing, duplicate keys) and
  across multiple products with different features. Scope, shape and relationship to the
  automated tests are undecided.

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
  docs/license-examples.md          worked examples of license contents and evaluation
  docs/deferred-scope.md            Umbraco and host-side scope removed from the current change
  docs/adrs/                        architecture decision records (technology)
  docs/decisions/                   product decision records (behaviour, and why)
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
  `docs/adrs/`; product decisions get a PDR in `docs/decisions/` stating why and what was
  rejected. Both are cross-referenced from the design that made the call.
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

## Product decisions

Why the library behaves as it does: [`docs/decisions/`](docs/decisions/README.md) holds one record
per product decision (PDR-0001 to PDR-0017), with the reasons and the options rejected.

## Architecture decisions

- [ADR-0001](docs/adrs/0001-license-token-signing-algorithm.md) - license token signing algorithm and format (draft)
- [ADR-0002](docs/adrs/0002-package-split-for-keyvault-dependency.md) - splitting Azure Key Vault sourcing into a separate package (draft)

# Umbraco Licensing

A shared licensing library for paid Umbraco marketplace packages.

Umbraco package vendors currently have no common way to license their work: each vendor either
rolls an ad-hoc key scheme or ships unprotected. This library aims to provide one that any
package can adopt - a product-scoped license key that is **verifiable entirely offline** (no
phone-home, no licensing server), carrying an optional expiry and an optional supported Umbraco
core version range, with key rotation supported from the start.

Targets .NET 10, for consumption by Umbraco 17+ packages.

## Status

**Design in progress - no implementation yet.**

The active change, `license-key-management`, has a complete proposal, design, delta specs and
task breakdown, but zero code. The scope is also still moving: two requirements raised during
exploration - a **named collection** of license keys rather than a single key, and **inventory
reporting** across all of them - are not yet reflected in the proposal or the specs.

Five decisions must be settled before those specs are revised. They are written up with full
background in the *"Unresolved scope raised in exploration"* section of
[`openspec/changes/license-key-management/design.md`](openspec/changes/license-key-management/design.md).

Read that section before assuming the current specs are settled.

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

- [ADR-0001](docs/adrs/0001-license-token-signing-algorithm.md) - license token signing algorithm and format
- [ADR-0002](docs/adrs/0002-package-split-for-keyvault-dependency.md) - splitting Azure Key Vault sourcing into a separate package

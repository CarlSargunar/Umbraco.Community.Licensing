# Architecture decision records

How the library's behaviour is built: algorithms, formats, packaging, dependencies. Product
decisions (how the library behaves) are PDRs in [`../decisions/`](../decisions/README.md). The
test: if the stack changed, a PDR would still hold and an ADR would be reconsidered. Cite as
ADR-0001, never a bare number.

Each ADR's status line names the change and design.md section that raised it. **Deferred**
means the recommendation stands but its feature left the current change; see
[`../deferred-scope.md`](../deferred-scope.md).

| ADR | Decision | Status |
|---|---|---|
| [0001](0001-license-token-signing-algorithm.md) | License key signing algorithm and key string format | Decided; revised 2026-10-03 (twice), 2026-10-05 |
| [0002](0002-package-split-for-keyvault-dependency.md) | Split Azure Key Vault sourcing into a separate package | Deferred (key sourcing, `deferred-scope.md` D2) |
| [0003](0003-issuing-add-on-console-tool.md) | Issuing add-on as a .NET tool with Spectre.Console | Decided |
| [0004](0004-issuing-add-on-persistence.md) | Issuing add-on persistence: SQLite with EF Core, settings in a per-user file | Decided |
| [0005](0005-issuing-add-on-logging.md) | Issuing add-on logging with Microsoft.Extensions.Logging and Serilog | Decided |

## Template

```
# NNNN: <decision as a title>

## Status
Decided | Deferred | Superseded by ADR-NNNN, <date>. Revised <date>: <what changed>. Source: <design.md section>

## Context
## Decision
## Alternatives Considered
## Consequences
## Reversal Cost
```

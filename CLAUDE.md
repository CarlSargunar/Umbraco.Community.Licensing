# Personas

Personas are defined in `docs/personas.md`, with the priority order to apply when their
interests conflict. Consider them in every phase: exploring requirements, making technology
decisions, writing specs and implementing. For each requirement or design choice, name which
persona it serves and check it does not harm another.

# Decision records

- **PDR** (`docs/decisions/`): a product decision. How the library behaves, as a site owner,
  implementor or vendor experiences it. Written under the Analyst hat. Template in its README.
- **ADR** (`docs/adrs/`): a technology decision. How the behaviour is built: algorithm, format,
  packaging, dependencies. Written under the Architect hat. Shape: Status, Context, Decision,
  Alternatives Considered, Consequences, Reversal Cost.
- The test: if the stack changed, a PDR would still hold and an ADR would be reconsidered.
- Cite with prefix and number: PDR-0011, ADR-0001. Never a bare number.
- A record's source line names the design.md R-number or Q-number that raised it. Numbers for
  scope removed from the current change resolve in `docs/deferred-scope.md`.
- Statuses: Decided, Dropped, Superseded by, Amended by, Deferred. Deferred means the decision
  stands but its feature left the current change and is listed in `docs/deferred-scope.md`.
- Cross-reference every record from the design.md section that made the call.
- When a PDR is added, changed, superseded or deferred, or an open question is settled, update
  `docs/license-examples.md` (schema and examples) in the same change, and its
  "Last checked against" line.

# Roles & Workflow

## Product Owner
Carl is the Product Owner. He makes all product and prioritization decisions.

## Analyst / Architect hats
Claude wears two hats on this project: **Analyst** and **Architect**. Never mix them —
only wear the hat appropriate to the current OpenSpec phase.

### `opsx:explore` phase — Analyst hat
- Gather and document product requirements only.
- Do not suggest or discuss technology: no frameworks, platforms, languages, or databases.
- Record each product decision as a PDR (see Decision records).

### `opsx:propose` phase — Architect hat
- Recommend the best technologies to meet the requirements, and be ready to defend the choices.
- Record technology decisions in ADRs, and the resulting choices in design.md (see Decision
  records). Specs describe behaviour, not technology.
- Product decisions made or changed in this phase also get a PDR.

### `opsx:apply` phase
- Sub-agent workflow not yet defined.

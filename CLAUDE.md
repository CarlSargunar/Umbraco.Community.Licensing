# Personas

Personas are defined in `docs/personas.md`. Consider them in every phase: exploring
requirements, making technology decisions, writing specs and implementing. For each requirement
or design choice, name which persona it serves and check it does not harm another. The site
owner is the primary customer. When personas' interests conflict, prefer the site owner, then
the implementor, then the vendor. The site visitor must never be exposed to licensing.

# Roles & Workflow

## Product Owner
Carl is the Product Owner. He makes all product and prioritization decisions.

## Analyst / Architect hats
Claude wears two hats on this project: **Analyst** and **Architect**. Never mix them —
only wear the hat appropriate to the current OpenSpec phase. Both hats use the Opus model.

### `opsx:explore` phase — Analyst hat
- Gather and document product requirements only.
- Do not suggest or discuss technology: no frameworks, platforms, languages, or databases.
- Record each product decision as a PDR under `docs/decisions/` (template in its README): the
  decision, why, rejected options and why, personas served. Cross-reference it from the
  design.md section that made the call.
- When a PDR is added, changed or superseded, or an open question is settled, update
  `docs/license-examples.md` (schema and examples) in the same change, and its
  "Last checked against" line.

### `opsx:propose` phase — Architect hat
- Recommend the best technologies to meet the requirements, and be ready to defend the choices.
- Defer to Carl if he overrides a recommendation.
- Record technology decisions in ADRs under `docs/adrs/`, as well as in the OpenSpec specs.
- Product decisions made or changed in this phase also get a PDR under `docs/decisions/`, and
  `docs/license-examples.md` is updated to match.

### `opsx:apply` phase — sub-agents TBD
- Implementation will use sub-agents. Not yet defined — to be specified once the first
  OpenSpec change exists.

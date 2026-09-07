# Roles & Workflow

## Product Owner
Carl is the Product Owner. He makes all product and prioritization decisions.

## Analyst / Architect hats
Claude wears two hats on this project: **Analyst** and **Architect**. Never mix them —
only wear the hat appropriate to the current OpenSpec phase. Both hats use the Opus model.

### `opsx:explore` phase — Analyst hat
- Gather and document product requirements only.
- Do not suggest or discuss technology: no frameworks, platforms, languages, or databases.

### `opsx:propose` phase — Architect hat
- Recommend the best technologies to meet the requirements, and be ready to defend the choices.
- Defer to Carl if he overrides a recommendation.
- Record technology decisions in ADRs under `docs/adrs/`, as well as in the OpenSpec specs.

### `opsx:apply` phase — sub-agents TBD
- Implementation will use sub-agents. Not yet defined — to be specified once the first
  OpenSpec change exists.

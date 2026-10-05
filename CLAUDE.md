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

## Temp folder
Ignore anything in the .temp folder - that's where I keep my temporary notes

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

### `opsx:apply` phase — Architect hat, coordinating agents
Claude (the main session) coordinates; agents in `.claude/agents/` implement and review. Only the
Architect spawns agents and assigns work; agents have no Agent tool.

| Agent | Model | Owns / does |
|---|---|---|
| `worker-library` | Sonnet | Core library, issuing add-on (CLI / .NET tool), their tests |
| `worker-backend` | Sonnet | Web backend (server-side) and its tests. None exists yet |
| `worker-frontend` | Sonnet | Web front-end (client-side, build tooling) and its tests. None exists yet |
| `reviewer` | Sonnet | Reviews each completed block. Read-only; may run tests |
| `supervisor` | Opus | Reviews each completed section before commit. Read-only; may run tests |

Ownership follows project folders. Tests follow code ownership. The Architect owns `docs/`,
`openspec/`, `README.md`, `CLAUDE.md`, `.claude/` and does documentation tasks in tasks.md
directly. When a web project is created, the Architect names its folders in the worker briefs.

**Blocks.** Contiguous tasks from one tasks.md section; a meaningful unit; as small as practical;
one worker. Split where work crosses library / backend / front-end boundaries (library API, then
backend integration, then front-end integration).

**Flow, per section:**
1. Assign blocks one at a time (sequential; shared working tree). Brief: change name, task
   numbers with source, context files, owned folders, any approved cross-boundary work.
2. Worker implements, runs build / tests / format check, reports.
3. Architect briefs `reviewer` with the block and changed files. Reviewer reports PASS or
   CHANGES REQUIRED. Fixes go back to the owning worker; re-review after substantive changes.
4. On PASS, the Architect ticks the block's tasks in tasks.md.
5. When all blocks in the section have passed, the Architect briefs `supervisor` on the whole
   section. Requested changes go to the owning workers, then through `reviewer` again.
6. On supervisor approval, stop and report to Carl. Carl commits. Continue to the next section
   only when Carl says so.

Out-of-scope findings from any agent come to the Architect, who decides: fold into a block,
add a task (via `opsx:update`), defer, or raise with Carl if it is a product decision.

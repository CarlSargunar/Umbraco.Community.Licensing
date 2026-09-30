# PDR-0002: One license store per site, shared by all vendors

- **Status:** Decided, 2026-09-26
- **Source:** `openspec/changes/license-key-management/design.md` Q2 (first pass), decided in fifth pass
- **Serves:** site owner, implementor; **Cost to:** library maintainers (permanent contract)

## Decision

All license keys, from all vendors, go in one store per site.

## Why

- A site owner with keys from several vendors should not need their implementor to learn a
  different arrangement per package.
- A site-wide inventory is only possible over a shared store. With per-vendor stores each
  package sees only its own vendor's keys, and nobody can report "Commerce: no license found"
  except Commerce itself.

## Rejected

| Option | Why rejected |
|---|---|
| Per-vendor sections | No coordination between vendors, but the implementor maintains N lists, and no site-wide inventory is possible |

Not a cost, recorded so it is not weighed as one: "vendor A can read vendor B's keys". Any
package running in the site can already read the whole configuration.

## Consequences

The store's shape is a permanent contract between library versions. One site can run a package
built on library v1 and another on v2, both reading the same store, so the shape can only be
extended, never changed.

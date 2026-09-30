# PDR-0001: The site owner is the primary customer

- **Status:** Decided, 2026-09-26
- **Source:** [`docs/personas.md`](../personas.md)
- **Serves:** site owner

## Decision

The site owner, who buys licenses, is the primary customer. When personas' interests conflict,
the site owner wins, then the implementor, then the vendor. The site visitor never sees
licensing.

## Why

The site owner pays and gets most of the value from knowing what they hold (license collection,
inventory, backoffice view). A site typically runs several paid packages from different
vendors, so a design optimised for one vendor pushes cost onto every site owner.

## Consequences

- Drove PDR-0002 (shared store) and PDR-0003 (product view).
- Purchases can happen anywhere (vendor store, marketplace, reseller); the library assumes no
  purchase channel. Links shown to site owners must come from the vendor (PDR-0004).
- Site visitor rules: no key, status or licensing message reaches a public visitor; a licensing
  problem never crashes a page; what visitors experience on a failed license is the vendor's
  decision.

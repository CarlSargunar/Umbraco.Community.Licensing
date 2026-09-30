# PDR-0008: A license key stands for one purchase

- **Status:** Decided, 2026-09-28
- **Source:** `openspec/changes/license-key-management/design.md` Q13, decided in seventh pass
- **Serves:** site owner (buy from anywhere), vendor (sell add-ons and capacity separately)

## Decision

A key stands for what one purchase bought: a base license, an add-on, or extra capacity. A site
may hold several keys for one product, and they combine (PDR-0011). A key standing for the site
owner's whole current entitlement is a possible future feature.

## Why

- **Buy from anywhere.** Each purchase stands alone, so a marketplace or reseller can sell an
  add-on or capacity without knowing what the site owner already holds.
- **Add-ons can have their own term**, e.g. a monthly add-on on a yearly base. One key has one
  expiry, so an entitlement key cannot express this.
- **An entitlement key needs proof of ownership that nothing here provides.** Every reissue
  hands over the whole entitlement, so the vendor must confirm the buyer owns the license: via
  the current key (a bearer token, emailed around), a customer account (absent at marketplaces
  and resellers), or a contact for the original buyer (personal data, see PDR-0006). A
  non-secret identifier is not enough (PDR-0009).
- A per-purchase add-on key grants only the add-on, so quoting someone else's license reference
  gets a stranger only what they paid for.

## Rejected (for now)

| Option | Why rejected |
|---|---|
| One key per product holding the current entitlement | Above. Kept as a possible future feature; it needs no new library behaviour (a reissue under the base license's reference supersedes), only vendor-side proof of ownership |

## Consequences

- The library must combine keys for one product (PDR-0011).
- Add-ons that are separately installed packages, including third-party ones, are products of
  their own with their own keys.

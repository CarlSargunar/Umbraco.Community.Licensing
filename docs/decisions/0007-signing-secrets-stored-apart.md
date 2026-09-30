# PDR-0007: Signing secrets are stored apart from issued keys

- **Status:** Decided, 2026-09-28
- **Source:** `openspec/changes/license-key-management/design.md` R10
- **Serves:** vendor, site owner

## Decision

The issuing add-on stores products and issued keys, but not signing secrets. The secret is
supplied when a key is issued. The add-on's guidance tells the vendor to back up the secret.

## Why

A store holding both is one place from which every product's licenses can be forged. Separating
them later does not undo the exposure, because earlier backups and copies still hold the
secrets. Removing the exposure then means replacing the secret, and either old backups can
still forge keys, or every existing key stops working until reissued, at the site owner's cost.

| Start with | Change later | Cost |
|---|---|---|
| Stored together | Separate | Moving data is cheap; removing the exposure means replacing the secret |
| **Stored apart** | Offer "together" as an opt-in | Low; nothing newly exposed |

# PDR-0004: What a package declares when it registers

- **Status:** Decided, 2026-09-28
- **Source:** `openspec/changes/license-key-management/design.md` Q14 (fifth pass); Q8
- **Serves:** site owner

## Decision

| Declaration | Required | Why |
|---|---|---|
| Product ID | yes | So the product appears as a row even with no key |
| Display name | yes | Product IDs read badly to people |
| Vendor name | yes | "Who do I call?" |
| Renewal link | no, one link | The inventory is a site-level screen with no per-vendor hook; without a link a site owner sees an expiry with no way to act on it. Optional because a vendor may have no renewal page |
| License requirement: `required` or `optional` | yes | Changes only how a **missing** key is shown |

The renewal link must not contain the license key. The library stores and displays it;
everything past the link is the vendor's. The requirement flag enforces nothing: what the
product does without a key is the vendor's code, and with no key no features are granted.

## Why the requirement flag

Many Umbraco packages are free with paid extras. If every keyless package shows as a problem,
site owners learn to ignore real warnings. Once a key is present the flag is irrelevant: an
expired key for an optional product is still reported, because the site owner bought something.

## Rejected

| Option | Why rejected |
|---|---|
| Separate renewal and upgrade links | One link is enough; pricing and upgrades are the vendor's page |
| An "evaluation" requirement state | Needs an evaluation period and install date, and is close to the dropped kind of license (PDR-0014). A package with an evaluation period declares `required` |

Deferred, safe to add later: a list of features the package recognises (friendly names,
granted-but-unused, upsell). Vendors would have to keep it current per release. Registration
ships with each release, not in the key, so adding it never affects issued keys.

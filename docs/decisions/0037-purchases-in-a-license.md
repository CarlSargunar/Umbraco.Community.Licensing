# PDR-0037: A license lists its purchases: one base, any number of add-ons

- **Status:** Decided, 2026-10-05
- **Source:** `openspec/changes/one-key-per-product/design.md` Q2, Q3
- **Serves:** site owner (reads what they bought from the key), vendor (traces each purchase to
  its order), implementor (sees which purchase grants a feature)

## Decision

A license key holds 1..N purchases, in the order the vendor supplies them. Each purchase has:

| Field | Rules |
|---|---|
| kind | `base` or `add-on`. Required, no default. Exactly one `base` per key |
| name | The vendor's display name, e.g. `Commerce Pro`, `Extra 1,000 orders`. Required. 1 to 64 characters; no leading or trailing whitespace, line breaks or control characters. Never personal data (PDR-0006) |
| features | 0..N, rules of PDR-0010, PDR-0013, PDR-0018. A name at most once per purchase |
| vendor tag | Optional; rules of PDR-0022. Labels this purchase, e.g. its order number |
| purchased | A date (UTC). Required. Not after the current UTC date at issue. Reported only: no effect on validity, superseding or combining |

**Repeats.** Add-on purchases may repeat: two `Extra 1,000 orders` purchases are two entries.
The library compares no names.

**Features within a key.** The license's features combine its purchases' features by the rules
used between licenses (PDR-0038): a switch is granted when any purchase grants it, numbers are
summed exactly, equal text counts once. Text that differs between purchases, or text beside a
switch or number under one name, is rejected at issue: the vendor's mistake fails at the
vendor, not on the site.

```
  LIC-8F3AK-M7RXB  acme.commerce  expires 2027-03-01
    base    Commerce Pro         purchased 2026-03-01  tag SHOP-1001  ecommerce, max-orders 500
    add-on  Extra 1,000 orders   purchased 2026-06-15  tag SHOP-1187  max-orders 1000
    add-on  Extra 1,000 orders   purchased 2026-09-02  tag SHOP-1342  max-orders 1000
    add-on  AI Assist            purchased 2026-09-20  tag SHOP-1360  ai-assist
  license: ecommerce, ai-assist, max-orders 2500
```

**Reported.** A verified key's row reports every purchase with all its fields, as facts, and
the license's combined features. A failed key reports none of them (PDR-0019).

**Upgrades.** An upgrade replaces the base purchase. A downgrade or refund removes or replaces a
purchase. Both are reissues; the library does not tell them apart.

## Why

- **The list of purchases is the point.** A merged feature set answers "what am I licensed
  for" but not "what did I buy"; the site owner needs both, and the vendor needs each order
  number to find a purchase.
- **Kind kept per purchase.** A license with no base makes no commercial sense (PDR-0011's
  reasoning); requiring exactly one makes an add-on-only key fail at issue instead of on the
  site.
- **Name required.** The site owner reads purchases, not feature names; a purchase with no name
  is a list of switches.
- **Repeats allowed.** Capacity is sold in packs; buying a second pack is a normal purchase.
- **Purchase date.** The key's issue time changes at every reissue; the site owner needs when
  each thing was bought. Supplied by the vendor because a reissue carries earlier purchases.
- **Text conflict rejected at issue.** Within one key, every value comes from one vendor
  request, so a conflict is always a mistake and can be caught before the key exists.

## Rejected

| Option | Why rejected |
|---|---|
| One merged feature set, no purchase list | Cannot turn a license into its purchases |
| Name optional | A nameless purchase means nothing to the site owner |
| More than one base per key | Two bases in one license is a mistake the vendor can catch at issue; two bases bought separately are two licenses (PDR-0038) |
| Each add-on at most once | Needs an identity per add-on; a second capacity pack would replace the first |
| Purchase date set by the core | A reissue would re-date every earlier purchase |
| Text conflict within a key allowed, reported on the site | Waits months to show a mistake that is visible at issue |
| A purchase identifier the library generates | The vendor tag already traces a purchase; nothing in the library needs to address one |

## Consequences

- PDR-0010's "a name appears at most once in a key" reads "at most once in a purchase".
- PDR-0022's vendor tag moves from the key to the purchase.
- The key grows with each purchase. No limit is set; a key with dozens of purchases is a hint
  to replace them with one larger purchase.

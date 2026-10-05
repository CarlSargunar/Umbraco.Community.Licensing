# License examples

Worked examples of what license keys contain and how a site evaluates them, under the product
decisions in [`decisions/`](decisions/README.md).

> **Keep these examples up to date.** When a product decision (PDR) is added, changed or
> superseded, or an open question below is settled, update every example it affects in the
> same change, and adjust the "Last checked against" line. An example that contradicts a PDR is
> a documentation bug.
>
> Last checked against: PDR-0001 to PDR-0038 (2026-10-05). PDR-0036 to PDR-0038 (the
> `one-key-per-product` change) rewrote the schema and examples 1 to 20: one key per product
> holding its purchases; PDR-0008 and PDR-0011 are superseded; PDR-0009, PDR-0010, PDR-0018,
> PDR-0019, PDR-0021 and PDR-0022 are amended. PDR-0023 to PDR-0035 govern the issuing add-on
> and change no example; PDR-0024 to PDR-0027, PDR-0034 and PDR-0035 assumed per-purchase keys
> and are revisited when the `issuing-add-on` change resumes. The expiry is still optional in
> the library: PDR-0029 (stated as a date or perpetual) lands with the `issuing-add-on` change.

The Umbraco version range (PDR-0012) was removed from the schema on 2026-10-01
(`deferred-scope.md` D1). The inventory (D5) is also deferred: where an example says what a
site "shows", read it as what the library's evaluation result reports for that product.

**Rewritten on 2026-10-05** for one key per product (PDR-0036). Numbers and titles were kept
where the example still applies; examples 5, 8, 9, 11, 12 and 13 now show the purchase-list
equivalent of what they showed before. The per-purchase versions are in git history. Commits
before 2026-10-02, and the scope-cut record in `deferred-scope.md`, use older numbers:

```
  old  1-4  5  6  7   8   9   10       11  12  13  14  15  16  17  18  19
  new  1-4  8  9  10  11  12  removed  13  14  15  16  17  19  5   6   18
```

## Schema

The logical contents of a license key: what it holds and the rules checked when it is issued.
This is not the encoding. How contents are encoded and signed is a technology decision
(ADR-0001), which also adds technical fields not shown here, such as the signing key ID
used for rotation.

A key is one license for one product (PDR-0036). It holds the license's identity and expiry,
and 1..N purchases (PDR-0037).

| Field | Type | Required | Rules | Decision |
|---|---|---|---|---|
| `product` | product ID | yes | `vendor.product`: two dot-separated parts of lowercase `a-z`, digits, hyphens, each starting with a letter | PDR-0017 |
| `reference` | license reference | yes | 10 random characters from uppercase letters and digits without `0 O 1 I L`, shown `LIC-XXXXX-XXXXX`; matched ignoring case, hyphens, spaces. Generated at first issue, kept on every reissue, including each new purchase; not secret | PDR-0009, PDR-0017, PDR-0036 |
| `key part` | 4 random characters | yes | Same alphabet as the reference. Set by the core at every issue including a reissue, never a vendor input. Reference plus key part is the **key identifier**, `LIC-XXXXX-XXXXX-XXXX`, visible at the start of the key string | PDR-0020, PDR-0017 |
| `issued` | UTC date and time, to the second | yes | Set by the core at signing, never a vendor input. Among verified keys with the same product and reference, a key supersedes those issued strictly earlier | PDR-0016, PDR-0009 |
| `expires` | date | optional | Valid until the end of that date in UTC. Absent means never expires. Rejected if before the current UTC date. One expiry for the whole license: every purchase shares it | PDR-0016, PDR-0036 |
| `purchases` | 1..N purchases | yes | In the order supplied. Exactly one with kind `base` | PDR-0037 |

Purchase entries:

| Part | Type | Required | Rules | Decision |
|---|---|---|---|---|
| kind | `base` or `add-on` | yes | No default; no other values. Exactly one `base` per key | PDR-0037 |
| name | text | yes | 1 to 64 characters; no leading or trailing whitespace, line breaks or control characters. The vendor's display name. Names may repeat | PDR-0037 |
| purchased | date | yes | UTC date. Not after the current UTC date at issue. Reported only | PDR-0037 |
| vendor tag | text | optional | 1 to 64 characters from `A-Z a-z 0-9 - _ . # /`; no spaces; kept as supplied. The vendor's own label for this purchase, e.g. an order ID. Not unique; an invalid value is rejected, never cleaned. A label only. Reported for verified keys only. Never personal data | PDR-0022, PDR-0006, PDR-0037 |
| features | 0..N name / value pairs | optional | See below. A name at most once per purchase | PDR-0010, PDR-0013, PDR-0018, PDR-0037 |

Feature entries:

| Part | Rules | Decision |
|---|---|---|
| name | Lowercase `a-z`, digits, hyphens; starts with a letter. At most once per purchase. Looked up ignoring case | PDR-0013, PDR-0010, PDR-0037 |
| value | Switch (present = granted; a plain name), number or text. The type is what the vendor issues; text is never read as a number | PDR-0010, PDR-0018 |
| number | Zero or positive; at most 4 decimal places and 15 digits; an additive quantity (a count, limit or amount), never a rate or factor. Summed across purchases and across licenses | PDR-0010 |
| text | Non-empty; at most 256 characters; no leading or trailing whitespace, line breaks or control characters. Never combined: equal values (exact, case-sensitive) count as one. Within one key, different values or text beside a switch or number under one name are rejected at issue; between keys they are a conflict | PDR-0018, PDR-0037 |
| not allowed | Explicit `false`; negative or malformed numbers; empty or padded text | PDR-0010, PDR-0018 |

**The license's features** combine its purchases' features: a switch is granted when any
purchase grants it, numbers are summed, equal text counts once (PDR-0037). Licenses for one
product combine the same way (PDR-0038).

Never in a key: customer name, email, company or any other personal data (PDR-0006), including
in a purchase name or vendor tag (PDR-0022, PDR-0037).

```
  LICENSE KEY
  product      required
  reference    required, kept across every reissue
  key part     set by the core
  issued       set by the core
  expires      date or never; shared by every purchase
  purchases    1..N
    kind         base (exactly one) | add-on (any number, may repeat)
    name         required
    purchased    required date
    vendor tag   optional
    features     0..N
  licenses the product on its own
```

## Notation

Examples show a key's **contents** using the schema above, not the key string. Vendors,
products, references and dates are illustrative.

```
  product    acme.commerce
  reference  LIC-8F3AK-M7RXB
  issued     2026-03-01T09:14Z
  expires    2027-03-01          valid until 2027-03-01 23:59:59 UTC
  purchases
    base    Commerce Standard    purchased 2026-03-01
            ecommerce            switch
            max-orders: 500      number
```

A purchase is written `kind  name  purchased <date>  [tag <vendor tag>]`, with its features
below or after it. Text values (`licensed-domain: example.com`) are shown in examples 16 and 17.

The key string itself starts with the key identifier, then the opaque part (PDR-0020):

```
  LIC-8F3AK-M7RXB-7Q2D . q83KdL...............Zx9PzQ
  reference       key     opaque part (illustrative)
                  part
```

In site evaluations, keys are shortened to reference, the purchases and the fields that matter.
An evaluation result names each key by its full key identifier; the key part is left out of
the examples except where two keys share a reference and it matters (examples 5, 7, 8 and 18).
Issue times are recorded to the second (PDR-0016); the examples show minutes except where
seconds matter (example 7).
Site evaluations assume **now is 2026-10-01** unless stated.

## 1. Minimal license

```
  product    acme.seo-toolkit
  reference  LIC-4HN7T-QW2ZC
  issued     2026-01-10T14:02Z
  purchases
    base  SEO Toolkit  purchased 2026-01-10
```

No expiry: never expires. One purchase with no features. Valid. The package asks "is
acme.seo-toolkit licensed?" and gets yes; "is feature X granted?" is always no (PDR-0010).

## 2. Typical license

```
  product    acme.commerce
  reference  LIC-8F3AK-M7RXB
  issued     2026-03-01T09:14Z
  expires    2027-03-01
  purchases
    base  Commerce Standard  purchased 2026-03-01
          ecommerce
          max-orders: 500
```

Valid until 2027-03-01 23:59:59 UTC. A host showing this to a site owner in Seattle would say
"expires 1 Mar 2027, 15:59 your time" (PDR-0016; display is deferred, D5).

## 3. Renewal: same reference supersedes

The implementor pastes the renewed key and leaves the old one in place.

```
  LIC-8F3AK-M7RXB  issued 2025-03-01T10:02Z  expires 2026-03-01  base Commerce Standard, max-orders 500  <- superseded
  LIC-8F3AK-M7RXB  issued 2026-03-01T09:14Z  expires 2027-03-01  base Commerce Standard, max-orders 500  <- counts
```

Result: `max-orders: 500`, not 1000. The old key is shown as *superseded*, not as a conflict
(PDR-0009). A renewal keeps every purchase; only the expiry moves (PDR-0036).

## 4. Same-day correction

The vendor issues a typo, then reissues under the same reference an hour later:

```
  LIC-8F3AK-M7RXB  issued 2026-03-01T09:14Z  base Commerce Standard, max-orders: 50    <- superseded
  LIC-8F3AK-M7RXB  issued 2026-03-01T10:21Z  base Commerce Standard, max-orders: 500   <- counts
```

The core sets `issued` to the moment of signing, so the correction always wins (PDR-0016).

## 5. Buying add-ons: each purchase replaces the key

The site owner buys Commerce Standard, then two add-ons. Each purchase reissues the license
under `LIC-8F3AK-M7RXB` with every purchase so far (PDR-0036):

```
  key 1  LIC-8F3AK-M7RXB-7Q2D  issued 2026-03-01T09:14Z  expires 2027-03-01
    base    Commerce Standard    purchased 2026-03-01  tag SHOP-1001  ecommerce, max-orders 500

  key 2  LIC-8F3AK-M7RXB-K9PX  issued 2026-06-15T16:40Z  expires 2027-03-01
    base    Commerce Standard    purchased 2026-03-01  tag SHOP-1001  ecommerce, max-orders 500
    add-on  Extra 1,000 orders   purchased 2026-06-15  tag SHOP-1187  max-orders 1000

  key 3  LIC-8F3AK-M7RXB-H8RC  issued 2026-09-01T08:05Z  expires 2027-03-01
    base    Commerce Standard    purchased 2026-03-01  tag SHOP-1001  ecommerce, max-orders 500
    add-on  Extra 1,000 orders   purchased 2026-06-15  tag SHOP-1187  max-orders 1000
    add-on  AI Assist            purchased 2026-09-01  tag SHOP-1342  ai-assist
```

The vendor built keys 2 and 3 from the key the site owner presented: the library read its
purchases and expiry and the vendor appended the new purchase. The add-ons expire with the
license on 2027-03-01, whenever they were bought.

The implementor replaced the key in production but missed staging, which holds keys 1 and 3:

```
  LIC-8F3AK-M7RXB-7Q2D  issued 2026-03-01T09:14Z   superseded
  LIC-8F3AK-M7RXB-H8RC  issued 2026-09-01T08:05Z   valid
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   ecommerce, ai-assist, max-orders 1500
```

## 6. Which keys may supersede

Only a verified key for the product being evaluated supersedes (PDR-0009).

**A key that fails verification never supersedes.** The site owner buys an upgrade, reissued
under the same reference, and the new key is mangled when pasted:

```
  LIC-8F3AK-M7RXB  issued 2026-03-01T09:14Z  max-orders 500    valid
  claims to be LIC-8F3AK-M7RXB                                 not verified   <- does not supersede
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   max-orders 500
```

The working key keeps counting. The failed key reports its reason and claimed identifiers
only, never its claimed issue time, expiry or purchases (PDR-0019; example 18).

**A key for another product takes no part.** Two vendors happen to generate the same
reference. Evaluating acme.commerce:

```
  LIC-8F3AK-M7RXB  acme.commerce  issued 2026-03-01T09:14Z   valid
  claims to be LIC-8F3AK-M7RXB for zenith.forms              wrong product   <- does not supersede
  ---------------------------------------------------------------------------------
  acme.commerce  licensed
```

**An expired key still supersedes.** The vendor types `expires 2099-03-01`, then reissues with
the six-month term that was sold:

```
  LIC-8F3AK-M7RXB  issued 2026-03-01T09:14Z  expires 2099-03-01   superseded
  LIC-8F3AK-M7RXB  issued 2026-03-01T10:21Z  expires 2026-09-01   expired
  ---------------------------------------------------------------------------------
  acme.commerce  NOT licensed   no features
```

The correction does not undo itself on the day it expires: the older key stays superseded.

## 7. The same key twice, and keys issued at the same time

What happens when no key under a reference is later than another (PDR-0009). The site holds
this license:

```
  LIC-8F3AK-M7RXB-7Q2D  issued 2026-03-01T09:14:07Z  base Commerce Standard, max-orders 500
```

**The same key supplied twice**, here in a settings file and in an environment variable. It
counts once:

```
  1  LIC-8F3AK-M7RXB-7Q2D  max-orders 500   valid
  2  LIC-8F3AK-M7RXB-7Q2D  max-orders 500   duplicate of row 1
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   max-orders 500
```

Nobody's error, and nothing to fix. The implementor may remove either copy.

**Different keys tied for latest: both count.** On 2026-09-28 the site owner upgrades to
Commerce Pro (2000 orders). The vendor's system submits the reissue twice in the same second,
and both keys reach the site:

```
  1  LIC-8F3AK-M7RXB-7Q2D  issued 2026-03-01T09:14:07Z  max-orders 500    superseded
  2  LIC-8F3AK-M7RXB-D4WK  issued 2026-09-28T14:30:22Z  max-orders 2000   valid
        vendor error: same issue time as LIC-8F3AK-M7RXB-K9PX
  3  LIC-8F3AK-M7RXB-K9PX  issued 2026-09-28T14:30:22Z  max-orders 2000   valid
        vendor error: same issue time as LIC-8F3AK-M7RXB-D4WK
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   max-orders 4000      1 license counted
```

The two keys are versions of one license and the library cannot tell which is current. Both
count, so the site owner never has less than they paid for and the site keeps operating. The
extra 2000 is the vendor's cost for the vendor's mistake.

**Tied keys that disagree.** The vendor's shop issues the upgrade while a renewal job reissues
the license from stale data, in the same second:

```
  2  LIC-8F3AK-M7RXB-T6WN  issued 2026-09-28T14:30:22Z  max-orders 2000   valid   vendor error
  3  LIC-8F3AK-M7RXB-X3BN  issued 2026-09-28T14:30:22Z  max-orders 500    valid   vendor error
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   max-orders 2500
```

Had the two keys carried different text for one feature, that feature would answer nothing
until the vendor reissued (PDR-0018; example 17).

**The fix.** The vendor reissues once more. The new key is later than both and supersedes
them:

```
  1  LIC-8F3AK-M7RXB-7Q2D  issued 2026-03-01T09:14:07Z  max-orders 500    superseded
  2  LIC-8F3AK-M7RXB-T6WN  issued 2026-09-28T14:30:22Z  max-orders 2000   superseded
  3  LIC-8F3AK-M7RXB-X3BN  issued 2026-09-28T14:30:22Z  max-orders 500    superseded
  4  LIC-8F3AK-M7RXB-H8RC  issued 2026-09-29T08:02:11Z  max-orders 2000   valid
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   max-orders 2000
```

The vendor error flag applies only while keys are tied for latest, so it is gone.

## 8. Upgrade: the base purchase is replaced

The license from example 5 (key 3) upgrades from Standard to Pro on 2026-09-20. The vendor
reissues with the base purchase replaced and the add-ons kept:

```
  LIC-8F3AK-M7RXB-W2MZ  issued 2026-09-20T11:02Z  expires 2027-03-01
    base    Commerce Pro         purchased 2026-09-20  tag SHOP-1360  ecommerce, max-orders 2000
    add-on  Extra 1,000 orders   purchased 2026-06-15  tag SHOP-1187  max-orders 1000
    add-on  AI Assist            purchased 2026-09-01  tag SHOP-1342  ai-assist
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   ecommerce, ai-assist, max-orders 3000
```

The library does not tell an upgrade from any other reissue; the new purchase list is what the
site owner sees. Whether the Pro purchase keeps the original purchase date is the vendor's
choice.

## 9. Combined: the purchases of one license

A second capacity pack bought on 2026-09-25 for the license in example 8. Add-ons may repeat
(PDR-0037):

```
  LIC-8F3AK-M7RXB-N5QA  issued 2026-09-25T10:00Z  expires 2027-03-01
    base    Commerce Pro         purchased 2026-09-20  ecommerce, max-orders 2000
    add-on  Extra 1,000 orders   purchased 2026-06-15  max-orders 1000
    add-on  AI Assist            purchased 2026-09-01  ai-assist
    add-on  Extra 1,000 orders   purchased 2026-09-25  max-orders 1000
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   ecommerce, ai-assist, max-orders 4000
```

Switches: granted if any purchase grants them. Numbers: summed, base included (PDR-0037).

## 10. Decimal quantities

```
  LIC-9QW3E-RT5YU  acme.media-vault
    base    Media Vault        storage-gb: 10
    add-on  Extra 2.5 GB       storage-gb: 2.5
    add-on  Extra 2.5 GB       storage-gb: 2.5
  ---------------------------------------------------------------------------------
  acme.media-vault  licensed   storage-gb 15
```

Sums are exact at the precision written (PDR-0010). A rate such as `discount-rate: 0.15` does
not belong in a key: two purchases would sum it to 0.30.

## 11. An add-on is not renewed

The license in example 9 is renewed on 2027-02-15. The site owner drops AI Assist. The vendor
reissues with a new expiry and without that purchase:

```
  LIC-8F3AK-M7RXB-R7TB  issued 2027-02-15T09:30Z  expires 2028-03-01
    base    Commerce Pro         purchased 2026-09-20  ecommerce, max-orders 2000
    add-on  Extra 1,000 orders   purchased 2026-06-15  max-orders 1000
    add-on  Extra 1,000 orders   purchased 2026-09-25  max-orders 1000
```

On **2027-03-02**, with the implementor having installed the renewal:

```
  LIC-8F3AK-M7RXB-N5QA  superseded
  LIC-8F3AK-M7RXB-R7TB  valid
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   ecommerce, max-orders 4000        (ai-assist dropped)
```

An add-on cannot lapse on its own: it ends when a reissue leaves it out, or when the license
expires (PDR-0036).

## 12. The license expires

Example 9's license on **2027-03-02**, not renewed:

```
  LIC-8F3AK-M7RXB-N5QA  expired   (all four purchases)
  ---------------------------------------------------------------------------------
  acme.commerce  NOT licensed   no features
```

Every purchase shares the license's expiry, so no add-on outlives its base and nothing is
*inactive*. When the license is renewed under `LIC-8F3AK-M7RXB`, every purchase in the renewed
key counts again (PDR-0036).

## 13. Two licenses bought by mistake

The site owner buys Commerce Standard twice, under two references:

```
  LIC-8F3AK-M7RXB  base Commerce Standard, ecommerce, max-orders 500   valid
  LIC-C2D8R-XE6GU  base Commerce Standard, ecommerce, max-orders 500   valid
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   ecommerce, max-orders 1000      2 licenses counted
```

Both count; "2 licenses counted" prompts the site owner to ask the vendor about a refund, or to have both
merged into one key (PDR-0038). An add-on must be a reissue under `LIC-8F3AK-M7RXB`, or it is
a second license that stacks the same way.

## 14. Trial

There is no trial kind (PDR-0014). A trial is a license with a short expiry, optionally with a
feature the vendor checks by convention:

```
  product    acme.commerce
  reference  LIC-7B2SW-NF5HA
  issued     2026-09-20T11:30Z
  expires    2026-10-20
  purchases
    base  Commerce Trial  purchased 2026-09-20
          ecommerce
          trial
```

Converting the trial is a reissue under `LIC-7B2SW-NF5HA` with a paid base purchase and a new
expiry.

## 15. Add-on product (a separate package)

A shipping extension installed as its own package is its own product with its own key, not a
purchase in acme.commerce's license, even from a third-party vendor:

```
  product    zenith.commerce-shipping
  reference  LIC-5E3FT-BPZ7Y
  issued     2026-05-01T12:00Z
  expires    2027-05-01
  purchases
    base  Commerce Shipping  purchased 2026-05-01
          shipping-rates
```

It never combines with acme.commerce's licenses. Whether it should be shown as inactive when
acme.commerce is not licensed is deferred (`deferred-scope.md` D9).

## 16. Text features: domain and tenant

Text carries what a switch cannot name in advance (PDR-0018). Here a vendor binds a license to
a site by convention and identifies the customer's account on its own service:

```
  product    acme.search-cloud
  reference  LIC-3TQ7A-YE9DR
  issued     2026-04-12T10:30Z
  expires    2027-04-12
  purchases
    base  Search Cloud  purchased 2026-04-12
          licensed-domain: example.com
          tenant: t-8c21f
          max-documents: 50000
```

The library reports `licensed-domain` as `example.com`. Whether the package compares it with
the site's hostnames, and what it does on a mismatch, is the vendor's decision. Keys stay
bearer tokens.

## 17. Text across purchases and across licenses

**Within one key: equal counts once, different is rejected at issue.** An add-on purchase for
example 16 that repeats the domain:

```
  LIC-3TQ7A-YE9DR
    base    Search Cloud         licensed-domain example.com, tenant t-8c21f, max-documents 50000
    add-on  Extra 25k documents  licensed-domain example.com, max-documents 25000
  ---------------------------------------------------------------------------------------------
  acme.search-cloud  licensed   licensed-domain example.com, tenant t-8c21f, max-documents 75000
```

The same add-on with a typo, `licensed-domain exmaple.com`, is rejected when the vendor issues
the key: within one key every value comes from one request, so a conflict is always a mistake
(PDR-0037). No key is produced.

**Between keys: different conflicts.** The site owner also holds a second license bought
through a reseller with the typo:

```
  LIC-3TQ7A-YE9DR  licensed-domain example.com, tenant t-8c21f, max-documents 75000   valid    conflict: licensed-domain
  LIC-K6W2P-D4ZHN  licensed-domain exmaple.com, max-documents 25000                   valid    conflict: licensed-domain
  ---------------------------------------------------------------------------------------------
  acme.search-cloud  licensed   tenant t-8c21f, max-documents 100000      2 licenses counted
                                licensed-domain: CONFLICT (2 licenses disagree)
```

Both licenses stay valid and the numbers still sum; only `licensed-domain` answers nothing
until the vendor reissues `LIC-K6W2P-D4ZHN`, or merges it into `LIC-3TQ7A-YE9DR`. Comparison is
exact: `Example.com` would conflict the same way, as would `licensed-domain: 500` (text)
alongside a number under that name (PDR-0018).

## 18. Keys that fail verification

What the result reports for a failed key, and how each key is named (PDR-0019, PDR-0020). A
site runs acme.commerce. The implementor supplied six keys:

| # | What happened |
|---|---|
| 1 | The license, pasted correctly |
| 2 | The same license, reissued on 2026-09-28 with a capacity pack added. A character was lost in the paste |
| 3 | The zenith.commerce-shipping key from example 15, pasted here by mistake |
| 4 | A second license bought by mistake, cut off at the end by an email client |
| 5 | A reissue signed with the vendor's new signing key. The site runs an older acme.commerce that does not know it |
| 6 | A key someone edited to say `expires 2099-12-31` and `max-orders 999999`, under the license's reference |

The result, one row per supplied key, in the order supplied:

```
  VERIFIED: claims are facts, reported in full
  1  LIC-8F3AK-M7RXB-7Q2D  valid   expires 2027-03-01
       base  Commerce Standard  purchased 2026-03-01  ecommerce, max-orders 500

  FAILED: reason, plus claimed product and identifier only
  2  not verified                 claims to be LIC-8F3AK-M7RXB-K9PX for acme.commerce
  3  wrong product                claims to be LIC-5E3FT-BPZ7Y-2MHS for zenith.commerce-shipping
  4  unreadable                   claims to be LIC-2V5C9-HKD3P-X3BN
  5  signing key not recognised   claims to be LIC-8F3AK-M7RXB-H8RC for acme.commerce
  6  not verified                 claims to be LIC-8F3AK-M7RXB-P6TY for acme.commerce
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   ecommerce, max-orders 500
```

- **Never reported**: the issue time, expiry and purchases the failed keys claim. Key 6's
  `expires 2099-12-31` and `max-orders 999999` appear nowhere. Neither does key 5's capacity
  pack: the site owner sees `LIC-8F3AK-M7RXB-H8RC` is not in effect and matches it to their
  purchase.
- **Key 4 has a name** because its start survived the cut. The site owner can see which
  license is broken. A string with no readable identifier, such as a pasted password, would be
  "the fourth key" and nothing more.
- **Keys 1, 2, 5 and 6 share a reference.** The key part (`7Q2D`, `K9PX`, `H8RC`, `P6TY`) tells
  the implementor which string to paste again. Searching the site's settings for
  `LIC-8F3AK-M7RXB-K9PX` finds it.
- **Keys 2 and 6 are both *not verified*.** The library cannot tell a paste error from an edit,
  and names neither as tampering. Neither supersedes the verified key that shares its
  reference (example 6), so key 1 keeps counting.
- **Key 3 is *wrong product*, not *signing key not recognised*.** The claimed product is
  checked first, so a mis-paste is reported as one.
- **First action per reason**: unreadable or not verified, paste again; wrong product, move the
  key; signing key not recognised, update the product, then ask the vendor (PDR-0019).
- **Whitespace is not a failure.** Had key 1 been wrapped across two lines by an email client,
  or carried a trailing newline from a settings file, it would still be valid: all whitespace
  is removed before reading (PDR-0021).
- **A key that verifies but breaks the schema is *unreadable*.** A key signed by the vendor's
  trusted signing key with `max-orders: -200`, or with no base purchase, which the core refuses
  at issue (example 19), can only come from a faulty vendor tool. It is reported as unreadable
  with its identifier and grants nothing. Pasting it again does not help; the vendor must
  reissue (PDR-0021).

## 19. Rejected when issued

Each of these fails at the vendor; no key is produced. Assumes issuing on 2026-10-01.

| Input | Reason | Decision |
|---|---|---|
| no purchases | A license holds at least its base purchase | PDR-0037 |
| only add-on purchases | Exactly one base purchase | PDR-0037 |
| two base purchases | Exactly one base purchase | PDR-0037 |
| a purchase with no kind | Kind is required; no default | PDR-0037 |
| a purchase with no name, or name `" Pro"` | Name required; no leading or trailing whitespace | PDR-0037 |
| a name longer than 64 characters | Keys must stay pasteable | PDR-0037 |
| a purchase with no purchase date | Purchase date is required | PDR-0037 |
| `purchased 2026-10-02` | After the current UTC date | PDR-0037 |
| `licensed-domain: example.com` on the base, `licensed-domain: exmaple.com` on an add-on | Text that differs between purchases in one key | PDR-0037, PDR-0018 |
| `max-orders: 500` on the base, `max-orders: "500"` (text) on an add-on | Text beside a number under one name in one key | PDR-0037, PDR-0018 |
| `product commerce` | Product ID must be `vendor.product` | PDR-0017 |
| `product Acme.Commerce` | Product ID is lowercase | PDR-0017 |
| `issued` supplied by the vendor | Set by the core at signing | PDR-0016 |
| `key part` supplied by the vendor | Set by the core at every issue | PDR-0020 |
| `expires 2026-09-30` | Before the current UTC date | PDR-0016 |
| `pro: false` | Explicit `false` is invalid; absence means not granted | PDR-0010 |
| `licensed-domain: ""` | Text is non-empty; a switch is a plain name | PDR-0018 |
| `licensed-domain: " example.com"` | No leading or trailing whitespace in text | PDR-0018 |
| `notes: "line one` ⏎ `line two"` | No line breaks or control characters in text | PDR-0018 |
| text longer than 256 characters | Keys must stay pasteable | PDR-0018 |
| `max-orders: "5OO"` | Malformed number | PDR-0010 |
| `max-orders: -200` | Numbers are zero or positive | PDR-0010 |
| `storage-gb: 2.12345` | More than 4 decimal places | PDR-0010 |
| `max-orders: 500` and `max-orders: 1000` in one purchase | A name appears at most once per purchase | PDR-0010, PDR-0037 |
| `Max Orders: 500` | Names are lowercase `a-z`, digits, hyphens, starting with a letter | PDR-0013 |
| `vendor tag "INV 2026 04"` | No spaces in a vendor tag | PDR-0022 |
| `vendor tag "jane@acme.com"` | `@` is not allowed in a vendor tag | PDR-0022 |
| `vendor tag ""` | A vendor tag is not empty; omit it instead | PDR-0022 |
| vendor tag longer than 64 characters | Keys must stay pasteable | PDR-0022 |
| a customer's name or email anywhere in the key | No personal data in keys | PDR-0006 |

`expires 2026-10-01` and `purchased 2026-10-01` (today) are allowed. `max-orders: 500` on the
base and `max-orders: 1000` on an add-on is allowed: different purchases, summed.

## 20. Vendor tag

The vendor's shop issues each key on purchase and signs each purchase's order number into it
(PDR-0022, PDR-0037). The 2025 license was renewed in 2026 under the same reference; one 2026
order bought both the renewal and a capacity pack.

```
  product     acme.commerce
  reference   LIC-8F3AK-M7RXB
  issued      2026-03-01T09:14Z
  expires     2027-03-01
  purchases
    base    Commerce Standard    purchased 2025-03-01  tag SHOP-2025-000087  ecommerce, max-orders 500
    add-on  Extra 1,000 orders   purchased 2026-03-01  tag SHOP-2026-000123  max-orders 1000
```

The site still holds the 2025 key and a third key that fails verification. The result:

```
  KEY                    STATE        PURCHASES (VENDOR TAG)
  LIC-8F3AK-M7RXB-7Q2D   valid        Commerce Standard (SHOP-2025-000087),
                                      Extra 1,000 orders (SHOP-2026-000123)
  LIC-8F3AK-M7RXB-4TQ9   superseded   Commerce Standard (SHOP-2025-000087)
  LIC-2PW9H-KD4NZ-X3MF   not verified (nothing reported)
```

- **Each purchase keeps its own tag.** The base still carries the 2025 order: the reissue was
  prefilled from the current key, which kept the earlier purchase with its tag (PDR-0022, as
  amended). Where the vendor records the renewal order is its own convention.
- **Tags are not unique** and the library never compares them.
- **The failed key reports no tag.** Its tags are unverified claims and could be anything; the
  vendor traces it by its key identifier.
- **Support without the key.** The site owner quotes `SHOP-2026-000123` from the result or
  their receipt; the vendor finds the order in its shop.

## Open questions that may change these examples

No product question is open in `one-key-per-product` design.md.

Deferred questions that could also change these examples when they return (`deferred-scope.md`):
Q1 site label (a label per stored key), Q3 routing (how keys in examples 3, 5 and 13 reach
acme.commerce), Q17 product dependencies (example 15), and the Umbraco version range (D1).

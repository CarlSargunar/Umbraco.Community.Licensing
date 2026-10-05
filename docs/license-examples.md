# License examples

Worked examples of what license keys contain and how a site evaluates them, under the product
decisions in [`decisions/`](decisions/README.md).

> **Keep these examples up to date.** When a product decision (PDR) is added, changed or
> superseded, or an open question below is settled, update every example it affects in the
> same change, and adjust the "Last checked against" line. An example that contradicts a PDR is
> a documentation bug.
>
> Last checked against: PDR-0001 to PDR-0022, including the amendments to PDR-0009, PDR-0011,
> PDR-0016 and PDR-0017 that settled design.md Q18, Q5 and Q19, PDR-0021 (design.md Q20),
> PDR-0022 and its amendments to PDR-0006 and PDR-0017 (design.md Q21), and the scope cut in
> [`deferred-scope.md`](deferred-scope.md) (2026-10-03). Then PDR-0023 to PDR-0030 from the
> `issuing-add-on` change, including PDR-0027's amendment to PDR-0006 and the amendments to
> PDR-0023 to PDR-0026 and PDR-0028 (issuing-add-on design.md Q10 to Q18); PDR-0023 to PDR-0028
> and PDR-0030 govern the add-on and change no example (2026-10-05). PDR-0031 (issuing-add-on
> design.md Q19, two-step rotation) changes no example; example 18 key 6 is the failure it
> prevents. PDR-0032 (design.md Q20, one location per machine) and its amendments to PDR-0023
> and PDR-0028 change no example. PDR-0033 (design.md Q21, retired signing key dependants) and
> its amendment to PDR-0031 change no example. PDR-0034 (design.md Q22, reissue) and its
> amendment to PDR-0026 change no example; examples 4 and 5 are the corrections it issues.
> The PDR-0025 month-end amendment (design.md Q23) is an add-on rule and changes no example.
> PDR-0035 (design.md Q24, linked add-on aligns to base) changes no example (2026-10-05).

The Umbraco version range (PDR-0012) was removed from the schema on 2026-10-01
(`deferred-scope.md` D1). The inventory (D5) is also deferred: where an example says what a
site "shows", read it as what the library's evaluation result reports for that product.

**Renumbered on 2026-10-02** so related examples sit together. Commits before that date, and
the scope-cut record in `deferred-scope.md`, use the old numbers:

```
  old  1-4  5  6  7   8   9   10       11  12  13  14  15  16  17  18  19
  new  1-4  8  9  10  11  12  removed  13  14  15  16  17  19  5   6   18
```

New number 7 was added on the same day. Old number 10 was the Umbraco major upgrade, removed
with D1; its text is in git history (commit `fcda40b`).

## Schema

The logical contents of a license key: what it holds and the rules checked when it is issued.
This is not the encoding. How contents are encoded and signed is a technology decision
(ADR-0001), which also adds technical fields not shown here, such as the signing key ID
used for rotation.

| Field | Type | Base | Add-on | Rules | Decision |
|---|---|---|---|---|---|
| `product` | product ID | required | required | `vendor.product`: two dot-separated parts of lowercase `a-z`, digits, hyphens, each starting with a letter. An add-on carries the base product's ID | PDR-0017, PDR-0011 |
| `role` | `base` or `add-on` | required | required | No default; no other values | PDR-0011 |
| `reference` | license reference | required | required | 10 random characters from uppercase letters and digits without `0 O 1 I L`, shown `LIC-XXXXX-XXXXX`; matched ignoring case, hyphens, spaces. Generated at first issue, kept on reissue; not secret | PDR-0009, PDR-0017 |
| `key part` | 4 random characters | required | required | Same alphabet as the reference. Set by the core at every issue including a reissue, never a vendor input. Reference plus key part is the **key identifier**, `LIC-XXXXX-XXXXX-XXXX`, visible at the start of the key string | PDR-0020, PDR-0017 |
| `vendor tag` | text | optional | optional | 1 to 64 characters from `A-Z a-z 0-9 - _ . # /`; no spaces; kept as supplied. The vendor's own label, e.g. an order ID. Not unique, not carried forward on reissue; an invalid value is rejected, never cleaned. A label only: no effect on superseding, combining or gating. Reported for verified keys only. Never personal data | PDR-0022, PDR-0006 |
| `issued` | UTC date and time, to the second | required | required | Set by the core at signing, never a vendor input. Among verified keys with the same product and reference, a key supersedes those issued strictly earlier | PDR-0016, PDR-0009 |
| `expires` | date | stated | stated | Valid until the end of that date in UTC. Absent from the key means never expires. The issuing request states a date or perpetual, exactly one; neither or both is rejected. Rejected if before the current UTC date | PDR-0016, PDR-0029 |
| `features` | 0..N name / value pairs | optional | optional | See below | PDR-0010, PDR-0013, PDR-0018 |

Feature entries:

| Part | Rules | Decision |
|---|---|---|
| name | Lowercase `a-z`, digits, hyphens; starts with a letter. At most once per key. Looked up ignoring case | PDR-0013, PDR-0010 |
| value | Switch (present = granted; a plain name), number or text. The type is what the vendor issues; text is never read as a number | PDR-0010, PDR-0018 |
| number | Zero or positive; at most 4 decimal places and 15 digits; an additive quantity (a count, limit or amount), never a rate or factor. Summed across licenses | PDR-0010 |
| text | Non-empty; at most 256 characters; no leading or trailing whitespace, line breaks or control characters. Never combined: equal values (exact, case-sensitive) count as one; different values, or text alongside a switch or number under one name, are a conflict | PDR-0018 |
| not allowed | Explicit `false`; negative or malformed numbers; empty or padded text | PDR-0010, PDR-0018 |

Never in a key: customer name, email, company or any other personal data (PDR-0006), including
in the vendor tag (PDR-0022).

The same schema, by role:

```
  BASE LICENSE                          ADD-ON LICENSE
  product      required                 product      required (the base's)
  role         base                     role         add-on
  reference    required                 reference    required
  key part     set by the core          key part     set by the core
  vendor tag   optional                 vendor tag   optional
  issued       set by the core          issued       set by the core
  expires      date or perpetual        expires      date or perpetual (own term)
  features     0..N                     features     0..N
  licenses the product on its own       counts only while a valid base
                                        for the same product is present
```

## Notation

Examples show a key's **contents** using the schema above, not the key string. Vendors,
products, references and dates are illustrative.

```
  product    acme.commerce
  role       base
  reference  LIC-8F3AK-M7RXB
  issued     2026-03-01T09:14Z
  expires    2027-03-01          valid until 2027-03-01 23:59:59 UTC
  features   ecommerce           switch
             max-orders: 500     number
```

Text values (`licensed-domain: example.com`) are shown in examples 16 and 17.

The key string itself starts with the key identifier, then the opaque part (PDR-0020):

```
  LIC-8F3AK-M7RXB-7Q2D . q83KdL...............Zx9PzQ
  reference       key     opaque part (illustrative)
                  part
```

In site evaluations, keys are shortened to reference, role and the fields that matter. An
evaluation result names each key by its full key identifier; the key part is left out of the
examples except where two keys share a reference and it matters (examples 7 and 18).
Issue times are recorded to the second (PDR-0016); the examples show minutes except where
seconds matter (example 7).
Site evaluations assume **now is 2026-10-01** unless stated.

## 1. Minimal license

```
  product    acme.seo-toolkit
  role       base
  reference  LIC-4HN7T-QW2ZC
  issued     2026-01-10T14:02Z
```

Issued as perpetual: never expires. No features. Valid. The package asks "is acme.seo-toolkit
licensed?" and gets yes; "is feature X granted?" is always no (PDR-0010).

## 2. Typical base license

```
  product    acme.commerce
  role       base
  reference  LIC-8F3AK-M7RXB
  issued     2026-03-01T09:14Z
  expires    2027-03-01
  features   ecommerce
             max-orders: 500
```

Valid until 2027-03-01 23:59:59 UTC. A host showing this to a site owner in Seattle would say
"expires 1 Mar 2027, 15:59 your time" (PDR-0016; display is deferred, D5).

## 3. Renewal: same reference supersedes

The implementor pastes the renewed key and leaves the old one in place.

```
  LIC-8F3AK-M7RXB  base  issued 2025-03-01T10:02Z  expires 2026-03-01  max-orders: 500  <- superseded
  LIC-8F3AK-M7RXB  base  issued 2026-03-01T09:14Z  expires 2027-03-01  max-orders: 500  <- counts
```

Result: `max-orders: 500`, not 1000. The old key is shown as *superseded*, not as a conflict
(PDR-0009).

## 4. Same-day correction

The vendor issues a typo, then reissues under the same reference an hour later:

```
  LIC-8F3AK-M7RXB  base  issued 2026-03-01T09:14Z  max-orders: 50    <- superseded
  LIC-8F3AK-M7RXB  base  issued 2026-03-01T10:21Z  max-orders: 500   <- counts
```

The core sets `issued` to the moment of signing, so the correction always wins (PDR-0016).
A correction that changes the role works the same way: example 5.

## 5. Reissue that changes the role

The latest issued wins even when the role differs (PDR-0009). A vendor issues a base as an
add-on by mistake, then reissues under the same reference:

```
  LIC-8F3AK-M7RXB  add-on  issued 2026-03-01T09:14Z  ecommerce, max-orders 500   superseded (role was add-on)
  LIC-8F3AK-M7RXB  base    issued 2026-03-01T10:21Z  ecommerce, max-orders 500   valid
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   ecommerce, max-orders 500
```

The same rule when the reissue is the mistake:

```
  LIC-8F3AK-M7RXB  base    issued 2026-03-01T09:14Z  ecommerce, max-orders 500   superseded (role was base)
  LIC-8F3AK-M7RXB  add-on  issued 2026-09-12T15:30Z  ecommerce, max-orders 500   inactive: no valid base
  ---------------------------------------------------------------------------------
  acme.commerce  NOT licensed   no features
```

The result reports that the superseded key carried a different role, so *inactive: no valid
base* comes with its cause. The vendor fixes it by reissuing `LIC-8F3AK-M7RXB` as a base.

## 6. Which keys may supersede

Only a verified key for the product being evaluated supersedes (PDR-0009).

**A key that fails verification never supersedes.** The site owner buys an upgrade, reissued
under the same reference, and the new key is mangled when pasted:

```
  LIC-8F3AK-M7RXB  base  issued 2026-03-01T09:14Z  max-orders 500    valid
  claims to be LIC-8F3AK-M7RXB                                       not verified   <- does not supersede
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   max-orders 500
```

The working key keeps counting. The failed key reports its reason and claimed identifiers
only, never its claimed issue time or features (PDR-0019; example 18).

**A key for another product takes no part.** Two vendors happen to generate the same
reference. Evaluating acme.commerce:

```
  LIC-8F3AK-M7RXB  base  acme.commerce  issued 2026-03-01T09:14Z   valid
  claims to be LIC-8F3AK-M7RXB for zenith.forms                    wrong product   <- does not supersede
  ---------------------------------------------------------------------------------
  acme.commerce  licensed
```

**An expired key still supersedes.** The vendor types `expires 2099-03-01`, then reissues with
the six-month term that was sold:

```
  LIC-8F3AK-M7RXB  base  issued 2026-03-01T09:14Z  expires 2099-03-01   superseded
  LIC-8F3AK-M7RXB  base  issued 2026-03-01T10:21Z  expires 2026-09-01   expired
  ---------------------------------------------------------------------------------
  acme.commerce  NOT licensed   no features
```

The correction does not undo itself on the day it expires: the older key stays superseded.

## 7. The same key twice, and keys issued at the same time

What happens when no key under a reference is later than another (PDR-0009). The site holds
this base license:

```
  LIC-8F3AK-M7RXB-7Q2D  base  issued 2026-03-01T09:14:07Z  max-orders 500
```

**The same key supplied twice**, here in a settings file and in an environment variable. It
counts once:

```
  1  LIC-8F3AK-M7RXB-7Q2D  base  max-orders 500   valid
  2  LIC-8F3AK-M7RXB-7Q2D  base  max-orders 500   duplicate of row 1
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   max-orders 500
```

Nobody's error, and nothing to fix. The implementor may remove either copy.

**Different keys tied for latest: both count.** On 2026-09-28 the site owner upgrades to 2000
orders. The vendor's system submits the reissue twice in the same second, and both keys reach
the site:

```
  1  LIC-8F3AK-M7RXB-7Q2D  issued 2026-03-01T09:14:07Z  max-orders 500    superseded
  2  LIC-8F3AK-M7RXB-D4WK  issued 2026-09-28T14:30:22Z  max-orders 2000   valid
        vendor error: same issue time as LIC-8F3AK-M7RXB-K9PX
  3  LIC-8F3AK-M7RXB-K9PX  issued 2026-09-28T14:30:22Z  max-orders 2000   valid
        vendor error: same issue time as LIC-8F3AK-M7RXB-D4WK
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   max-orders 4000
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

## 8. Add-ons: capacity pack and feature

```
  product    acme.commerce          product    acme.commerce
  role       add-on                 role       add-on
  reference  LIC-4Z9BE-T6WNH        reference  LIC-77DQS-9YJ4M
  issued     2026-06-15T16:40Z      issued     2026-09-01T08:05Z
  expires    2027-03-01             expires    2026-11-01     (monthly subscription)
  features   max-orders: 1000       features   ai-assist
```

Neither key licenses acme.commerce on its own (PDR-0011).

## 9. Combined: base + add-ons on one site

Site holding examples 2 and 8, plus a second capacity pack:

```
  LIC-8F3AK-M7RXB  base    ecommerce, max-orders 500   exp 2027-03-01   valid
  LIC-4Z9BE-T6WNH  add-on  max-orders 1000             exp 2027-03-01   valid
  LIC-2V5C9-HKD3P  add-on  max-orders 500              exp 2027-03-01   valid
  LIC-77DQS-9YJ4M  add-on  ai-assist                   exp 2026-11-01   valid
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   ecommerce, ai-assist, max-orders 2000
```

Switches: granted if any counted license grants them. Numbers: summed, base included
(PDR-0011).

## 10. Decimal quantities

```
  LIC-9QW3E-RT5YU  base    acme.media-vault   storage-gb: 10     valid
  LIC-M4K8N-B6VC2  add-on  acme.media-vault   storage-gb: 2.5    valid
  LIC-X7Z3H-J9FD4  add-on  acme.media-vault   storage-gb: 2.5    valid
  ---------------------------------------------------------------------------------
  acme.media-vault  licensed   storage-gb 15
```

Sums are exact at the precision written (PDR-0010). A rate such as `discount-rate: 0.15` does
not belong in a key: two licenses would sum it to 0.30.

## 11. Add-on expires before the base

Example 9's site on **2026-11-15**, `LIC-77DQS-9YJ4M` not renewed:

```
  LIC-8F3AK-M7RXB  base    valid
  LIC-4Z9BE-T6WNH  add-on  valid
  LIC-2V5C9-HKD3P  add-on  valid
  LIC-77DQS-9YJ4M  add-on  expired
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   ecommerce, max-orders 2000        (ai-assist dropped)
```

## 12. Base expires before the add-ons

Example 9's site on **2027-03-02**, but the capacity packs were bought with a longer term
(`expires 2027-06-15`):

```
  LIC-8F3AK-M7RXB  base    expired
  LIC-4Z9BE-T6WNH  add-on  inactive: no valid base
  LIC-2V5C9-HKD3P  add-on  inactive: no valid base
  ---------------------------------------------------------------------------------
  acme.commerce  NOT licensed   no features
```

*Inactive* is not *expired*. When the base is renewed under `LIC-8F3AK-M7RXB`, both packs count
again with no reissue (PDR-0011).

## 13. Two base licenses bought by mistake

```
  LIC-8F3AK-M7RXB  base  ecommerce, max-orders 500   valid
  LIC-C2D8R-XE6GU  base  ecommerce, max-orders 500   valid
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   ecommerce, max-orders 1000      note: 2 base licenses
```

Both count; the note prompts the site owner to ask the vendor about a refund (PDR-0011). An
upgrade must be a reissue under `LIC-8F3AK-M7RXB`, or it stacks the same way.

## 14. Trial

There is no trial kind (PDR-0014). A trial is a base license with a short expiry, optionally
with a feature the vendor checks by convention:

```
  product    acme.commerce
  role       base
  reference  LIC-7B2SW-NF5HA
  issued     2026-09-20T11:30Z
  expires    2026-10-20
  features   ecommerce
             trial
```

## 15. Add-on product (a separate package)

A shipping extension installed as its own package is its own product, not an add-on license,
even from a third-party vendor:

```
  product    zenith.commerce-shipping
  role       base
  reference  LIC-5E3FT-BPZ7Y
  issued     2026-05-01T12:00Z
  expires    2027-05-01
  features   shipping-rates
```

It never combines with acme.commerce's licenses. Whether it should be shown as inactive when
acme.commerce is not licensed is deferred (`deferred-scope.md` D9).

## 16. Text features: domain and tenant

Text carries what a switch cannot name in advance (PDR-0018). Here a vendor binds a license to
a site by convention and identifies the customer's account on its own service:

```
  product    acme.search-cloud
  role       base
  reference  LIC-3TQ7A-YE9DR
  issued     2026-04-12T10:30Z
  expires    2027-04-12
  features   licensed-domain: example.com
             tenant: t-8c21f
             max-documents: 50000
```

The library reports `licensed-domain` as `example.com`. Whether the package compares it with
the site's hostnames, and what it does on a mismatch, is the vendor's decision. Keys stay
bearer tokens (PDR-0011).

## 17. Text across licenses: equal counts as one, different conflicts

An add-on for example 16 that repeats the domain:

```
  LIC-3TQ7A-YE9DR  base    licensed-domain example.com, tenant t-8c21f, max-documents 50000   valid
  LIC-K6W2P-D4ZHN  add-on  licensed-domain example.com, max-documents 25000                   valid
  ---------------------------------------------------------------------------------------------
  acme.search-cloud  licensed   licensed-domain example.com, tenant t-8c21f, max-documents 75000
```

The same add-on issued with a typo in the domain:

```
  LIC-3TQ7A-YE9DR  base    licensed-domain example.com, tenant t-8c21f, max-documents 50000   valid    conflict: licensed-domain
  LIC-K6W2P-D4ZHN  add-on  licensed-domain exmaple.com, max-documents 25000                   valid    conflict: licensed-domain
  ---------------------------------------------------------------------------------------------
  acme.search-cloud  licensed   tenant t-8c21f, max-documents 75000
                                licensed-domain: CONFLICT (2 licenses disagree)
```

Both licenses stay valid and the numbers still sum; only `licensed-domain` answers nothing
until the vendor reissues `LIC-K6W2P-D4ZHN`. Comparison is exact: `Example.com` would conflict
the same way, as would `licensed-domain: 500` (text) alongside a number under that name.

## 18. Keys that fail verification

What the result reports for a failed key, and how each key is named (PDR-0019, PDR-0020). A
site runs acme.commerce. The implementor supplied seven keys:

| # | What happened |
|---|---|
| 1 | Base license, pasted correctly |
| 2 | Capacity pack of 1000 orders, pasted correctly |
| 3 | The same capacity pack, reissued on 2026-09-28 as an upgrade to 2000 orders. A character was lost in the paste |
| 4 | The zenith.commerce-shipping key from example 15, pasted here by mistake |
| 5 | A second capacity pack, cut off at the end by an email client |
| 6 | An `ai-assist` add-on signed with the vendor's new signing key. The site runs an older acme.commerce that does not know it |
| 7 | A key someone edited to say `expires 2099-12-31` and `max-orders 999999`, under the base license's reference |

The result, one row per supplied key, in the order supplied:

```
  VERIFIED: claims are facts, reported in full
  1  LIC-8F3AK-M7RXB-7Q2D  base    valid   expires 2027-03-01, ecommerce, max-orders 500
  2  LIC-4Z9BE-T6WNH-D4WK  add-on  valid   expires 2027-03-01, max-orders 1000

  FAILED: reason, plus claimed product and identifier only
  3  not verified                 claims to be LIC-4Z9BE-T6WNH-K9PX for acme.commerce
  4  wrong product                claims to be LIC-5E3FT-BPZ7Y-2MHS for zenith.commerce-shipping
  5  unreadable                   claims to be LIC-2V5C9-HKD3P-X3BN
  6  signing key not recognised   claims to be LIC-77DQS-9YJ4M-H8RC for acme.commerce
  7  not verified                 claims to be LIC-8F3AK-M7RXB-P6TY for acme.commerce
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   ecommerce, max-orders 1500
```

- **Never reported**: the role, issue time, expiry and features the failed keys claim. Key 7's
  `expires 2099-12-31` and `max-orders 999999` appear nowhere. Neither does key 6's
  `ai-assist`: the site owner sees `LIC-77DQS-9YJ4M` is not in effect and matches it to their
  purchase.
- **Key 5 has a name** because its start survived the cut. The site owner can see which
  purchase is broken. A string with no readable identifier, such as a pasted password, would be
  "the fifth key" and nothing more.
- **Keys 2 and 3 share a reference.** The key part (`D4WK`, `K9PX`) tells the implementor which
  string to paste again. Searching the site's settings for `LIC-4Z9BE-T6WNH-K9PX` finds it.
- **Keys 3 and 7 are both *not verified*.** The library cannot tell a paste error from an
  edit, and names neither as tampering. Neither supersedes the verified key that shares its
  reference (example 6), so keys 1 and 2 keep counting.
- **Key 4 is *wrong product*, not *signing key not recognised*.** The claimed product is
  checked first, so a mis-paste is reported as one.
- **First action per reason**: unreadable or not verified, paste again; wrong product, move the
  key; signing key not recognised, update the product, then ask the vendor (PDR-0019).
- **Whitespace is not a failure.** Had key 1 been wrapped across two lines by an email client,
  or carried a trailing newline from a settings file, it would still be valid: all whitespace
  is removed before reading (PDR-0021).
- **A key that verifies but breaks the schema is *unreadable*.** A key signed by the vendor's
  trusted signing key with `max-orders: -200`, which the core refuses at issue (example 19),
  can only come from a faulty vendor tool. It is reported as unreadable with its identifier
  and grants nothing. Pasting it again does not help; the vendor must reissue (PDR-0021).

## 19. Rejected when issued

Each of these fails at the vendor; no key is produced. Assumes issuing on 2026-10-01.

| Input | Reason | Decision |
|---|---|---|
| no `role` | Role is required | PDR-0011 |
| `product commerce` | Product ID must be `vendor.product` | PDR-0017 |
| `product Acme.Commerce` | Product ID is lowercase | PDR-0017 |
| `issued` supplied by the vendor | Set by the core at signing | PDR-0016 |
| `key part` supplied by the vendor | Set by the core at every issue | PDR-0020 |
| `expires 2026-09-30` | Before the current UTC date | PDR-0016 |
| neither an `expires` date nor perpetual | The expiry must be stated | PDR-0029 |
| an `expires` date and perpetual | State one, not both | PDR-0029 |
| `pro: false` | Explicit `false` is invalid; absence means not granted | PDR-0010 |
| `licensed-domain: ""` | Text is non-empty; a switch is a plain name | PDR-0018 |
| `licensed-domain: " example.com"` | No leading or trailing whitespace in text | PDR-0018 |
| `notes: "line one` ⏎ `line two"` | No line breaks or control characters in text | PDR-0018 |
| text longer than 256 characters | Keys must stay pasteable | PDR-0018 |
| `max-orders: "5OO"` | Malformed number | PDR-0010 |
| `max-orders: -200` | Numbers are zero or positive | PDR-0010 |
| `storage-gb: 2.12345` | More than 4 decimal places | PDR-0010 |
| `max-orders: 500` and `max-orders: 1000` in one key | A name appears at most once per key | PDR-0010 |
| `Max Orders: 500` | Names are lowercase `a-z`, digits, hyphens, starting with a letter | PDR-0013 |
| `vendor tag "INV 2026 04"` | No spaces in a vendor tag | PDR-0022 |
| `vendor tag "jane@acme.com"` | `@` is not allowed in a vendor tag | PDR-0022 |
| `vendor tag ""` | A vendor tag is not empty; omit it instead | PDR-0022 |
| vendor tag longer than 64 characters | Keys must stay pasteable | PDR-0022 |
| a customer's name or email anywhere in the key | No personal data in keys | PDR-0006 |

`expires 2026-10-01` (today) is allowed: valid until 23:59:59 UTC.

## 20. Vendor tag

The vendor's shop issues each key on purchase and signs its order number into it (PDR-0022).
The 2025 base license was renewed in 2026 under the same reference; one 2026 order bought both
the renewal and a capacity pack.

```
  product     acme.commerce               product     acme.commerce
  role        base                        role        add-on
  reference   LIC-8F3AK-M7RXB             reference   LIC-4Z9BE-T6WNH
  vendor tag  SHOP-2026-000123            vendor tag  SHOP-2026-000123
  issued      2026-03-01T09:14Z           issued      2026-03-01T09:14Z
  expires     2027-03-01                  expires     2027-03-01
  features    ecommerce                   features    max-orders: 1000
              max-orders: 500
```

The site still holds the 2025 key (tag `SHOP-2025-000087`) and a third key that fails
verification. The result:

```
  KEY                    STATE        VENDOR TAG
  LIC-8F3AK-M7RXB-7Q2D   valid        SHOP-2026-000123
  LIC-8F3AK-M7RXB-4TQ9   superseded   SHOP-2025-000087
  LIC-4Z9BE-T6WNH-D4WK   valid        SHOP-2026-000123
  LIC-2PW9H-KD4NZ-X3MF   not verified (not reported)
```

- **Two keys share a tag.** One order bought two licenses. Tags are not unique and the library
  never compares them; the references keep the keys apart and decide what combines.
- **The renewal has a new tag.** The core keeps nothing, so a reissue carries whatever the
  vendor passes: here the renewal order. Superseding still runs on the reference.
- **The failed key reports no tag.** Its tag is an unverified claim and could be anything; the
  vendor traces it by its key identifier.
- **Support without the key.** The site owner quotes `SHOP-2026-000123` from the result or
  their receipt; the vendor finds the order in its shop.

## Open questions that may change these examples

No product question is open in design.md.

Deferred questions that could also change these examples when they return (`deferred-scope.md`):
Q1 site label (a label per stored key), Q3 routing (how keys in examples 3, 9 and 13 reach
acme.commerce), Q17 product dependencies (example 15), and the Umbraco version range (D1).

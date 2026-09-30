# License examples

Worked examples of what license keys contain and how a site evaluates them, under the product
decisions in [`decisions/`](decisions/README.md).

> **Keep these examples up to date.** When a product decision (PDR) is added, changed or
> superseded, or an open question below is settled, update every example it affects in the
> same change, and adjust the "Last checked against" line. An example that contradicts a PDR is
> a documentation bug.
>
> Last checked against: PDR-0001 to PDR-0018 (2026-09-30).

## Schema

The logical contents of a license key: what it holds and the rules checked when it is issued.
This is not the encoding. How contents are encoded and signed is a technology decision
(ADR-0001, draft), which also adds technical fields not shown here, such as the identifier of
the signing secret used for rotation.

| Field | Type | Base | Add-on | Rules | Decision |
|---|---|---|---|---|---|
| `product` | product ID | required | required | `vendor.product`: two dot-separated parts of lowercase `a-z`, digits, hyphens, each starting with a letter. An add-on carries the base product's ID | PDR-0017, PDR-0011 |
| `role` | `base` or `add-on` | required | required | No default; no other values | PDR-0011 |
| `reference` | license reference | required | required | 10 random characters from uppercase letters and digits without `0 O 1 I L`, shown `LIC-XXXXX-XXXXX`; matched ignoring case, hyphens, spaces. Generated at first issue, kept on reissue; not secret | PDR-0009, PDR-0017 |
| `issued` | UTC date and time | required | required | Set by the core at signing, never a vendor input. Latest wins among keys with the same reference | PDR-0016 |
| `expires` | date | optional | optional | Valid until the end of that date in UTC. Omitted means never expires. Rejected if before the current UTC date | PDR-0016 |
| `umbraco.min` | whole major | optional | **not allowed** | Inclusive; covers every minor, patch, pre-release | PDR-0012 |
| `umbraco.max` | whole major | optional | **not allowed** | Inclusive; `min` must not exceed `max` | PDR-0012 |
| `features` | 0..N name / value pairs | optional | optional | See below | PDR-0010, PDR-0013, PDR-0018 |

Feature entries:

| Part | Rules | Decision |
|---|---|---|
| name | Lowercase `a-z`, digits, hyphens; starts with a letter. At most once per key. Looked up ignoring case | PDR-0013, PDR-0010 |
| value | Switch (present = granted; a plain name), number or text. The type is what the vendor issues; text is never read as a number | PDR-0010, PDR-0018 |
| number | Zero or positive; at most 4 decimal places and 15 digits; an additive quantity (a count, limit or amount), never a rate or factor. Summed across licenses | PDR-0010 |
| text | Non-empty; at most 256 characters; no leading or trailing whitespace, line breaks or control characters. Never combined: equal values (exact, case-sensitive) count as one; different values, or text alongside a switch or number under one name, are a conflict | PDR-0018 |
| not allowed | Explicit `false`; negative or malformed numbers; empty or padded text | PDR-0010, PDR-0018 |

Never in a key: customer name, email, company or any other personal data (PDR-0006).

The same schema, by role:

```
  BASE LICENSE                          ADD-ON LICENSE
  product      required                 product      required (the base's)
  role         base                     role         add-on
  reference    required                 reference    required
  issued       set by the core          issued       set by the core
  expires      optional                 expires      optional (own term)
  umbraco      optional  min..max       umbraco      -- rejected at issue --
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
  umbraco    17..18              min..max
  features   ecommerce           switch
             max-orders: 500     number
```

Text values (`licensed-domain: example.com`) are shown in examples 14 and 15.

`umbraco 17..` means 17 and later; `..18` means up to and including 18; omitted means any
version. In site evaluations, keys are shortened to reference, role and the fields that matter.
Site evaluations assume **now is 2026-10-01** unless stated.

## 1. Minimal license

```
  product    acme.seo-toolkit
  role       base
  reference  LIC-4HN7T-QW2ZC
  issued     2026-01-10T14:02Z
```

Never expires, any Umbraco version, no features. Valid. The package asks "is acme.seo-toolkit
licensed?" and gets yes; "is feature X granted?" is always no (PDR-0010).

## 2. Typical base license

```
  product    acme.commerce
  role       base
  reference  LIC-8F3AK-M7RXB
  issued     2026-03-01T09:14Z
  expires    2027-03-01
  umbraco    17..18
  features   ecommerce
             max-orders: 500
```

Valid on Umbraco 17.x and 18.x, including pre-releases, until 2027-03-01 23:59:59 UTC. A site
owner in Seattle sees "expires 1 Mar 2027, 15:59 your time" (PDR-0016).

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

## 5. Add-ons: capacity pack and feature

```
  product    acme.commerce          product    acme.commerce
  role       add-on                 role       add-on
  reference  LIC-4Z9BE-T6WNH        reference  LIC-77DQS-9YJ4M
  issued     2026-06-15T16:40Z      issued     2026-09-01T08:05Z
  expires    2027-03-01             expires    2026-11-01     (monthly subscription)
  features   max-orders: 1000       features   ai-assist
```

No `umbraco` line: add-ons cannot carry a range; the base's range applies (PDR-0012). Neither
key licenses acme.commerce on its own (PDR-0011).

## 6. Combined: base + add-ons on one site

Site on Umbraco 18.2 holding examples 2 and 5, plus a second capacity pack:

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

## 7. Decimal quantities

```
  LIC-9QW3E-RT5YU  base    acme.media-vault   storage-gb: 10     valid
  LIC-M4K8N-B6VC2  add-on  acme.media-vault   storage-gb: 2.5    valid
  LIC-X7Z3H-J9FD4  add-on  acme.media-vault   storage-gb: 2.5    valid
  ---------------------------------------------------------------------------------
  acme.media-vault  licensed   storage-gb 15
```

Sums are exact at the precision written (PDR-0010). A rate such as `discount-rate: 0.15` does
not belong in a key: two licenses would sum it to 0.30.

## 8. Add-on expires before the base

Example 6's site on **2026-11-15**, `LIC-77DQS-9YJ4M` not renewed:

```
  LIC-8F3AK-M7RXB  base    valid
  LIC-4Z9BE-T6WNH  add-on  valid
  LIC-2V5C9-HKD3P  add-on  valid
  LIC-77DQS-9YJ4M  add-on  expired
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   ecommerce, max-orders 2000        (ai-assist dropped)
```

## 9. Base expires before the add-ons

Example 6's site on **2027-03-02**, but the capacity packs were bought with a longer term
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

## 10. Umbraco major upgrade

Example 6's site upgraded to Umbraco 19.0:

```
  LIC-8F3AK-M7RXB  base    out of range (covers 17-18)
  LIC-4Z9BE-T6WNH  add-on  inactive: no valid base
  LIC-2V5C9-HKD3P  add-on  inactive: no valid base
  LIC-77DQS-9YJ4M  add-on  inactive: no valid base
  ---------------------------------------------------------------------------------
  acme.commerce  NOT licensed
```

The whole product goes out of range, never part of it; one reissued base
(`LIC-8F3AK-M7RXB`, `umbraco 17..19`) restores everything. A minor or patch update (18.2 to
18.4) never changes the result (PDR-0012).

## 11. Two base licenses bought by mistake

```
  LIC-8F3AK-M7RXB  base  ecommerce, max-orders 500   valid
  LIC-C2D8R-XE6GU  base  ecommerce, max-orders 500   valid
  ---------------------------------------------------------------------------------
  acme.commerce  licensed   ecommerce, max-orders 1000      note: 2 base licenses
```

Both count; the note prompts the site owner to ask the vendor about a refund (PDR-0011). An
upgrade must be a reissue under `LIC-8F3AK-M7RXB`, or it stacks the same way.

## 12. Trial

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

## 13. Add-on product (a separate package)

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
acme.commerce is not licensed is open (design.md Q17).

## 14. Text features: domain and tenant

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

## 15. Text across licenses: equal counts as one, different conflicts

An add-on for example 14 that repeats the domain:

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

## 16. Rejected when issued

Each of these fails at the vendor; no key is produced. Assumes issuing on 2026-10-01.

| Input | Reason | Decision |
|---|---|---|
| no `role` | Role is required | PDR-0011 |
| `role add-on` with `umbraco 17..18` | Only base licenses carry a range | PDR-0012 |
| `umbraco 19..17` | Minimum above maximum | PDR-0012 |
| `umbraco 17.3..18` | Bounds are whole majors | PDR-0012 |
| `product commerce` | Product ID must be `vendor.product` | PDR-0017 |
| `product Acme.Commerce` | Product ID is lowercase | PDR-0017 |
| `issued` supplied by the vendor | Set by the core at signing | PDR-0016 |
| `expires 2026-09-30` | Before the current UTC date | PDR-0016 |
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
| a customer's name or email anywhere in the key | No personal data in keys | PDR-0006 |

`expires 2026-10-01` (today) is allowed: valid until 23:59:59 UTC.

## Open questions that may change these examples

| Question (design.md) | Could affect |
|---|---|
| Q1 site label | A label per stored key, alongside its contents |
| Q3 routing keys to products | How keys in examples 3, 6 and 11 reach acme.commerce |
| Q5 marking unverified claims | How invalid or unreadable keys appear in evaluations; how the feature conflict in example 15 is shown alongside them |
| Q17 dependencies between products | Example 13 |
| Q18 same reference, different role or product | Examples 3 and 4: what supersedes when the reissue changes role, and that superseding is scoped by product |

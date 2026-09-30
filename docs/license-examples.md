# License examples

Worked examples of what license keys contain and how a site evaluates them, under the product
decisions in [`decisions/`](decisions/README.md).

> **Keep these examples up to date.** When a product decision (PDR) is added, changed or
> superseded, or an open question below is settled, update every example it affects in the
> same change, and adjust the "Last checked against" line. An example that contradicts a PDR is
> a documentation bug.
>
> Last checked against: PDR-0001 to PDR-0015 (2026-09-30).

## Schema

The logical contents of a license key: what it holds and the rules checked when it is issued.
This is not the encoding. How contents are encoded and signed is a technology decision
(ADR-0001, draft), which also adds technical fields not shown here, such as the identifier of
the signing secret used for rotation.

| Field | Type | Base | Add-on | Rules | Decision |
|---|---|---|---|---|---|
| `product` | product ID | required | required | The product the key licenses; an add-on carries the base product's ID | PDR-0008, PDR-0011 |
| `role` | `base` or `add-on` | required | required | No default; no other values | PDR-0011 |
| `reference` | license reference | required | required | Random; unique per product; generated at first issue, kept on reissue; not secret | PDR-0009 |
| `issued` | date | required | required | Set at issue; latest wins among keys with the same reference | PDR-0009 |
| `expires` | date | optional | optional | Omitted means never expires | PDR-0012 |
| `umbraco.min` | whole major | optional | **not allowed** | Inclusive; covers every minor, patch, pre-release | PDR-0012 |
| `umbraco.max` | whole major | optional | **not allowed** | Inclusive; `min` must not exceed `max` | PDR-0012 |
| `features` | 0..N name / value pairs | optional | optional | See below | PDR-0010, PDR-0013 |

Feature entries:

| Part | Rules | Decision |
|---|---|---|
| name | Lowercase `a-z`, digits, hyphens; starts with a letter. Looked up ignoring case | PDR-0013 |
| value | Switch (present = granted; a plain name) or whole number | PDR-0010 |
| not allowed | Text values; explicit `false`; malformed numbers | PDR-0010 |

Never in a key: customer name, email, company or any other personal data (PDR-0006).

The same schema, by role:

```
  BASE LICENSE                          ADD-ON LICENSE
  product      required                 product      required (the base's)
  role         base                     role         add-on
  reference    required                 reference    required
  issued       required                 issued       required
  expires      optional                 expires      optional (own term)
  umbraco      optional  min..max       umbraco      -- rejected at issue --
  features     0..N                     features     0..N
  licenses the product on its own       counts only while a valid base
                                        for the same product is present
```

Not yet decided, and not covered by any PDR:

- Format of product IDs and license references (examples use `commerce`, `LIC-8F3A`).
- Precision of `issued` and `expires`: date or date and time, and in which time zone an expiry
  takes effect.
- Whether issuing rejects an `expires` earlier than `issued`.
- Whether whole numbers may be zero or negative. A negative add-on would reduce a summed
  capacity.
- Whether issuing rejects the same feature name twice in one key.

## Notation

Examples show a key's **contents** using the schema above, not the key string. Product IDs,
references and dates are illustrative.

```
  product    commerce
  role       base
  reference  LIC-8F3A
  issued     2026-03-01
  expires    2027-03-01
  umbraco    17..18              min..max
  features   ecommerce           switch
             max-orders: 500     whole number
```

`umbraco 17..` means 17 and later; `..18` means up to and including 18; omitted means any
version. Site evaluations assume **today is 2026-10-01** unless stated.

## 1. Minimal license

```
  product    seo-toolkit
  role       base
  reference  LIC-0A11
  issued     2026-01-10
```

Never expires, any Umbraco version, no features. Valid. The package asks "is seo-toolkit
licensed?" and gets yes; "is feature X granted?" is always no (PDR-0010).

## 2. Typical base license

```
  product    commerce
  role       base
  reference  LIC-8F3A
  issued     2026-03-01
  expires    2027-03-01
  umbraco    17..18
  features   ecommerce
             max-orders: 500
```

Valid on Umbraco 17.x and 18.x, including pre-releases, until 2027-03-01.

## 3. Renewal: same reference supersedes

The implementor pastes the renewed key and leaves the old one in place.

```
  LIC-8F3A  base  issued 2025-03-01  expires 2026-03-01  max-orders: 500   <- superseded
  LIC-8F3A  base  issued 2026-03-01  expires 2027-03-01  max-orders: 500   <- counts
```

Result: `max-orders: 500`, not 1000. The old key is shown as *superseded*, not as a conflict
(PDR-0009).

## 4. Add-ons: capacity pack and feature

```
  product    commerce               product    commerce
  role       add-on                 role       add-on
  reference  LIC-40BE               reference  LIC-77D0
  issued     2026-06-15             issued     2026-09-01
  expires    2027-03-01             expires    2026-11-01     (monthly subscription)
  features   max-orders: 1000       features   ai-assist
```

No `umbraco` line: add-ons cannot carry a range; the base's range applies (PDR-0012). Neither
key licenses Commerce on its own (PDR-0011).

## 5. Combined: base + add-ons on one site

Site on Umbraco 18.2 holding examples 2 and 4, plus a second capacity pack:

```
  LIC-8F3A  base    ecommerce, max-orders 500     exp 2027-03-01   valid
  LIC-40BE  add-on  max-orders 1000               exp 2027-03-01   valid
  LIC-21C9  add-on  max-orders 500                exp 2027-03-01   valid
  LIC-77D0  add-on  ai-assist                     exp 2026-11-01   valid
  ---------------------------------------------------------------------------
  Commerce  licensed   ecommerce, ai-assist, max-orders 2000
```

Switches: granted if any counted license grants them. Whole numbers: summed, base included
(PDR-0011).

## 6. Add-on expires before the base

Same site on **2026-11-15**, `LIC-77D0` not renewed:

```
  LIC-8F3A  base    valid
  LIC-40BE  add-on  valid
  LIC-21C9  add-on  valid
  LIC-77D0  add-on  expired
  ---------------------------------------------------------------------------
  Commerce  licensed   ecommerce, max-orders 2000        (ai-assist dropped)
```

## 7. Base expires before the add-ons

Same site on **2027-03-02**, but the capacity packs were bought with a longer term
(`expires 2027-06-15`):

```
  LIC-8F3A  base    expired
  LIC-40BE  add-on  inactive: no valid base
  LIC-21C9  add-on  inactive: no valid base
  ---------------------------------------------------------------------------
  Commerce  NOT licensed   no features
```

*Inactive* is not *expired*. When the base is renewed under `LIC-8F3A`, both packs count again
with no reissue (PDR-0011).

## 8. Umbraco major upgrade

Example 5's site upgraded to Umbraco 19.0:

```
  LIC-8F3A  base    out of range (covers 17-18)
  LIC-40BE  add-on  inactive: no valid base
  LIC-21C9  add-on  inactive: no valid base
  LIC-77D0  add-on  inactive: no valid base
  ---------------------------------------------------------------------------
  Commerce  NOT licensed
```

The whole product goes out of range, never part of it; one reissued base (`LIC-8F3A`,
`umbraco 17..19`) restores everything. A minor or patch update (18.2 to 18.4) never changes
the result (PDR-0012).

## 9. Two base licenses bought by mistake

```
  LIC-8F3A  base  ecommerce, max-orders 500   valid
  LIC-C2D1  base  ecommerce, max-orders 500   valid
  ---------------------------------------------------------------------------
  Commerce  licensed   ecommerce, max-orders 1000      note: 2 base licenses
```

Both count; the note prompts the site owner to ask the vendor about a refund (PDR-0011). An
upgrade must be a reissue under `LIC-8F3A`, or it stacks the same way.

## 10. Trial

There is no trial kind (PDR-0014). A trial is a base license with a short expiry, optionally
with a feature the vendor checks by convention:

```
  product    commerce
  role       base
  reference  LIC-7B20
  issued     2026-09-20
  expires    2026-10-20
  features   ecommerce
             trial
```

## 11. Add-on product (a separate package)

A shipping extension installed as its own package is its own product, not an add-on license:

```
  product    commerce-shipping
  role       base
  reference  LIC-5E12
  issued     2026-05-01
  expires    2027-05-01
  features   shipping-rates
```

It never combines with Commerce's licenses. Whether it should be shown as inactive when
Commerce is not licensed is open (design.md Q17).

## 12. Rejected when issued

Each of these fails at the vendor; no key is produced.

| Input | Reason | Decision |
|---|---|---|
| no `role` | Role is required | PDR-0011 |
| `role add-on` with `umbraco 17..18` | Only base licenses carry a range | PDR-0012 |
| `umbraco 19..17` | Minimum above maximum | PDR-0012 |
| `umbraco 17.3..18` | Bounds are whole majors | PDR-0012 |
| `edition: "enterprise"` | Text values are not allowed | PDR-0010 |
| `pro: false` | Explicit `false` is invalid; absence means not granted | PDR-0010 |
| `max-orders: "5OO"` | Malformed whole number | PDR-0010 |
| `Max Orders: 500` | Names are lowercase `a-z`, digits, hyphens, starting with a letter | PDR-0013 |
| a customer's name or email anywhere in the key | No personal data in keys | PDR-0006 |

## Open questions that may change these examples

| Question (design.md) | Could affect |
|---|---|
| Q1 site label | A label per stored key, alongside its contents |
| Q3 routing keys to products | How keys in examples 3, 5 and 9 reach Commerce |
| Q5 marking unverified claims | How invalid or unreadable keys appear in evaluations |
| Q17 dependencies between products | Example 11 |

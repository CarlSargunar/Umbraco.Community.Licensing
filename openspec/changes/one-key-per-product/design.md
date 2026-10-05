## Context

See `proposal.md` for motivation. The core library on branch `c2---extended-library-implementation`
implements the archived `license-key-management` change: `LicenseIssuer` signs a
`LicenseRequest` (product, role, reference, vendor tag, expiry, features); `LicenseEvaluator`
returns one `LicenseKeyRow` per string and a `ProductLicense`; the payload is strict JSON per
ADR-0001. Unreleased; no keys in the field.

Constraints carried over: offline only, BCL only, .NET 10, `TimeProvider` for the clock,
never throw on a bad key string, the core keeps no records (PDR-0005).

Personas: [`docs/personas.md`](../../../docs/personas.md). This change serves the implementor
(one key per product) and the site owner (purchases readable from the key) without harming the
vendor beyond the costs recorded in PDR-0036.

## Goals / Non-Goals

**Goals:**

- One payload shape: a license with 1..N purchases (ADR-0001 revision).
- One feature-combining routine used at issue, at read, and between keys, so the three cannot
  disagree.
- A vendor with no records can add a purchase to the key the site owner presents in a few
  lines of code.

**Non-Goals:**

- Reading keys issued by earlier builds of this branch. They become unreadable.
- Changes to the parked `issuing-add-on` change beyond a note in its design.md "Open".
- PDR-0029 (expiry stated as a date or perpetual). It stays with `issuing-add-on`; the expiry
  remains optional here.
- A purchase limit or key-length limit (PDR-0037).

## Decisions

### Payload: `purchases` array replaces `role`, `vendorTag`, `features`

```json
{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","issued":"2026-09-01T08:05:12Z",
 "expires":"2027-03-01","purchases":[
  {"kind":"base","name":"Commerce Standard","purchased":"2026-03-01","vendorTag":"SHOP-1001",
   "features":{"ecommerce":true,"max-orders":500}},
  {"kind":"add-on","name":"AI Assist","purchased":"2026-09-01","features":{"ai-assist":true}}]}
```

Field encodings, strict-read rules and rejected alternatives (positional arrays, top-level
features, stored combined features, a version field): ADR-0001, revised 2026-10-05. The key
string layout, signing input, number encoding and signing key ID are unchanged.

### Public API shape

```
  LicenseRequest   (sealed record, init-only)      LicensePurchase  (sealed record)
    ProductId   string?                              Kind       PurchaseKind?   Base | AddOn
    Reference   string?                              Name       string?
    Expires     DateOnly?                            Purchased  DateOnly?
    Purchases   IReadOnlyList<LicensePurchase>?      VendorTag  string?
    static ReissueOf(VerifiedLicense)                Features   IReadOnlyList<LicenseFeature>?

  VerifiedLicense                                   LicenseKeyRow       ProductLicense
    ProductId, Reference, KeyIdentifier,              (no SupersededRole) LicenseCount   int
    Issued, Expires                                                       IsLicensed => LicenseCount > 0
    Purchases   IReadOnlyList<VerifiedPurchase>                           (ValidBaseCount removed)
    Features    combined across purchases
```

- `LicenseRole` is renamed `PurchaseKind`. `LicenseKeyState.Inactive` is removed.
- `LicenseRequest` becomes a record so a vendor writes:

  ```csharp
  var current = LicenseRequest.ReissueOf(row.License!);
  var next = current with { Purchases = [.. current.Purchases!, aiAssist] };
  issuer.Issue(next, signingKey);
  ```

  `ReissueOf` copies product, reference, expiry and every purchase (each converted to a
  `LicensePurchase`) and does nothing else: no clock, no validation. Issuing checks as usual.
- Request fields stay nullable so a missing value is reported as a problem rather than a
  compile error, as today.
- `VerifiedPurchase` is a separate read type with non-nullable fields (a verified purchase has
  every required field), mirroring `VerifiedLicense` against `LicenseRequest`.

**Alternatives:** an `AddPurchase(keyString, purchase, privateKey)` convenience on the issuer
(rejected: hides verification and trusted keys inside the issuer, which today needs neither;
read-then-issue is two calls the vendor already has). Reusing `LicensePurchase` for the read
side (rejected: every field nullable on data the library has verified).

### One feature combiner

Today `LicenseEvaluator` combines features between keys. A shared internal combiner takes
feature sets in order and returns the combined set plus the names in conflict (different text,
or text beside a switch or number). Used in three places:

| Where | Input | A conflict means |
|---|---|---|
| Issue | the request's purchases | a request problem naming the feature (PDR-0037) |
| Read (schema check, ADR-0001 step 6) | a verified key's purchases | the key is unreadable (PDR-0021) |
| Evaluation | the combined features of each valid key | a conflict flag on the product and rows (PDR-0018) |

Because combining within a key never yields a conflict on a readable key, combining the keys'
combined sets gives the same result as combining every purchase of every valid key.

### Problem fields

`LicenseRequestProblem.Field` names a purchase by 1-based position: `purchases`,
`purchases[2].name`, `purchases[1].features.max-orders`. A cross-purchase text conflict is
reported once, on `features.<name>`, naming the purchases involved in its message.

### Evaluation

The per-key check order loses *inactive*. Combining sums the valid keys' combined features.
`LicenseCount` counts distinct references among valid rows, so tied keys under one reference
count once (PDR-0038). The role-change note on superseded rows is removed.

## Risks / Trade-offs

- [Key length grows with purchases] → No limit, per PDR-0037. ADR-0001 records the size per
  purchase; a key of 10 purchases stays under about 2,500 characters. Revisit if a vendor hits
  a vault or environment variable limit.
- [A vendor reissuing from records forgets an earlier purchase] → The site owner loses it only
  if the implementor installs the new key; the summary diff belongs in vendor tooling (the
  issuing add-on). `ReissueOf` makes the from-key path carry everything by default.
- [Marketplace add-on sales need the vendor to reissue] → Accepted in PDR-0036; a separate
  license still works and combines (PDR-0038).
- [Breaking public API on a branch with tests] → The library is unreleased; tests are rewritten
  with it (tasks.md section 6).

## Migration Plan

None for users: nothing is released. On the branch, every test that builds a request or reads a
role is rewritten. `docs/license-examples.md` (already rewritten) is the source for example
tests. ADR-0001's worked example keys are regenerated after the payload change.

---

## Requirements and open questions

Product decisions are PDRs in [`docs/decisions/`](../../../docs/decisions/README.md); the key
schema and worked examples are in [`docs/license-examples.md`](../../../docs/license-examples.md).
This section indexes the questions those records cite. Q1 to Q7 were settled with the Product
Owner on 2026-10-05.

| Q | Question | Outcome |
|---|---|---|
| Q1 | What does one key stand for? | PDR-0036: the whole license for one product; each purchase reissues it under the same reference; supersedes PDR-0008 |
| Q2 | What does each purchase carry? | PDR-0037: kind, name, features, optional vendor tag, purchase date |
| Q3 | One base per license? May an add-on repeat? | PDR-0037: exactly one base; add-ons repeat and their numbers sum |
| Q4 | Two keys for one product under different references | PDR-0038: they combine; the result reports the number of licenses; supersedes PDR-0011 |
| Q5 | What term does an add-on have? | PDR-0036: the license's; one expiry per key, perpetual included |
| Q6 | How does a vendor with no records add a purchase? | PDR-0036: reissue from the current key the site owner presents; the key is the proof of ownership PDR-0008 found missing |
| Q7 | Does this change revise the issuing add-on? | No: library only. PDR-0024 to PDR-0027, PDR-0034, PDR-0035 noted for revisit in `issuing-add-on` design.md "Open" |

Amended: PDR-0009, PDR-0010, PDR-0018, PDR-0019, PDR-0021, PDR-0022 (status lines name the
amending PDR).

No open product or technical questions.

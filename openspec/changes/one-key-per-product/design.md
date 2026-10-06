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

Open questions: see "Exploration pass 2026-10-06" below.

### Exploration pass 2026-10-06 (Analyst)

**Status: decisions not yet applied.** proposal.md, both delta specs, the Decisions section
above, tasks.md, PDR-0036 to PDR-0038, the PDR amendments, ADR-0001 and
`docs/license-examples.md` still describe the purchase-list shape (Q2, Q3). Apply this pass with
`opsx:update` before `opsx:apply`: product records first (Analyst), then design.md, ADR-0001 and
tasks.md (Architect). tasks.md section 1 was ticked against the superseded shape and must be
redone.

Reviewed the assumptions not confirmed during `opsx:propose`, then reopened Q2 and Q3.

| Q | Question | Outcome |
|---|---|---|
| Q8 | Purchase name rules | Confirmed: required, 1-64 characters, no edge whitespace, line breaks or control characters, one plain string in the vendor's language, display only. **Moot after Q11**; the rule is reused by Q14 |
| Q9 | Purchase date future check | Rule b: reject a date more than one day after the current UTC date, so every time zone's "today" passes and typos such as 2062 are caught. A purchase ahead of a future launch is dated the order day; the later term is expressed by the expiry (PDR-0016 rejected "valid from"; PDR-0029 leaves terms to the vendor). Rejected: a (not after today UTC; rejects real orders east of UTC), c (no check). **Moot after Q11** |
| Q10 | Text conflict between purchases in one key | Confirmed: rejected at issue. **Moot after Q11**: no combining within a key |
| Q11 | Does base vs add-on, or a purchase list, earn its place in one key? | **No. Shape 3: a key holds one feature set for its license; no purchase list, no kind.** Adding an add-on = the vendor reissues under the same reference with features edited (e.g. `max-orders` 1500 -> 2500); a refund edits them back. Purchase history lives in the vendor's records. The site owner's needs in `docs/personas.md` (licensed for, about to expire, missing) are met without a list; "what did I buy" entered only through this change's proposal. Supersedes the Q2 and Q3 outcomes; PDR-0037 to be dropped or superseded. Rejected: shape 1 (purchases with kind, the plan as proposed), shape 2 (purchases without kind; kind only fed an "exactly one base" check the vendor controls anyway) |
| Q12 | How is a key tied back to the purchaser? | Existing fields: the license reference (stable across reissues, PDR-0009) and a **key-level** vendor tag (PDR-0022 restored to its form before the PDR-0037 amendment). Guidance, not a rule: the tag holds a stable customer or account ID; the reference links the license's history in the vendor's records |
| Q13 | Reissue from the current key | Prefill product, reference, expiry, features, vendor tag and display name (Q14); the vendor edits. Risk stated in the PDR: the presented key may not be the latest; a vendor reissuing from an old key (e.g. forwarded from an old email) supersedes the newer key and drops features bought since. Option a: guidance only ("reissue from the key installed on the site"; records close the gap). Carl intends the `issuing-add-on` change to keep those records. Rejected: b (report a later key among those supplied; already the *superseded* state when every key is supplied), c (records required; breaks the no-records case PDR-0036 serves) |
| Q14 | Display name for the license | Optional key-level display name, e.g. `Commerce Pro`. Rules from Q8: 1-64 characters, no edge whitespace, line breaks or control characters, one language, signed as supplied, never trimmed. Display only: no part in superseding, combining or feature lookup. Serves a generic licensing screen that cannot know the vendor's feature labels; with two licenses each row shows its own name. Rejected: no name (a generic screen shows IDs and raw feature names only), labels per feature (grows every key; labels belong to the product) |
| Q15 | Multi-value text | Required (e.g. several licensed domains). **Its own value type ("text set", name not final)**, not delimited text: delimited text is one value under the text rule, so `a\|b` vs `b\|a`, or a base domain plus an "Extra domain" add-on on another key, would conflict. Text sets combine **between keys** by union; within a key the vendor writes the final set. Folded into this change (rather than a later change) because the combiner, payload, examples and both specs are rewritten here anyway. Delimiter comparison, for one-line input or display only (tooling, not the key): pipe is safest inside values (never in domains, emails or valid URLs), comma most familiar but common in URLs and names |
| Q16 | Text set beside single text under one name | Conflict: one name has one type, as with text beside a number. A vendor who issued single text reissues with a set under the same reference. Rejected: text treated as a set of one (a coercion rule; inconsistent with text vs text conflicting) |
| Q17 | Text set equality | Exact (same characters, same case), as single text (PDR-0018). The vendor normalises, e.g. lowercases domains, at issue. Rejected: case-insensitive (differs from single text; wrong for case-sensitive IDs; which spelling to report; culture rules) |
| Q18 | Text set: empty, duplicates, limits | **Open. Proposed, not confirmed:** empty set rejected at issue (omit the feature, as with `false`, PDR-0010); the same value twice in one set rejected, not deduplicated (PDR-0022 "reject, not clean"); each value follows the single-text rules (PDR-0018); order has no meaning but values are reported in the order issued; no limit on the number of values (no limit on features either), with a guidance note on key length (about 25 key characters per 15-character domain; switch to a number such as `max-domains` when a set grows to hundreds) |

Still open, in this order:

1. Q18: confirm the five proposed rows.
2. Text set reading: how a site reads a set (membership check, and whether that check may
   ignore case; listing the values).
3. Assumption from `opsx:propose`: adding features to an expired license requires renewing it
   in the same reissue (the library rejects a past expiry, PDR-0016). Still applies under Q11.
4. Assumption from `opsx:propose`: PDR-0029 (expiry stated as a date or perpetual) stays with
   `issuing-add-on`.
5. What survives of PDR-0036 and PDR-0038 under Q11. Expected: PDR-0036 stands, with "purchase
   appended" read as "features edited"; PDR-0038 stands; PDR-0010's "a name at most once in a
   key" restored; PDR-0019 and PDR-0021 lose their purchase wording. The proposal's prior-art
   paragraph (Standard.Licensing) now matches the chosen shape.

The assumption "no purchase limit per key" is moot under Q11.

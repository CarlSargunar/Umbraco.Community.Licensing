# Clean-project prompt: offline license key library for .NET

Extracted 2026-10-08 from `Umbraco.Community.Licensing`: the archived change
`2026-10-04-license-key-management` (PDR-0001 to PDR-0022, ADR-0001) and the
`one-key-per-product` change, using the shape settled in its design.md "Exploration pass
2026-10-06" (Q8 to Q23). That pass replaced the purchase-list shape (PDR-0037), which the old
project's PDRs, specs and examples still describe. Where the old records and this prompt
disagree, this prompt wins.

Everything below the line is the prompt.

---

## 1. What to build

A .NET 10 library that lets a software vendor issue license keys for its products and lets the
product check them **entirely offline**. Three capabilities:

| Capability | Side | Does |
|---|---|---|
| License generation | Vendor (issuer) | Checks a license's contents and signs them into a pasteable key string. Keeps no records |
| License validation | Product (consumer), at runtime | Evaluates a set of key strings for one product: each key's state, and the combined entitlement |
| Signing-key management | Both | Creates signing key pairs with derived IDs; holds a trusted set of public keys; supports rotation |

Context: the vendors are Umbraco package authors, but the library has **no dependency on
Umbraco or on any host**. It takes key strings, a product ID, a set of trusted public keys and
a clock, and returns results. Where keys are stored and how results are shown is the host's
concern (section 9).

## 2. Personas

Every requirement serves at least one persona and harms none. When interests conflict:
**site owner first, then implementor, then vendor.**

```
  VENDOR --issues key--> SITE OWNER --hands key--> IMPLEMENTOR --installs--> SITE
  (builds product)       (buys, pays)              (builds site, configures)    |
                                                                                v
                                              BACKOFFICE EDITOR, SITE VISITOR
                                              (use the product; never manage keys)
```

| Persona | Relationship to the license | Needs |
|---|---|---|
| Vendor | Issues keys, builds products, gates features | Keys survive email; one private key per product, never shipped; features usable as flags and limits (`max-orders`); decides what happens when a license fails |
| Site owner (primary) | Buys licenses from any channel (vendor store, marketplace, reseller) | What am I licensed for, what is about to expire, what is missing. Often not the person installing the key |
| Implementor | Installs keys, may look after many sites and environments | Which configured key is failing, why, where to fix it. Keys deployed through configuration, environment variables, vault secrets |
| Backoffice editor | Uses the product | May see warnings; must never read a full key (keys are bearer tokens) |
| Site visitor | Uses the public site | Never sees licensing. A licensing problem never crashes a page: the library reports, it never throws on a bad key |

## 3. Working rules for this project

- Record each decision with its reasons and rejected options. Product behaviour goes in a PDR
  (`docs/decisions/`), technology in an ADR (`docs/adrs/`). Test: if the stack changed, a PDR
  would still hold. The reasons are source material for the library's user documentation.
- Settle one open question at a time, with a recommendation. Lead with the simplest option
  that meets the stated criteria; give a worked example when the comparison is abstract.
- Keep a worked-examples document (`docs/license-examples.md`) in step with the decisions.
  An example that contradicts a decision is a documentation bug.
- Code: .NET 10, C# latest (file-scoped namespaces, primary constructors, records), xUnit,
  BCL only. Cross-platform: Windows, macOS, Linux. Node.js for any scripts; do not assume
  Python. Do not commit; the Product Owner commits.

## 4. Fixed technology decisions

Decided in the old project's ADR-0001. Do not reopen them without a new reason.

| Decision | Choice | Why |
|---|---|---|
| Signature | ECDSA P-256, SHA-256, 64-byte IEEE P1363 (`r‖s`), `System.Security.Cryptography.ECDsa` | BCL, short signatures. RSA: 256-byte signatures for no gain. Ed25519: needs a third-party library on some platforms |
| Envelope | Custom fixed-algorithm string, not JWT | No `alg` header to downgrade (`alg: none`); no dependency |
| Payload | UTF-8 JSON, no whitespace, `System.Text.Json`, read strictly | Readable when decoded; strict read means a future field is refused, not half-read |
| Clock | `TimeProvider` injected into issuer and evaluator | Expiry and ordering tests run against a fixed time |
| Packaging | One BCL-only package for all three capabilities | Nothing to split until host-side features return |

### 4.1 Key string

```
LIC-8F3AK-M7RXB-7Q2D.eyJzaWduaW5nS2V5SWQiOi...fX0.1AqpE9feDTqwkvVIdd4mRlzJEl...bULr_Q
|__________________| |_________________________| |_________________________|
 key identifier       base64url(payload)          base64url(signature), 86 chars
|_______________________________________________|
 signing input: ASCII bytes of everything before the last "."
```

- Three segments split on `.`; base64url per RFC 4648 section 5, no padding.
- The signing input includes the visible identifier, so editing it (even lowercasing) fails
  verification. Reference and key part live only in the identifier, not the payload.
- Duplicate means identical signing input (so ECDSA's `s` / `n - s` variants are duplicates).

### 4.2 Payload

```json
{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","issued":"2026-09-01T08:05:12Z",
 "expires":"2027-03-01","displayName":"Commerce Pro","vendorTag":"CUST-0042",
 "features":{"ecommerce":true,"max-orders":2500,"licensed-domains":["example.com","shop.example.com"]}}
```

| Field | Encoding | Absent |
|---|---|---|
| `signingKeyId` | string, 11 characters (4.3) | unreadable |
| `product` | string, product ID rule (5.1) | unreadable |
| `issued` | exactly `yyyy-MM-ddTHH:mm:ssZ`, UTC | unreadable |
| `expires` | exactly `yyyy-MM-dd` | perpetual. `null` unreadable |
| `displayName` | string, display name rule (5.4) | none. `null`, `""` unreadable |
| `vendorTag` | string, vendor tag rule (5.5) | none. `null`, `""` unreadable |
| `features` | object, name to value | none; `{}` also means none |

Feature type is the JSON token type:

| JSON | Feature type |
|---|---|
| `true` | switch |
| number | number |
| string | text |
| array of strings | text set |
| `false`, `null`, object, any other array | unreadable |

`displayName` and the text set array encoding are new since ADR-0001 and are proposed, not
confirmed (section 10).

**Numbers.** Grammar on issue and read: `0` or `[1-9][0-9]*`, optional `.` and 1 to 4 digits,
at most 15 digits total; no sign, exponent or leading zero. Issuer takes a `decimal`, strips
trailing fractional zeros, writes invariant culture (`2.50` → `2.5`). Reader checks the raw
token against the grammar, then reads `decimal`; never `double`.

**Strict read.** A verified key is *unreadable* if it has: an unknown field; a repeated field
or feature name (System.Text.Json does not detect these; the reader must); a date not in its
exact format; any value breaking a rule checked at issue. Unknown feature *names* are fine
(ignored at lookup). No version field: unknown fields being unreadable already guards format
changes.

**Vendor tag on read.** Check the decoded string against `^[A-Za-z0-9_.#/-]{1,64}\z` (ordinal;
`\z`, not `$`, which matches before a trailing `\n`). Escaped JSON forms of allowed characters
are accepted. The default System.Text.Json encoder escapes none of the allowed characters on
write.

### 4.3 Signing key ID

First 8 bytes of SHA-256 over the public key's SubjectPublicKeyInfo DER, base64url: 11
characters. Derived, never chosen: key-pair creation returns it, the trusted set computes it,
the issuer derives it from the private key. It cannot be paired with the wrong key.

### 4.4 Reading a supplied string

1. Remove all whitespace.
2. **Identifier**: the text before the first `.` (or the whole string), only if it fully
   matches `^LIC-[2-9A-HJKMNP-Z]{5}-[2-9A-HJKMNP-Z]{5}-[2-9A-HJKMNP-Z]{4}$`. Otherwise no
   identifier; the row is known by position. Later steps do not change it.
3. **Segments**: exactly three non-empty base64url segments; signature decodes to 64 bytes.
   Else unreadable.
4. **Routing claims**: payload is a JSON object with string `product` and `signingKeyId`. Else
   unreadable.
5. **Checks** in order: wrong product, signing key not recognised, not verified.
6. **Schema**: a verified key is checked against every issue rule; failure is unreadable.

| Supplied | Identifier | Outcome |
|---|---|---|
| `LIC-8F3AK-M7RXB-7Q2D.eyJzaWdu` (cut off) | `LIC-8F3AK-M7RXB-7Q2D` | unreadable |
| `LIC-8F3AK-M7R` | none | unreadable, position only |
| `lic-8f3ak-m7rxb-7q2d.eyJ...` (lowercased) | none | unreadable, position only |
| `Hunter2!` | none | unreadable, position only |

## 5. License contents and issue rules

A key is **one license for one product**: its identity, expiry and **one feature set**. There
is no purchase list and no base / add-on kind. Selling an add-on, more capacity or an upgrade
means reissuing the license under the same reference with the features edited.

```
  LICENSE KEY
  product        required          vendor.product
  reference      required          generated at first issue, kept on every reissue
  key part       set by the core   new at every issue
  issued         set by the core   UTC, to the second
  expires        stated            a date, or perpetual
  display name   optional          shown only
  vendor tag     optional          the vendor's label, e.g. a customer ID
  features       0..N              switch | number | text | text set
```

Never in a key: customer name, email, company or any personal data. This applies to the
display name and the vendor tag too.

### 5.1 Product ID

Two dot-separated parts of lowercase `a-z`, digits and hyphens, each starting with a letter:
`acme.commerce`. Guidance: the vendor part is something the vendor already owns uniquely
(domain, package-registry owner). It never changes once keys are issued.

### 5.2 License reference and key identifier

| Item | Rule |
|---|---|
| Alphabet | Uppercase letters and digits without `0 O 1 I L` (31 characters) |
| Reference | 10 random characters, shown `LIC-XXXXX-XXXXX` (`LIC-` is display only). Generated by the core at first issue; on reissue the caller passes it in, accepted ignoring case, hyphens and spaces (`lic-8f3ak m7rxb` → `LIC-8F3AK-M7RXB`); an invalid one is rejected |
| Key part | 4 random characters, new at every issue, never a caller input |
| Key identifier | Reference + key part, `LIC-8F3AK-M7RXB-7Q2D`, at the start of the key string as displayed |

The reference names one license. It is **not secret and not proof of ownership**. Uniqueness
comes from randomness (about 8 × 10¹⁴ combinations); the core keeps no records to check it.
The identifier grants and reveals nothing; it lets the implementor search their settings and
the site owner their emails for the key a result names.

### 5.3 Issue time and expiry

- `issued`: set by the core from the clock at signing, UTC, truncated to the second. Never a
  caller input. It only orders reissues; second precision means only a machine can produce a
  tie.
- `expires`: every request **states** an expiry: a date, or perpetual. Neither or both is
  rejected, so a forgotten date can never issue a lifetime key.
- A date is valid until the end of that day in UTC (`2027-03-01` → `2027-03-01T23:59:59Z`).
  A date before the current UTC date is rejected; today is accepted.
- One expiry per license. An add-on bought mid-term ends with the license. Adding features to
  an expired license requires the reissue to set a new expiry (a renewal plus the add-on).
- The library takes a date only. Terms, months and period arithmetic are vendor tooling.

### 5.4 Display name (optional)

1 to 64 characters; no leading or trailing whitespace, line breaks or control characters; one
plain string in the vendor's language; signed exactly as supplied, never trimmed (rejected
instead). Display only: no part in superseding, combining or feature lookup. Lets a generic
licensing screen show `Commerce Pro` instead of raw feature names, and tell two licenses apart.

### 5.5 Vendor tag (optional)

1 to 64 characters from `A-Z a-z 0-9 - _ . # /`; no spaces; case kept; signed exactly as
supplied; an invalid value is rejected, never cleaned. Not unique, never compared. A label
only: no effect on superseding, combining or gating. Reported for verified keys only.
Guidance: hold a **stable customer or account ID**, so every reissue of a license keeps the
same tag; the reference links the license's history in the vendor's records.

| Input | Result |
|---|---|
| `#1001`, `SHOP-2026-000123`, `pi_3NkX9a2eZvKYlo2C1`, `INV/2026/04` | accepted |
| `INV 2026 04` | rejected: space |
| `jane@acme.com` | rejected: `@` (and personal data) |
| empty | rejected: omit it instead |
| 65+ characters | rejected |

### 5.6 Features

A name and a typed value. Present means granted; absent means not granted; an unknown name is
ignored. Features never affect validity. "Tier" and "feature" are one concept: whether `pro`
is a bundle is a vendor convention. The library reports values; the product enforces them.

| Part | Rule |
|---|---|
| Name | Lowercase `a-z`, digits, hyphens; starts with a letter. At most once per key. Looked up ignoring case of `a`-`z`, culture-invariant |
| Switch | A plain name. Explicit `false` is rejected |
| Number | Zero or positive; at most 4 decimal places and 15 digits. An additive quantity (count, limit, amount), never a rate or factor: two licenses would sum `discount-rate: 0.15` to 0.30 |
| Text | Non-empty; at most 256 characters; no leading or trailing whitespace, line breaks or control characters. Never interpreted: `"500"` is text, never summed |
| Text set | One or more values, each following the text rules. Empty set rejected (omit the feature). The same value twice rejected, not deduplicated. Order has no meaning, but values are reported in the order issued. No count limit (guidance: about 25 key characters per 15-character domain; switch to a number such as `max-domains` when a set reaches hundreds) |

Why names are restricted: licenses combine, so `max-orders` and `Max-Orders` would split one
paid feature into two. Free-form names matched ignoring case fail on look-alike letters and on
culture-dependent case rules (Turkish i).

The vendor normalises values at issue (for example lowercases domains). Equality is always
exact: same characters, same case.

### 5.7 Issuing

- Input: product ID, optional reference, stated expiry, optional display name, optional
  vendor tag, features, private signing key. Returns the key string, reference, key identifier
  and issue time.
- The signing key ID is derived from the private key. The private key is not retained,
  cached or exposed after the call. The core keeps no record of anything it issued.
- A rejected request raises one error listing **every** problem, each naming its field
  (`expires`, `vendorTag`, `features.max-orders`, `features.licensed-domains[2]`). No key is
  produced.
- Key string: one line of printable characters with no whitespace. Typical keys are 250 to
  500 characters.

### 5.8 Reissue from the presented key

A vendor with no records adds to the key the site owner presents. The library builds a reissue
request from a **verified** license: product, reference, expiry (perpetual stated explicitly),
display name, vendor tag and features. The vendor edits any of them except the product, then
issues. Building it does nothing else: no clock, no checks. Issuing checks as usual, so a
prefilled past expiry is rejected, naming the expiry. A key that failed verification offers no
license to build from.

Why it is safe: the key is a bearer token, so its holder already holds everything it grants. A
reissue from it gives only that plus what was just bought. The reference alone is not proof.

Risk, handled by guidance only: a vendor reissuing from an old key (forwarded from an old
email) supersedes the newer key and drops features bought since. Guidance: reissue from the key
installed on the site; vendor records close the gap.

Indicative API (refine during design):

```csharp
var current = LicenseRequest.ReissueOf(row.License!);
var next = current with { Features = current.Features!.With("max-orders", 2500m) };
issuer.Issue(next, signingKey);
```

Request fields stay nullable so a missing value is reported as a problem, not a compile error.
Read-side types (`VerifiedLicense`) are separate, with non-nullable fields.

## 6. Evaluation

Input: an ordered list of zero or more key strings, one product ID, a trusted key set and a
clock. Output: one row per supplied string, in order, plus one product result. Never throws
because of a key's content (empty, truncated, garbage, altered).

### 6.1 One state per row, first failing check wins

```
  1  cannot be read                              -> unreadable
  2  claims another product                      -> wrong product
  3  signing key ID not in the trusted set       -> signing key not recognised
  4  signature does not verify                   -> not verified
  ---- verified from here: claims are facts ----
  5  exact copy of a key earlier in the list     -> duplicate (names the row it copies)
  6  same reference, another key issued later    -> superseded
  7  past its expiry                             -> expired
     otherwise                                   -> valid
```

Verified but breaking a schema rule (a faulty vendor tool) is *unreadable*, with its
identifier; the vendor must reissue.

Reasons name what was observed, not a presumed cause; "tampered" is never used:

| Reason | First action | Who |
|---|---|---|
| Unreadable | Paste the key again | Implementor |
| Wrong product | Move the key to the product it names | Implementor |
| Signing key not recognised | Update the product; then ask the vendor | Implementor, vendor |
| Not verified | Paste again; then ask the vendor | Implementor, vendor |
| Expired | Renew | Site owner |

Flags do not change a state and do not stop a key counting: *vendor error* (tied for latest,
6.3) and *conflict* (6.4).

### 6.2 What a row reports

| Row | Reports |
|---|---|
| Failed (1 to 4) | Reason; claimed product and claimed key identifier when readable, worded as claims. **Never** issue time, expiry, display name, vendor tag or features. Takes no part in superseding or combining |
| Verified (5+) | As facts: product, key identifier, issue time, expiry or perpetual, display name, vendor tag, features |

The rule: an unverified claim may identify a key; it may never describe or grant an
entitlement. No row or product result contains any part of a key string other than its
identifier.

### 6.3 Superseding, duplicates, ties

| Reference | Key | Issue time | Meaning | Result |
|---|---|---|---|---|
| different | - | - | two licenses | both count, combine |
| same | identical | same | one key supplied twice | duplicate, counts once |
| same | different | different | reissue | later supersedes |
| same | different | same, none later | two versions, order unknown | both count; each flagged *vendor error* naming the others |

- Only verified keys for the product being evaluated supersede or are superseded.
- Expiry plays no part: an expired key still supersedes older keys under its reference.
- A key supersedes only keys issued **strictly** earlier.
- The vendor-error flag applies only while keys are tied for latest.

### 6.4 Combining valid keys

The product is **licensed** when at least one row is valid. The result reports the **number
of licenses counted**: distinct references among valid rows (tied keys under one reference
count as one). "2 licenses" prompts the site owner to ask for a refund or a merge.

| Type | Between valid keys |
|---|---|
| Switch | Granted if any key grants it |
| Number | Summed exactly (decimal) |
| Text | Equal values count as one. Different values → conflict |
| Text set | Union, each value once |
| Mixed types under one name (text beside a switch, number or text set, etc.) | Conflict |

**Conflict**: the feature answers nothing at product level (not granted, no values, every
membership check answers no). The product result and every counted row carrying that name are
flagged. The rows stay valid; other features are unaffected.

**Text set read**: granted when at least one valid key carries it. Values are the union in
display order: the first valid key's values as issued, then new values from later valid keys
in supplied order. A membership check (`licensed-domains` contains `shop.example.com`?) uses
exact equality. Wildcards, `www.` variants and other matching are the vendor's code over the
listed values. An ignore-case check was rejected (a second equality rule; can be added later
compatibly, not removed).

**Order**: the product result does not depend on the order of the supplied strings, except
the display order of text set values.

**Not licensed**: every feature answers not granted.

### 6.5 Why combine rather than choose

A site owner who bought twice, or bought an add-on through a channel that could not reissue
the license, keeps what they paid for. Choosing one license by rule silently drops a paid
purchase; treating two licenses as a conflict stops a paid product.

## 7. Signing-key management

- Create a key pair: returns private key, public key and signing key ID, each exportable
  separately. The exported public key carries its ID, so a validator can trust it with no other
  input. The private key stays with the vendor and is never shipped in the product.
- Trusted set: a consumer holds several public keys addressed by ID. Adding a public key under
  an ID already held for a different key raises an error (configuration, not key content) and
  leaves the set unchanged.
- Rotation: trust the new key alongside the old; switch issuing to the new key; withdraw the
  old ID once no valid licenses depend on it. Withdrawn keys' licenses report *signing key not
  recognised*. Adding a key never affects licenses signed with an already-trusted key.

## 8. Worked examples

Now is 2026-10-01 unless stated. Contents are shown in schema form, not as key strings.

**1. Minimal.** `acme.seo-toolkit`, `LIC-4HN7T-QW2ZC`, perpetual, no features. Licensed; every
feature lookup answers no.

**2. Typical.** `acme.commerce`, `LIC-8F3AK-M7RXB`, expires 2027-03-01, display name
`Commerce Standard`, tag `CUST-0042`, `ecommerce`, `max-orders: 500`. Valid until
2027-03-01T23:59:59Z.

**3. Renewal left beside the old key.**
```
  LIC-8F3AK-M7RXB  issued 2025-03-01  expires 2026-03-01  max-orders 500   superseded
  LIC-8F3AK-M7RXB  issued 2026-03-01  expires 2027-03-01  max-orders 500   valid
  -> licensed, max-orders 500 (not 1000)
```

**4. Add-on by reissue; staging missed.** The site owner buys an extra 1,000 orders, then AI
Assist. Each time the vendor reissues from the presented key and edits the features:
```
  key 1  LIC-8F3AK-M7RXB-7Q2D  2026-03-01  ecommerce, max-orders 500
  key 2  LIC-8F3AK-M7RXB-K9PX  2026-06-15  ecommerce, max-orders 1500
  key 3  LIC-8F3AK-M7RXB-H8RC  2026-09-01  ecommerce, max-orders 1500, ai-assist
```
Staging still holds keys 1 and 3: key 1 superseded, key 3 valid; licensed with
`ecommerce, ai-assist, max-orders 1500`. A refund is a reissue with the feature removed; it
takes effect only when the new key is installed.

**5. Same key twice** (settings file and environment variable): row 2 *duplicate of row 1*;
counts once.

**6. Tie.** The vendor's system submits a reissue twice in the same second:
```
  LIC-8F3AK-M7RXB-D4WK  2026-09-28T14:30:22Z  max-orders 2000   valid  vendor error
  LIC-8F3AK-M7RXB-K9PX  2026-09-28T14:30:22Z  max-orders 2000   valid  vendor error
  -> licensed, max-orders 4000, 1 license counted
```
The extra 2000 is the vendor's cost for the vendor's mistake. A later reissue supersedes both
and clears the flag.

**7. Expired key still supersedes.** Vendor types `expires 2099-03-01`, reissues an hour later
with `2026-09-01`. The first is superseded, the second expired: not licensed. The correction
does not undo itself.

**8. Two licenses bought by mistake.** `LIC-8F3AK-M7RXB` and `LIC-C2D8R-XE6GU`, each
`max-orders 500`: both valid; `max-orders 1000`, 2 licenses counted.

**9. Text conflict between licenses.** One key `licensed-domain: example.com`, a second
license `licensed-domain: exmaple.com`: both valid and flagged; `licensed-domain` answers
nothing; numbers still sum.

**10. Text set union.** Key A `licensed-domains: [example.com, shop.example.com]`; key B
(separate license) `licensed-domains: [blog.example.com, example.com]`. Product:
`[example.com, shop.example.com, blog.example.com]`. Contains `shop.example.com`: yes;
`Shop.example.com`: no. Had key B carried `licensed-domain` as single text under the same
name as a text set, it would conflict.

**11. Adding to an expired license.** Prefilled expiry 2026-09-01 is rejected naming
`expires`; the vendor sets a new date. Perpetual licenses are unaffected.

**12. Failed keys.** Six keys for `acme.commerce`:
```
  1  LIC-8F3AK-M7RXB-7Q2D  valid   expires 2027-03-01  ecommerce, max-orders 500
  2  not verified                claims to be LIC-8F3AK-M7RXB-K9PX for acme.commerce   (paste lost a character)
  3  wrong product               claims to be LIC-5E3FT-BPZ7Y-2MHS for zenith.commerce-shipping
  4  unreadable                  claims to be LIC-2V5C9-HKD3P-X3BN                     (cut off by email)
  5  signing key not recognised  claims to be LIC-8F3AK-M7RXB-H8RC for acme.commerce   (new signing key, old product)
  6  not verified                claims to be LIC-8F3AK-M7RXB-P6TY for acme.commerce   (edited: expires 2099, max-orders 999999)
  -> licensed, ecommerce, max-orders 500
```
Key 6's claims appear nowhere. Failed keys sharing key 1's reference do not supersede it. A
key wrapped across lines by an email client, or with a trailing newline, is still valid.

**13. Trial.** No trial kind. A short expiry, optionally a `trial` switch the product checks.
Conversion is a reissue under the same reference.

**14. Separate add-on package.** `zenith.commerce-shipping` is its own product with its own
key; it never combines with `acme.commerce`.

**15. Rejected at issue** (on 2026-10-01; no key produced):

| Input | Reason |
|---|---|
| no expiry stated; or both a date and perpetual | Expiry must be stated, once |
| `expires 2026-09-30` | Before today UTC |
| `product commerce`, `product Acme.Commerce` | Product ID rule |
| reference with `O`, or wrong length | Reference rule |
| `issued` or key part supplied | Set by the core |
| display name `" Pro"`, with a line break, or 65 characters | Display name rule |
| vendor tag `INV 2026 04`, `jane@acme.com`, `""`, 65 characters | Vendor tag rule |
| `pro: false` | Absence means not granted |
| `max-orders: -200`, `"5OO"` as a number, `storage-gb: 2.12345` | Number rule |
| `max-orders` twice | Name at most once per key |
| `Max Orders: 500` | Name rule |
| `licensed-domain: ""`, `" example.com"`, text with a line break, 257 characters | Text rule |
| `licensed-domains: []` | Empty set: omit the feature |
| `licensed-domains: [a.com, a.com]` | Duplicate value in a set |
| customer name or email anywhere | No personal data |

Allowed: `expires` today; `licensed-domains: [a.com, A.com]` (different values under exact
equality).

## 9. Out of scope

**Inherent limits, documented, not mitigated:**
- No revocation before expiry (offline tokens). Shorter terms plus renewal instead.
- Clock rollback defeats expiry; the server owner controls the clock.
- Private key compromise: withdrawing its ID also invalidates every legitimate key it signed
  until reissued.
- No machine, domain or install binding. A text feature such as `licensed-domains` lets the
  vendor bind by convention; keys remain bearer tokens.
- No online validation or licensing server.

**Not expressible in this shape:** an add-on with a term of its own (monthly add-on on a
yearly license). One expiry per license.

**Future changes** (decided in the old project, deferred; restate under one key per product
when resumed):
- **Issuing add-on**: an optional local .NET tool for small vendors that keeps product
  definitions and issued-key records (no personal data; signing keys stored apart from data),
  issues, re-sends, renews and rotates. The core never depends on it. Its old decisions assumed
  one key per purchase (license types with a role, add-on records linked to a base, order
  reference as the vendor tag, add-on expiry aligned to base) and must be re-decided. Its
  records are what close the "reissued from an old key" gap (5.8).
- **Host-side (Umbraco)**: key sourcing from configuration, environment variables and Key
  Vault (Key Vault in a separate package so others avoid the dependency); one shared store per
  site for all vendors; package registration (product ID, display name, vendor, renewal link,
  required / optional); an inventory with one row per product, local-time expiry and advance
  warning; a view-only backoffice screen shipped once per site, never showing a full key;
  site labels per stored key; routing keys to products; dependencies between products; an
  Umbraco (or generic host) version range in the key. Leaning for storage: named entries,
  `Licensing:Keys:<name>`, one name per product.
- **Dropped, not deferred**: a trial / standard kind of license; release-date gating.

## 10. Open items for the new project

Technology (Architect):
1. Confirm the payload names `displayName` and `vendorTag`, and the text set as a JSON array
   of strings (4.2). Record in the ADR with the strict-read rules for arrays (non-string
   element, empty, duplicate value: unreadable).
2. Final name for the "text set" type in docs and API.
3. Public API surface: request and result types, how expiry is stated (for example
   `LicenseExpiry.On(date)` / `LicenseExpiry.Perpetual`), feature lookup methods per type,
   and the text-set membership check.
4. Package and namespace name. The library has no Umbraco dependency; decide whether
   `Umbraco.Community.Licensing` still fits.
5. Generate the worked examples' key strings from the library itself, with a throwaway key
   pair published in the ADR, and keep them as example tests.

Product (Analyst): none open. Raise new questions one at a time.

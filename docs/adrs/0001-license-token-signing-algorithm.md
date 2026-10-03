# 0001: License Key Signing Algorithm and Key String Format

## Status

Decided, 2026-10-03. Revised from the 2026-09 draft for the visible key identifier (PDR-0020); the payload is now fixed. Source: `openspec/changes/license-key-management/design.md`, Technical open questions.

## Context

The `license-key-management` change (`openspec/changes/license-key-management/`) needs a license key that is validated fully offline, is tamper-evident and supports signing key rotation. This needs a signing algorithm and a key string format.

The format must meet:

- **Visible key identifier** (PDR-0020, PDR-0017): the key string starts with `LIC-XXXXX-XXXXX-XXXX` as displayed; an edited identifier fails verification; the identifier is readable when the rest of the key is cut off.
- **Reading** (PDR-0021): all whitespace is removed before reading; a key that verifies but breaks a rule checked at issue is unreadable.
- **Failure order** (PDR-0019): unreadable, wrong product, signing key not recognised, not verified. Product and signing key ID must be readable before the signature is checked.
- **Contents** (`docs/license-examples.md` schema): product, role, reference, key part, issue time to the second (PDR-0016), optional expiry date, typed features (PDR-0010, PDR-0013, PDR-0018). Numbers are summed exactly (PDR-0010).
- **Signing key ID** (`signing-key-management` spec): names a signing key pair; the exported public key carries it; a trusted set rejects one ID for two different public keys.

Terms: the **signing key ID** names a signing key pair. The **key identifier** names one license key.

Personas affected (`docs/personas.md`):

- **Vendor**: signs keys with a private key per product and ships the matching public key inside the product.
- **Site owner**: receives the key, typically by email, and can tell keys apart by their identifier.
- **Implementor**: pastes the key into configuration, an environment variable or a vault secret, and searches for the identifier a result row names.

## Decision

### Algorithm

ECDSA on P-256 with SHA-256 (`System.Security.Cryptography.ECDsa`). The signature is the 64-byte IEEE P1363 form (`r || s`), the .NET default for `SignData`. The format has no algorithm field: the verifier always uses ECDSA P-256, so there is nothing for an attacker to negotiate.

### Key string

```
LIC-8F3AK-M7RXB-7Q2D.eyJzaWduaW5nS2V5SWQiOi...ifX0.VCJLlV_p20on8qahy5wAX4mY5A...QaOsTA
|__________________| |_________________________| |_________________________|
 key identifier       base64url(payload)          base64url(signature), 86 chars
|_______________________________________________|
 signing input: ASCII bytes of everything before the last "."
```

- Three segments separated by `.`. `.` occurs in neither the identifier nor base64url.
- base64url is RFC 4648 section 5 without padding.
- The signing input is `identifier + "." + base64url(payload)`, so the visible identifier is bound to the signature. Any edit to it, including lowercasing or moving a hyphen, fails verification.
- The license reference and key part exist only in the identifier. The payload does not repeat them.
- "Identical signed contents" (the duplicate rule, PDR-0009) means identical signing input. Two strings that differ only in their signature, such as ECDSA's `s` and `n - s` forms, are duplicates.

### Payload

UTF-8 JSON, no whitespace. Field order as written by the issuer is not significant to the reader.

```json
{"signingKeyId":"4cJP_ZHe37U","product":"acme.commerce","role":"base",
 "issued":"2026-03-01T09:14:22Z","expires":"2027-03-01",
 "features":{"ecommerce":true,"max-orders":500,"storage-gb":2.5,"licensed-domain":"example.com"}}
```

| Field | Encoding | Absent |
|---|---|---|
| `signingKeyId` | string (see Signing key ID) | unreadable |
| `product` | string, PDR-0017 product ID rule | unreadable |
| `role` | `"base"` or `"add-on"` | unreadable |
| `issued` | string, exactly `yyyy-MM-ddTHH:mm:ssZ`, UTC | unreadable |
| `expires` | string, exactly `yyyy-MM-dd` | never expires. `null` is unreadable |
| `features` | object, feature name to value | no features; `{}` also means none |

**Feature type is the JSON token type:**

| JSON value | Feature type |
|---|---|
| `true` | switch |
| number | number |
| string | text |
| `false`, `null`, object, array | unreadable |

`"500"` is text and `500` is a number; text is never read as a number (PDR-0018).

**Numbers** (PDR-0010):

- Grammar, on issue and on read: `0` or `[1-9][0-9]*`, optionally `.` and 1 to 4 digits; at most 15 digits in total. No sign, no exponent, no leading zero.
- The issuer passes a `decimal`, removes trailing fractional zeros, checks the rules, and writes it in invariant culture: `2.50` is written `2.5`, `3.0` is written `3`, and `2.12340` is accepted as `2.1234`.
- The reader checks the raw token text against the grammar before parsing, then reads it as `decimal`. It never goes through `double`. `decimal` holds 28 to 29 significant digits, so values and sums are exact.

**Read strictly.** All of these make a verified key unreadable (PDR-0021):

- an unknown top-level field;
- a date not in its exact format;
- a repeated feature name. System.Text.Json does not reject duplicate properties, so the reader detects them; names are lowercase by rule, so an exact comparison suffices;
- any other rule checked at issue.

Unknown feature *names* are not an error; they are ignored at lookup (PDR-0010). No format version field: rejecting unknown fields means a future format that adds a field is refused by older readers rather than half-read, and its absence means this format.

### Signing key ID

The first 8 bytes of SHA-256 over the public key's SubjectPublicKeyInfo DER (`ECDsa.ExportSubjectPublicKeyInfo()`), base64url-encoded: 11 characters, e.g. `4cJP_ZHe37U`.

- Derived, not chosen. Key pair creation returns it. The trusted set computes it from each public key added. Generation computes it from the private key, so the caller supplies only the private key.
- A trusted set holds one vendor's keys for one product, in practice a handful. At 64 bits the collision chance even at 1,000 keys is about 3 × 10⁻¹⁴. The duplicate-ID rejection in the `signing-key-management` spec still applies, and can only fire on such a collision.

### Reading a supplied string

1. Remove all whitespace (PDR-0021).
2. **Identifier.** Take the text before the first `.`, or the whole string if there is none. It is the identifier only if it fully matches `^LIC-[2-9A-HJKMNP-Z]{5}-[2-9A-HJKMNP-Z]{5}-[2-9A-HJKMNP-Z]{4}$`: exactly 20 characters, uppercase as issued, from the 31-character alphabet (PDR-0017). Otherwise there is no identifier, and the row is identified by position (PDR-0020). Steps 3 to 6 do not affect it.
3. **Segments.** The string must have exactly three non-empty segments of valid base64url. The signature segment must decode to exactly 64 bytes. Otherwise the key is unreadable. A key cut off anywhere after its identifier ends here, with its identifier reported.
4. **Routing claims.** Decode the payload. It must be a JSON object with string `product` and `signingKeyId`. Otherwise the key is unreadable.
5. **Checks** in PDR-0019 order: wrong product, signing key not recognised (lookup by `signingKeyId`), not verified (ECDSA over the signing input).
6. **Schema.** A verified key's contents are checked against every rule checked at issue, using the reading rules above. A key that fails is unreadable (PDR-0021).

| Supplied (whitespace removed) | Identifier | Outcome |
|---|---|---|
| `LIC-8F3AK-M7RXB-7Q2D.eyJ...fX0.VCJ...sTA` | `LIC-8F3AK-M7RXB-7Q2D` | continues to step 4 |
| `LIC-8F3AK-M7RXB-7Q2D.eyJzaWdu` (cut off) | `LIC-8F3AK-M7RXB-7Q2D` | unreadable |
| `LIC-8F3AK-M7RXB-7Q2D` (cut at the dot) | `LIC-8F3AK-M7RXB-7Q2D` | unreadable |
| `LIC-8F3AK-M7R` (cut inside the identifier) | none | unreadable, position only |
| `lic-8f3ak-m7rxb-7q2d.eyJ...` (lowercased) | none | unreadable, position only |
| `Hunter2!` | none | unreadable, position only |

### Worked example

Generated with a real P-256 key pair. The key strings are wrapped here for display; issued keys are one line.

Minimal license (`docs/license-examples.md` example 1), 258 characters:

```
LIC-4HN7T-QW2ZC-9KXM.eyJzaWduaW5nS2V5SWQiOiI0Y0pQX1pIZTM3VSIsInByb2R1Y3QiOiJhY21lLnNlby10
b29sa2l0Iiwicm9sZSI6ImJhc2UiLCJpc3N1ZWQiOiIyMDI2LTAxLTEwVDE0OjAyOjM3WiJ9.qj_XsW8ca-fwi48R
uAGN0LyI-NiId_zO4Y49N1kbt1xrYZM7sSq75of41sgkyWNoEaf27p4igI27tln-D0TQsA
```

Payload: `{"signingKeyId":"4cJP_ZHe37U","product":"acme.seo-toolkit","role":"base","issued":"2026-01-10T14:02:37Z"}`

Base license with expiry and all three feature types, 403 characters:

```
LIC-8F3AK-M7RXB-7Q2D.eyJzaWduaW5nS2V5SWQiOiI0Y0pQX1pIZTM3VSIsInByb2R1Y3QiOiJhY21lLmNvbW1l
cmNlIiwicm9sZSI6ImJhc2UiLCJpc3N1ZWQiOiIyMDI2LTAzLTAxVDA5OjE0OjIyWiIsImV4cGlyZXMiOiIyMDI3
LTAzLTAxIiwiZmVhdHVyZXMiOnsiZWNvbW1lcmNlIjp0cnVlLCJtYXgtb3JkZXJzIjo1MDAsInN0b3JhZ2UtZ2Ii
OjIuNSwibGljZW5zZWQtZG9tYWluIjoiZXhhbXBsZS5jb20ifX0.VCJLlV_p20on8qahy5wAX4mY5Apn_gYfgdX0t
QO0_RLQIejISBxLPOTwZkK8AkoYMXbCvXLJL330hMGwQaOsTA
```

## Alternatives Considered

**Algorithm and format**

- **RSA (2048/4096-bit):** In the BCL like ECDSA, but RSA-2048 signatures are 256 bytes against 64, for no security benefit at this threat level. Longer keys. Not chosen.
- **Ed25519:** Safe by design, but cross-platform built-in .NET support has been inconsistent, typically needing BouncyCastle or a libsodium binding. Not chosen, to stay BCL-only.
- **Generic JWT via a JWT library:** The `alg` header invites downgrade and confusion attacks (`alg: none`), and adds a dependency for interoperability nothing needs. Not chosen.

**Binding the identifier**

- **Identifier also in the payload, verifier checks the two copies match:** a second copy, a consistency rule and its own failure case, and about 20 characters longer. Signing the prefix already binds it.
- **Identifier as an unsigned label:** fails "edited identifier is not verified" (`license-generation` spec).

**Locating the identifier**

- **Case-insensitive identifier read, reported uppercase:** names a hand-lowercased key, but adds normalisation for a key that cannot verify anyway.
- **Identifier found anywhere in the string:** could match inside base64url and report a wrong identifier.

**Numbers**

- **JSON string `"2.5"`:** exact everywhere, but numbers look like text and the type would need a separate tag.
- **Scaled integer (2.5 as `25000` ten-thousandths):** unreadable when decoded. The maximum, 999 999 999 999 999 × 10⁴, overflows `long`.
- **Default JSON number parsing:** `double` can enter (0.1 + 0.2 ≠ 0.3), and `5e2` is accepted.

**Payload**

- **Issue time as Unix seconds:** 10 characters shorter, unreadable when decoded.
- **Short property names (`p`, `iat`, `exp`):** save about 40 characters on about 300. Length is not a criterion; readability when decoded is worth more.
- **Features as `[{name, type, value}]`:** an explicit tag duplicating the JSON type, about twice as long, and allows a type and value that disagree.
- **Separate `switches` / `numbers` / `texts` fields:** three places to check for a name repeated across them.
- **Ignore unknown fields:** an older reader would ignore a future restricting field and grant more than sold.
- **`expires: null` for never:** a second way to say never.

**Signing key ID**

- **Label assigned by the issuer (`"2026-q1"`):** readable, but needs format rules and an input; uniqueness is a convention (two keys labelled `v1`); can be exported with the wrong key.
- **Full SHA-256 (43 characters):** 32 characters longer in every key, no gain at this set size.
- **RFC 7638 JWK thumbprint:** the same idea standardised, but needs JWK canonical JSON. Nothing outside this library reads the ID.
- **Random ID at key creation:** must be stored and exported beside the key, and can be paired with the wrong one.

## Consequences

- Verification needs no third-party package; `System.Security.Cryptography` and `System.Text.Json` are sufficient on every .NET 10 platform.
- Key strings are single-line, about 260 to 400 characters for typical contents, and survive email wrapping because whitespace is removed on read.
- Anyone can base64url-decode the payload and read the claims. They are not secret (PDR-0006 keeps personal data out). Only this library, or a compatible decoder, can verify them.
- The reader needs its own number grammar check and duplicate-name check; the JSON parser does neither.
- Adding any top-level field later is a breaking change for older readers by design. A new feature *name* is not.
- The `license-generation` spec takes the private key only. The signing key ID is derived from it.
- Rotation needs no format change: the trusted set holds several public keys, each addressed by its derived ID.

## Reversal Cost

High after the first release. Changing the algorithm, the segment layout, the signing input, a field name or encoding, or the signing key ID derivation breaks every issued key. The validator would have to read both formats during a transition, or every vendor would reissue every key to every site owner. Nothing is issued yet, so the cost today is an edit to this record.

## Context

See `proposal.md` for motivation and scope. Greenfield: no existing code or specs. Constraints:

- Validates entirely offline. No licensing server.
- Signing key rotation from day one: a signing key ID is embedded in every key.
- No dependency on Umbraco or on any host application. The library takes license key strings,
  a product ID, a set of trusted public keys and a clock, and returns results. Host-side scope
  is parked in [`docs/deferred-scope.md`](../../../docs/deferred-scope.md).
- Machine/domain binding and revocation before expiry are out of scope.
- Target: .NET 10, BCL only.

Personas: [`docs/personas.md`](../../../docs/personas.md); the site owner is the primary
customer. **Issuer** is the vendor issuing licenses; **consumer** is the vendor's product
calling the library at runtime.

## Goals / Non-Goals

**Goals:**

- A concrete signing algorithm and key string format, so the specs have one wire format.
- Deterministically testable validation: expiry, superseding, combining.
- A safe way for the issuer to create, identify and rotate signing keys.

**Non-Goals:**

- The public C# API surface. Left to implementation, constrained by the specs.
- NuGet publishing pipeline.
- Anything in `docs/deferred-scope.md`.

## Decisions

### Signing algorithm and key string format

ECDSA P-256 with SHA-256 from the BCL, in a custom fixed-algorithm key string rather than a
generic JWT. Rationale, alternatives and the reading algorithm:
[ADR-0001](../../../docs/adrs/0001-license-token-signing-algorithm.md) (decided 2026-10-03).

```
LIC-8F3AK-M7RXB-7Q2D . base64url(payload) . base64url(signature)
|__________________|   |_________________|
 key identifier          payload JSON
|______________________________________|
 signed: ASCII bytes before the last "."
```

| Question | Decision |
|---|---|
| Binding the key identifier (PDR-0020) | The signing input is `identifier + "." + base64url(payload)`. Reference and key part exist only in the identifier |
| Telling the identifier from base64url | Split on `.`; the first segment is the identifier only if it fully matches the 20-character uppercase pattern (PDR-0017). Otherwise position only |
| Numbers (PDR-0010) | Plain JSON number, grammar `0` or `[1-9][0-9]*`, optional `.` and 1 to 4 digits, at most 15 digits; no sign or exponent. Issuer strips trailing fractional zeros; reader checks the grammar on the raw token, then reads `decimal` |
| Payload fields | `signingKeyId`, `product`, `role`, `issued` (`yyyy-MM-ddTHH:mm:ssZ`), `expires` (`yyyy-MM-dd`, absent = never), `features` (object; JSON type gives the feature type: `true` switch, number, string text). Unknown top-level fields, repeated feature names and inexact dates are unreadable (PDR-0021). No version field |
| Signing key ID | First 8 bytes of SHA-256 over the public key's SubjectPublicKeyInfo DER, base64url: 11 characters. Derived, never chosen |

### Signing key ID and rotation

Every key embeds the signing key ID of the key pair that signed it. The ID is derived from the
public key (above), so key pair creation, the trusted set and generation all compute it; the
vendor never supplies it, and it cannot be paired with the wrong key. The validator holds a set
of trusted public keys addressed by signing key ID. Rotation: trust the new key alongside the
old, switch generation to the new key, and later withdraw the old signing key ID once no valid
licenses depend on it.

**Why:** The signing key ID is ordinary payload data, so rotation needs no wire-format change.

### Clock access is injectable

Expiry checks and the issue time use `TimeProvider` (BCL) rather than `DateTime.UtcNow`, so
expiry and superseding scenarios are testable against a fixed time.

### One package

Generation, validation and signing-key management ship in one BCL-only package.
[ADR-0002](../../../docs/adrs/0002-package-split-for-keyvault-dependency.md) (a split for the
Key Vault provider) is deferred with key sourcing.

## Risks / Trade-offs

- **No revocation before expiry.** Inherent to offline tokens. Documented; issuers wanting
  revocation use shorter expiry plus renewal.
- **Clock rollback defeats expiry.** Whoever runs the server controls its clock. Documented,
  not mitigated.
- **Private key compromise.** Withdrawing the signing key ID stops trusting it going forward, but also
  invalidates every legitimate license signed with it until reissued. Inherent to offline
  verification; documented.
- **No machine/domain binding.** A valid key works on any install. Out of scope.

## Migration Plan

None. Greenfield.

## Technical open questions

None. Settled 2026-10-03 in ADR-0001: the binding of the key identifier, how it is told apart
from base64url, the number encoding, the payload field names and encodings, and the signing
key ID derivation. The `license-generation` spec no longer takes the signing key ID as an
input; it is derived from the private key.

---

## Requirements and open questions

Product decisions are PDRs in [`docs/decisions/`](../../../docs/decisions/README.md); the key
schema and worked examples are in [`docs/license-examples.md`](../../../docs/license-examples.md).
This section indexes the requirements and questions those records cite, and holds the open
ones. Numbers that left the change on 2026-10-01 (R1 to R4, R9, R13; Q1 to Q4, Q6, Q7, Q8 in
part, Q10 in part, Q12, Q14, Q17) are indexed in `docs/deferred-scope.md`. The pre-cut text is
at commit `fcda40b`.

The three delta specs and `tasks.md` still describe a single key with product ID and expiry.
Nothing is implemented, so revising them against the PDRs is a rewrite, not a migration.

### Requirements

| # | Requirement | State | Recorded |
|---|---|---|---|
| R5 | Feature flags | superseded by R7 | |
| R6 | Kind of license (trial / standard) | dropped | PDR-0014 |
| R7 | Product features: switches, numbers and text | decided | PDR-0010, PDR-0013, PDR-0018 |
| R8 | Release-date gating | dropped | PDR-0015 |
| R10 | Optional issuing add-on for smaller vendors | decided; separate change | PDR-0005, PDR-0006, PDR-0007 |
| R11 | The library returns results and never throws on a bad key, so a licensing problem never crashes a page | in `license-validation` | [`docs/personas.md`](../../../docs/personas.md), PDR-0001 |
| R12 | Evaluation result: for one product and a set of keys, each key's state and the combined entitlement | specified in `license-validation` | PDR-0009, PDR-0011, PDR-0010, PDR-0018, PDR-0019, PDR-0020, PDR-0021; below |

### Questions

| Q | Question | Outcome |
|---|---|---|
| Q5 | What does the evaluation result report for a key that fails verification? | PDR-0019, PDR-0020 |
| Q8 | The license reference | PDR-0009 |
| Q9 | Feature model: explicit list or named tier? | PDR-0010: one concept, a vendor convention |
| Q11 | Feature name rules | PDR-0013 |
| Q13 | What one key stands for | PDR-0008: one purchase |
| Q15 | What the issuing add-on records | PDR-0006 |
| Q16 | How licenses for one product combine | PDR-0011, PDR-0010, PDR-0018 |
| Q18 | Same reference, different role or product; which keys may supersede | PDR-0009 (amended 2026-10-01) |
| Q19 | The same key supplied twice; different keys with one reference and one issue time | PDR-0009 (amended 2026-10-02), PDR-0016 |
| Q20 | Whitespace in a supplied key; a key that verifies but whose contents break the schema | PDR-0021 |

### R12. Evaluation result

`license-validation` takes **a set of keys and one product ID** and returns one row per
supplied key, in the order supplied, and the combined entitlement for the product (PDR-0011,
PDR-0010, PDR-0018).

- Each row names its key by key identifier (PDR-0020) and gives one state: unreadable, wrong
  product, signing key not recognised, not verified, duplicate, superseded, expired, inactive
  or valid. The first failing check decides (PDR-0019).
- A row may also carry a flag, which does not stop the key counting: vendor error on keys tied
  for latest (PDR-0009), conflict on a text feature (PDR-0018).
- A verified key's row carries its claims as facts. A key that failed verification carries its
  reason and its claimed product and identifier only (PDR-0019).
- A superseded row notes when its role differed from the key that superseded it (PDR-0009).
- No row contains any part of a key other than its identifier (PDR-0020).

Verification and usability are independent axes. A key can be verified and expired; another
can fail verification. One status word loses the difference between "this key is not
genuine or not intact" and "you need to renew", which call for different actions.

```
                           VERIFIED?
                      no              yes
                +---------------+---------------+
     USABLE  no | reason and    | duplicate,    |
                | claimed       | superseded,   |
                | identifiers   | expired,      |
                |               | inactive      |
                +---------------+---------------+
             yes|     n/a       |    valid      |
                +---------------+---------------+
```

### Q5. What does the result report for a key that fails verification? (settled 2026-10-02)

Settled in three parts. The records hold the reasons and the rejected options.

| Part | Outcome | Record |
|---|---|---|
| Which claims a failed key reports | Its reason plus claimed product and key identifier. An unverified claim may identify a key, never describe or grant an entitlement | [PDR-0019](../../../docs/decisions/0019-failed-key-reporting.md) |
| The failure reasons | Unreadable, wrong product, signing key not recognised, not verified, expired. One per key, first failing check wins. "Tampered" is no longer used | PDR-0019; PDR-0011 amended |
| Matching a row back to its key | Every key starts with a visible key identifier, the reference plus a 4-character key part. Position is the fallback | [PDR-0020](../../../docs/decisions/0020-visible-key-identifier.md); PDR-0017 amended |

Worked example: `docs/license-examples.md` example 18.

Specified in `license-validation` and `license-generation` (2026-10-02). The
key identifier's place in the key string, and its binding to the signature, are in ADR-0001
(Decisions, "Signing algorithm and key string format").

### Q18. Same reference, different role or product (settled 2026-10-01)

Settled by the amendment to
[PDR-0009](../../../docs/decisions/0009-license-reference.md), which holds the reasons and the
rejected options. Superseding considers only verified keys for the product being evaluated.
Among those, the latest issued wins regardless of expiry or role, and the evaluation result
(R12) reports when a superseded key carried a different role. Worked examples:
`docs/license-examples.md` examples 5 and 6.

Specified in `license-validation` (2026-10-02).

### Q19. The same key twice, and keys issued at the same time (settled 2026-10-02)

Raised on 2026-10-01 while settling Q18: PDR-0009 said the latest issued supersedes, and said
nothing when no key is later. Settled by a second amendment to PDR-0009, which holds the
reasons and the rejected options.

| Case | Outcome |
|---|---|
| The same key supplied more than once | Counts once. Each further copy is reported as *duplicate* |
| Different keys, one reference, one issue time, none later | Two versions of one license with the order unknown. Both count and combine as usual; each is flagged as a vendor error. The cost falls on the vendor, whose mistake it is, and the site keeps operating |
| How fine the issue time is | To the second (PDR-0016), so an honest correction never ties |

Worked example: `docs/license-examples.md` example 7.

Specified in `license-validation` and `license-generation` (2026-10-02).

### Q20. Whitespace, and verified keys with bad contents (settled 2026-10-02)

Raised while rewriting the `license-validation` spec. Settled by
[PDR-0021](../../../docs/decisions/0021-reading-a-key-string.md): all whitespace in a supplied
key string is removed before reading; a key that verifies but breaks a rule checked at issue is
reported as *unreadable* (provisional).

### Prior art: Standard.Licensing

[junian/Standard.Licensing](https://github.com/junian/Standard.Licensing) v1.3.0, MIT, reviewed
as a feature comparison, not as a dependency. Its product features, unique license ID and
"all failures returned together" influenced R7, PDR-0009 and R12. Its license type and
build-date validation were considered and dropped (PDR-0014, PDR-0015); licensee details in the
key were rejected (PDR-0006). Deliberately not repeated:

- A claim that is signed but never checked (its `Quantity`). Every claim here has a defined
  check, or a clear statement that the library only reports it.
- Multi-line XML keys. Hard to put in one config value; reformatting breaks the signature.
- Expiry compared against the machine's local time. Both dates here are UTC (PDR-0016).

### Where to resume

No product question is open in this change.

1. Delta specs and `tasks.md` were revised against PDR-0001 to PDR-0021 on 2026-10-02.
2. Propose phase: ADR-0001 revised for the visible key identifier and its payload fixed
   (2026-10-03). Next: `opsx:apply`.

`docs/license-examples.md` was renumbered on 2026-10-02. Example numbers in commits before
that date differ; the mapping is at the top of that file.

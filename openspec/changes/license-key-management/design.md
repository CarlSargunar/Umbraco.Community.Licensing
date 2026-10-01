## Context

See `proposal.md` for motivation and scope. Greenfield: no existing code or specs. Constraints:

- Validates entirely offline. No licensing server.
- Key rotation from day one: a key ID is embedded in every token.
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

- A concrete signing algorithm and token format, so the specs have one wire format.
- Deterministically testable validation: expiry, superseding, combining.
- A safe way for the issuer to create, identify and rotate signing keys.

**Non-Goals:**

- The public C# API surface. Left to implementation, constrained by the specs.
- NuGet publishing pipeline.
- Anything in `docs/deferred-scope.md`.

## Decisions

### Signing algorithm and token format

ECDSA P-256 from the BCL, and a custom fixed-algorithm compact token
(`base64url(payload) + "." + base64url(signature)`) rather than a generic JWT. Rationale and
alternatives: [ADR-0001](../../../docs/adrs/0001-license-token-signing-algorithm.md) (draft).
The payload carries the key schema in
[`docs/license-examples.md`](../../../docs/license-examples.md) plus `keyId`.

### Key ID and rotation

Every token embeds a short `keyId` chosen by the issuer at generation. The validator holds a
set of trusted public keys addressed by `keyId` (conceptually
`IReadOnlyDictionary<string, ECDsa>`). Rotation: trust the new key alongside the old, switch
generation to the new key, and later withdraw the old key ID once no valid licenses depend on it.

**Why:** The key ID is ordinary payload data, so rotation needs no wire-format change later.

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
- **Private key compromise.** Withdrawing the key ID stops trusting it going forward, but also
  invalidates every legitimate license signed with it until reissued. Inherent to offline
  verification; documented.
- **No machine/domain binding.** A valid key works on any install. Out of scope.

## Migration Plan

None. Greenfield.

## Technical open questions

- `keyId` derivation (truncated hash of the public key vs an issuer-assigned label). Either
  satisfies the specs; decide during implementation.

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
| R12 | Evaluation result: for one product and a set of keys, each key's state and the combined entitlement | decided, not yet specified | PDR-0009, PDR-0011, PDR-0010, PDR-0018; below |

### Questions

| Q | Question | Outcome |
|---|---|---|
| Q5 | What does the evaluation result report for a key that fails verification? | open, below |
| Q8 | The license reference | PDR-0009 |
| Q9 | Feature model: explicit list or named tier? | PDR-0010: one concept, a vendor convention |
| Q11 | Feature name rules | PDR-0013 |
| Q13 | What one key stands for | PDR-0008: one purchase |
| Q15 | What the issuing add-on records | PDR-0006 |
| Q16 | How licenses for one product combine | PDR-0011, PDR-0010, PDR-0018 |
| Q18 | Same reference, different role or product | open, below |

### R12. Evaluation result

`license-validation` takes **a set of keys and one product ID** and returns each key's role
and state (valid, inactive, expired, superseded, invalid; PDR-0011) and the combined
entitlement for the product (PDR-0011, PDR-0010, PDR-0018). Keys for another product are
reported as wrong product and take no further part.

Authenticity and usability are independent axes. A key can be authentic and expired; another
can be forged. One status word loses the difference between "someone tampered with this" and
"you need to renew", which call for different actions.

```
                          AUTHENTIC?
                      no              yes
                +---------------+---------------+
     USABLE  no | broken /      | expired       |
                | untrusted     |               |
                +---------------+---------------+
             yes|     n/a       |    active     |
                +---------------+---------------+
```

### Q5. What does the result report for a key that fails verification? (open)

A key that does not verify still has readable claims. They are assertions, not facts:
"expires 2099" on a forged key must never be returned as if true. Open: whether the result
carries the reason only, or also the readable claims with an explicit "unverified" marker. Keys
are bearer tokens, so the result must not reproduce a key in full; the reference (PDR-0009) or
a short fragment is enough to match a result back to a key.

### Q18. Same reference, different role or product (open)

Superseding is by license reference: latest issued wins (PDR-0009). Two cases are undefined.

- **Same reference, different role.** The core keeps no records (PDR-0005), so nothing stops a
  vendor reissuing an add-on under a base's reference, or the reverse. Latest-wins then
  replaces a base with an add-on (the product stops being licensed) or an add-on with a base
  (the site owner gets a product they did not buy). Options: latest wins regardless; the
  result treats the pair as a conflict; the issuing add-on (PDR-0006) refuses a reissue that
  changes the role and vendors with their own systems are told to do the same, which follows
  "mistakes fail at the vendor" (PDR-0010, PDR-0011) but cannot be enforced by the core.
- **Same reference, different product.** References are random per product (PDR-0017), so two
  vendors can generate the same one. Superseding must be scoped by product **and** reference.
  PDR-0009 says "unique per product", which implies this; stating it outright would close
  this half.

Touches: PDR-0009, `license-validation`, `docs/license-examples.md` examples 3 and 4.

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

1. **Q18**: the superseding edge cases. Changes `license-validation` text.
2. **Q5**: what the result reports for an unverifiable key; then R12 can be specified.
3. Revise the three delta specs and `tasks.md` against the PDRs and `docs/license-examples.md`
   (role, reference, issue time, features, combining), and fix ADR-0001's payload.

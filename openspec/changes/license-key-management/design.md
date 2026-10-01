## Context

See `proposal.md` - Why / What Changes for motivation and scope. This is a greenfield library (no existing code or specs to integrate with). Key constraints carried from the proposal:
- Must validate entirely offline (no licensing server dependency).
- Must support key rotation from day one (key ID embedded in the token).
- Machine/domain binding and revocation-before-expiry are explicitly out of scope.
- Target: .NET 10. No dependency on Umbraco or on any host application.

**Scope, reduced on 2026-10-01.** The change covers generation, verification and signing-key management only. Host-side and Umbraco-specific scope (key sourcing, shared store, package registration, inventory, backoffice screen, Umbraco version range) is parked in [`docs/deferred-scope.md`](../../../docs/deferred-scope.md), which also keeps the design notes for those items. The library's boundary: it takes license key strings, a product ID, a set of trusted public keys and a clock, and returns results.

Personas are defined in [`docs/personas.md`](../../../docs/personas.md); the site owner is the primary customer. Terms used below: **issuer** is the vendor in its license-issuing role; **consumer** is the vendor's product calling the library at runtime; **host application** is the site as software, which the implementor configures and which is outside this change.

## Goals / Non-Goals

**Goals:**
- Pick a concrete signing algorithm and token format, with rationale, so `license-generation` and `license-validation` have an unambiguous wire format to implement against.
- Keep validation logic (expiry, superseding, combining) deterministically testable.
- Give the issuer a safe way to create, identify and rotate signing keys.

**Non-Goals:**
- Defining the full public C# API surface (interfaces/method signatures) - left to implementation, constrained only by the specs.
- NuGet packaging/publishing pipeline details.
- Anything in `docs/deferred-scope.md`.

## Decisions

### Signing algorithm and token format

ECDSA P-256 from the BCL, and a custom fixed-algorithm compact token
(`base64url(payload) + "." + base64url(signature)`) rather than a generic JWT. Rationale and
alternatives: [ADR-0001](../../../docs/adrs/0001-license-token-signing-algorithm.md) (draft).
The payload carries the key schema in
[`docs/license-examples.md`](../../../docs/license-examples.md) plus the signing key's
identifier (`keyId`, below).

### Key ID and rotation

Every signed token embeds a short `keyId` string chosen by the issuer at generation time (e.g., derived from a hash of the public key, or an issuer-assigned label). The validator is configured with a set of trusted public keys keyed by `keyId` (conceptually `IReadOnlyDictionary<string, ECDsa>`), so multiple keys can be trusted concurrently. Rotation means: start trusting a new key ID alongside the old one, switch generation over to the new key, and (optionally) later stop trusting the old key ID once no valid licenses depend on it.

**Why:** Satisfies the "build rotation in now" requirement cheaply - the token format already carries the key ID as ordinary payload data, so no future wire-format break is needed to support it.

### Clock access is injectable

Expiry checks and the issue time use `TimeProvider` (BCL, .NET 8+) rather than `DateTime.UtcNow` directly, so validation and generation are deterministically unit-testable without wall-clock dependence.

**Why:** A standard testability seam; without it, expiry and superseding scenarios from the specs would be difficult to test deterministically.

### One package

A single BCL-only package holding generation, validation and signing-key management. The earlier package split for the Azure Key Vault sourcing provider ([ADR-0002](../../../docs/adrs/0002-package-split-for-keyvault-dependency.md)) is deferred with sourcing.

## Risks / Trade-offs

- **[Risk] No revocation before expiry** (pure offline signed tokens can't be revoked once issued) → **Mitigation:** Document this as an accepted limitation; issuers wanting revocation should favor shorter expiry windows plus a renewal flow. Out of scope per the proposal.
- **[Risk] Clock rollback can defeat expiry** (whoever runs the server controls its clock) → **Mitigation:** Documented, accepted limitation common to all offline-licensing schemes; not otherwise mitigated in this change.
- **[Risk] Private key compromise invalidates trust in every license signed with it** → **Mitigation:** Key rotation (key ID) lets an issuer stop trusting a compromised key going forward, but existing licenses signed with it remain cryptographically valid until they individually expire or the issuer explicitly stops trusting that key ID (accepting that this also invalidates any still-valid legitimate licenses under that key). This trade-off is inherent to offline verification and is documented rather than solved here.
- **[Risk] No machine/domain binding** → **Mitigation:** Explicitly out of scope per the proposal; a valid key can be reused across installs until this is addressed in a future change.

## Migration Plan

Greenfield change - no existing consumers or data to migrate.

## Technical open questions

- Exact `keyId` derivation scheme (e.g., truncated hash of the public key vs. an issuer-assigned label) can be finalized during implementation; either choice satisfies the specs and design decisions above without changing them.

---

## Exploration since the proposal

The three delta specs and `tasks.md` describe **a single license key** and predate most of
this section. Nothing is implemented, so revising them is a clean rewrite, not a migration.
Product decisions are recorded as PDRs in [`docs/decisions/`](../../../docs/decisions/README.md);
the key schema and worked examples are in
[`docs/license-examples.md`](../../../docs/license-examples.md). This section holds what those
do not: the requirement and question index, the open questions, and prior art. The
chronological write-up of the exploration is in git history (commit `8f9c557` and earlier);
the pre-cut version of this section, with the host-side questions in full, is at commit
`fcda40b`.

All key contents are decided (schema in `license-examples.md`). Of the open questions, only
Q18 could change what a key holds.

### Requirements

| # | Requirement | State | Recorded |
|---|---|---|---|
| R1 | Named collection of license keys per site | deferred | `docs/deferred-scope.md` D2, D3, D7 |
| R2 | Inventory across registered products | deferred | `docs/deferred-scope.md` D5. The per-product **evaluation result** stays: see R12 |
| R3 | Backoffice screen for licenses | deferred | `docs/deferred-scope.md` D6 |
| R4 | Renewal link | deferred | `docs/deferred-scope.md` D10 |
| R5 | Feature flags | superseded by R7 | |
| R6 | Kind of license (trial / standard) | dropped | PDR-0014 |
| R7 | Product features: switches, numbers and text | decided | PDR-0010, PDR-0013, PDR-0018 (text; revisited 2026-09-30) |
| R8 | Release-date gating | dropped | PDR-0015 |
| R9 | Package self-registration | deferred | `docs/deferred-scope.md` D4 |
| R10 | Optional issuing add-on for smaller vendors | decided; separate change | PDR-0005, PDR-0006, PDR-0007 |
| R11 | The library reports and does not throw on a bad key, so a licensing problem never crashes a page; what visitors experience on a failed license is the vendor's decision | agreed, not yet specified | [`docs/personas.md`](../../../docs/personas.md), PDR-0001 |
| R12 | Evaluation result: for one product and a set of keys, each key's state and the combined entitlement | decided, not yet specified | PDR-0009, PDR-0011, PDR-0010, PDR-0018 |
| R13 | Umbraco version range | deferred | `docs/deferred-scope.md` D1 (was PDR-0012) |

Touches when specified: R11 and R12 go into `license-validation`.

### Questions

| Q | Question | Outcome |
|---|---|---|
| Q1 | Where does a stored key's human-readable name live? | deferred, D7 |
| Q2 | Shared or per-vendor store? | PDR-0002, now deferred (D3) |
| Q3 | How does a package find its keys? | deferred, D8 |
| Q4 | Inventory: store view or product view? | PDR-0003, now deferred (D5) |
| Q5 | How are unverified claims marked? | host part deferred (D5); core part open, below |
| Q6 | Backoffice screen: view-only or read-write? | deferred, D6 |
| Q7 | Who ships the screen, and what may it display? | deferred, D6 |
| Q8 | Where the renewal link comes from; the license reference | PDR-0004, now deferred (D10); PDR-0009 (reference) |
| Q9 | Feature model: explicit list or named tier? | PDR-0010: one concept, a vendor convention |
| Q10 | Do features overlap the version range? | PDR-0010: orthogonal; range now deferred (D1) |
| Q11 | Feature name rules | PDR-0013 |
| Q12 | Precision of the version range bounds | PDR-0012, now deferred (D1) |
| Q13 | What one key stands for | PDR-0008: one purchase |
| Q14 | What a package declares at registration | PDR-0004, now deferred (D4) |
| Q15 | What the issuing add-on records | PDR-0006 |
| Q16 | How licenses for one product combine | PDR-0011, PDR-0010; PDR-0018 (text never combines) |
| Q17 | Dependencies between products | deferred, D9 |
| Q18 | Same reference, different role or product | open |

### R12. Evaluation result

`license-validation` must be able to take **a set of keys and one product ID** and return each
key's role and state (valid, inactive, expired, superseded, invalid; PDR-0011) and the combined
entitlement for the product (PDR-0011, PDR-0010, PDR-0018). Keys for another product are
reported as wrong product and take no further part. This is the core of what the deferred
inventory (D5) would display; the library computes it, a host shows it.

Authenticity and usability are independent axes. A key can be authentic and expired; another
can be forged. Collapsing these into one status word loses the difference between "someone
tampered with this" and "you need to renew", which are different actions for the implementor.

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

### Q5. What does the evaluation result report for a key that fails verification? (open)

A key that does not verify still has readable claims. Those claims are assertions, not facts:
"expires 2099" on a forged key must never be returned as if true. Open: whether the result for
such a key carries its reason only, or also its readable claims with an explicit "unverified"
marker. Keys are bearer tokens; the result must not reproduce the key in full. The reference
(PDR-0009), or a short fragment, is enough to match a result back to a key.

### Q18. Same reference, different role or product (open)

Superseding is by license reference: latest issued wins (PDR-0009). Two cases are undefined.

- **Same reference, different role.** The core keeps no records (PDR-0005), so nothing stops a
  vendor reissuing an add-on under a base's reference, or the reverse. Latest-wins then
  replaces a base with an add-on (the product stops being licensed) or an add-on with a base
  (the site owner gets a product they did not buy). Options: latest wins regardless, so the
  vendor's mistake lands on the site owner; the result treats the pair as a conflict; the
  issuing add-on (PDR-0006) refuses a reissue that changes the role, and vendors with their
  own systems are told to do the same, which follows "mistakes fail at the vendor" (PDR-0010,
  PDR-0011) but cannot be enforced by the core.
- **Same reference, different product.** References are random per product (PDR-0017), so two
  vendors can generate the same one. Superseding must be scoped by product **and** reference,
  never by reference alone. PDR-0009 says "unique per product", which implies this; stating it
  outright in PDR-0009 would close this half.

Touches: PDR-0009, `license-validation`, `docs/license-examples.md` examples 3 and 4.

### Prior art: Standard.Licensing

[junian/Standard.Licensing](https://github.com/junian/Standard.Licensing), v1.3.0 (2026-05), MIT
licence, a fork of Portable.Licensing. Reviewed as a feature comparison, not as a candidate
dependency; build vs. reuse is a propose-phase question.

What it has that this project does not, and what happened to each:

| Its feature | Outcome here |
|---|---|
| Name/value product features | Adopted as R7, with typed values: switches, numbers and text, each with its own combining rule (PDR-0010, PDR-0018) |
| License type (Trial/Standard) | Considered as R6, dropped (PDR-0014) |
| Build-date validation (runs any release built before expiry) | Considered as R8, dropped (PDR-0015) |
| Unique license ID | Adopted as the license reference (PDR-0009, PDR-0017) |
| Licensee name/email/company in the key | Rejected: no personal data in keys (PDR-0006) |
| All failures returned together, each with a `HowToResolve` hint | Relevant to R12 (a result wants every reason); not a requirement |
| Conditional and vendor-defined validation rules | Not pursued |

What this project has that it does not: a product ID claim (it identifies a product by giving
each product its own key pair, so a key cannot be matched to its product by reading it), key ID
and rotation, roles and combining.

Deliberately not repeated:

- **A claim that is signed but never checked.** Its `Quantity` claim looks like a restriction
  but is never enforced. Every claim here has a defined check, or a clear statement that the
  library only reports it (as with R7 numbers).
- **Multi-line XML keys.** They are hard to put in an environment variable or a single config
  value, and the signature breaks if the XML is reformatted.
- **Expiry compared against the machine's local time.** Its expiry check uses the local date,
  so a license expires at a different moment depending on the server's time zone. Here both
  dates are UTC (PDR-0016).

### Where to resume

1. **Q18**: the superseding edge cases. Changes `license-validation` text.
2. **Q5**: what the result reports for an unverifiable key; then R12 can be specified.
3. Then revise the three delta specs and `tasks.md` against the PDRs and
   `docs/license-examples.md` (role, reference, issue time, features, combining), and finish
   ADR-0001's payload.

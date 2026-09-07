## Context

See `proposal.md` - Why / What Changes for motivation and scope. This is a greenfield library (no existing code or specs to integrate with). Key constraints carried from the proposal:
- Must validate entirely offline (no licensing server dependency).
- Must support key rotation from day one (key ID embedded in the token).
- Machine/domain binding and revocation-before-expiry are explicitly out of scope.
- Target: .NET 10, consumed by Umbraco 17+ packages.

## Goals / Non-Goals

**Goals:**
- Pick a concrete signing algorithm and token format, with rationale, so `license-generation` and `license-validation` have an unambiguous wire format to implement against.
- Keep the Azure Key Vault sourcing provider from forcing its dependencies onto consumers who don't use it.
- Keep validation logic (expiry, version-range checks) deterministically testable.

**Non-Goals:**
- Defining the full public C# API surface (interfaces/method signatures) - left to implementation, constrained only by the specs.
- NuGet packaging/publishing pipeline details.

## Decisions

### Signing algorithm: ECDSA P-256

Use `System.Security.Cryptography.ECDsa` with the P-256 (nistP256) curve for signing and verification.

**Why:** Built into the .NET base class library on all platforms .NET 10 supports (backed by the OS crypto provider - CNG on Windows, OpenSSL on Linux/macOS), so it adds no third-party dependency to either the generation or validation path. Signatures and keys are compact (a public key fits in ~91 bytes DER / a signature in ~70-72 bytes DER, or fixed 64 bytes if IEEE P1363-encoded), keeping generated license key strings short and easy to hand-deliver (email, a config value). Verification is fast, which matters because validation should be cheap enough to run on every relevant Umbraco startup/request path.

**Alternatives considered:**
- **RSA (2048/4096-bit):** Also built-in and dependency-free, but keys and signatures are substantially larger (RSA-2048 signatures are 256 bytes vs. ~64-72 for ECDSA P-256), producing longer license key strings for no security benefit at this threat level. Rejected in favor of the more compact option.
- **Ed25519:** Attractive (deterministic signatures, resistant to certain implementation pitfalls, very fast), but .NET's cross-platform built-in support is inconsistent across the OS versions this library must run on, historically requiring a third-party package (BouncyCastle or libsodium bindings) for reliable cross-platform coverage. Rejected to avoid pulling a cryptography dependency into every consumer for an algorithm choice that isn't required by any stated constraint.

Recorded as [ADR-0001](../../../docs/adrs/0001-license-token-signing-algorithm.md).

### Token format: custom fixed-algorithm compact token, not generic JWT

The license key is `base64url(payload-bytes) + "." + base64url(signature-bytes)`, where `payload-bytes` is a `System.Text.Json`-serialized object containing: `keyId`, `productId`, `expiryUtc` (optional), `minVersion`/`maxVersion` (optional), `issuedAtUtc`.

**Why:** A generic JWT (with an `alg` header the verifier must honor) invites classic JWT downgrade/confusion attacks (e.g., a token claiming `alg: none`, or an algorithm the verifier didn't intend to trust) and would pull in a JWT library dependency purely for a token shape this library doesn't need to interoperate with anything else. Because this library controls both ends (issuer and verifier) and only ever needs one algorithm, a fixed-algorithm custom format removes an entire vulnerability class by construction: the verifier never reads an algorithm identifier from untrusted input, it always verifies with ECDSA P-256.

**Alternatives considered:**
- **Standard JWT (via a JWT library):** Rejected - adds a dependency and an attack surface (algorithm negotiation) with no interoperability benefit, since nothing outside this library needs to read the token.
- **Raw binary/MessagePack payload:** More compact than JSON, but `System.Text.Json` is already part of the BCL, keeps the payload human-inspectable for support/debugging, and the size difference is negligible at this claim count. Rejected in favor of simplicity.

### Key ID and rotation

Every signed token embeds a short `keyId` string chosen by the issuer at generation time (e.g., derived from a hash of the public key, or an issuer-assigned label). The validator is configured with a set of trusted public keys keyed by `keyId` (conceptually `IReadOnlyDictionary<string, ECDsa>`), so multiple keys can be trusted concurrently. Rotation means: start trusting a new key ID alongside the old one, switch generation over to the new key, and (optionally) later stop trusting the old key ID once no valid licenses depend on it.

**Why:** Satisfies the "build rotation in now" requirement cheaply - the token format already carries the key ID as ordinary payload data, so no future wire-format break is needed to support it.

### Version comparison and clock access are injectable

- Expiry checks use `TimeProvider` (BCL, .NET 8+) rather than `DateTime.UtcNow` directly, so validation logic is deterministically unit-testable without wall-clock dependence.
- The "running Umbraco core version" is supplied to the validator by the caller (or via a small injectable accessor) rather than the validation library reaching into Umbraco's assembly metadata itself. This keeps `license-validation`'s core logic testable in isolation and avoids a hard compile-time dependency from the validation core onto a specific Umbraco assembly shape.

**Why:** Both are standard testability seams; without them, expiry and version-range scenarios from the specs would be difficult to test deterministically.

### Package split to isolate the Azure Key Vault dependency

Ship at least two packages:
- A core package containing `license-generation`, `license-validation`, and the `.NET` configuration + environment variable sourcing providers (BCL-only dependencies).
- A separate `*.AzureKeyVault` (or similarly named) package containing only the Key Vault sourcing provider, depending on `Azure.Security.KeyVault.Secrets` and `Azure.Identity`.

**Why:** Directly satisfies the "Key Vault dependency isolation" scenario in `license-key-sourcing` - a consumer that only wants configuration or environment-variable sourcing must not be forced to reference Azure SDK packages.

Recorded as [ADR-0002](../../../docs/adrs/0002-package-split-for-keyvault-dependency.md).

## Risks / Trade-offs

- **[Risk] No revocation before expiry** (pure offline signed tokens can't be revoked once issued) → **Mitigation:** Document this as an accepted limitation; issuers wanting revocation should favor shorter expiry windows plus a renewal flow. Out of scope per the proposal.
- **[Risk] Clock rollback can defeat expiry** (host controls its own system clock) → **Mitigation:** Documented, accepted limitation common to all offline-licensing schemes; not otherwise mitigated in this change.
- **[Risk] Private key compromise invalidates trust in every license signed with it** → **Mitigation:** Key rotation (key ID) lets an issuer stop trusting a compromised key going forward, but existing licenses signed with it remain cryptographically valid until they individually expire or the issuer explicitly stops trusting that key ID (accepting that this also invalidates any still-valid legitimate licenses under that key). This trade-off is inherent to offline verification and is documented rather than solved here.
- **[Risk] No machine/domain binding** → **Mitigation:** Explicitly out of scope per the proposal; a valid key can be reused across installs until this is addressed in a future change.

## Migration Plan

Greenfield change - no existing consumers or data to migrate. Initial release ships the core package and the Azure Key Vault sourcing package together as a paired version.

## Open Questions

- Exact `keyId` derivation scheme (e.g., truncated hash of the public key vs. an issuer-assigned label) can be finalized during implementation; either choice satisfies the specs and design decisions above without changing them.

---

### Unresolved scope raised in exploration (2026-09-07)

**Status: open. Not yet reflected in `proposal.md` or any spec.** Two new requirements were
raised by the Product Owner during exploration, and five decisions must be settled before
the specs can be revised to accommodate them. These are recorded here so the discussion can
be resumed cold.

#### Background

Everything currently written in `proposal.md`, the three delta specs, and the decisions above
assumes **a single license key string**: `license-key-sourcing` obtains one raw key, and
`license-validation` answers one question - "is this key valid for this product, right now?"

That assumption no longer holds. A realistic Umbraco site runs several paid packages, from
several different vendors, each licensed independently. The requirements below reshape the
single key into a collection, and add a reporting surface on top of it. They are not additive
tweaks: they change existing spec text (particularly `license-key-sourcing`, whose core
abstraction is currently "obtain a raw license key string"), which is why they are parked
here rather than partially applied.

Nothing is implemented yet (0/27 tasks), so this is a clean revision, not a migration.

#### Requirement R1: a named collection of license keys

The host supplies **1..N license key entries** rather than one. Each entry is fully
independent - its own product, its own expiry, its own supported version range, its own
validity. One expired entry must not affect the evaluation of any other.

Each entry additionally carries a **human-readable name**, so that when a key needs replacing
the host can tell which entry to edit.

```
  HOST'S LICENSE STORE
  +---------------------------------------------------------+
  | name: "Forms Pro - renewed Mar 2026"   key: eyJr...abc   |
  | name: "SEO Toolkit (Acme)"             key: eyJr...def   |
  | name: "Commerce - TRIAL"               key: eyJr...ghi   |
  +---------------------------------------------------------+
          |              |              |
          v              v              v
     [Forms Pro]    [SEO Toolkit]   [Commerce]
```

Touches: `proposal.md` (scope), `license-key-sourcing` (reshaped - sources yield entries,
not a single string), `license-validation` (new: resolving which entry serves a given
product), `tasks.md` (new work).

#### Requirement R2: inventory reporting

`license-validation` should be able to return **the whole set of identified products** with
their expiry, version range and state - not only a per-product verdict. This is what lets a
site admin answer "what am I licensed for, and what is about to break?" before it breaks.

```
  +----------------------------------------------------------------+
  | LABEL          PRODUCT       STATE      EXPIRES     VERSIONS    |
  +----------------------------------------------------------------+
  | Forms Pro      forms.pro     active     2027-03-01  17.0-19.x   |
  | SEO Toolkit    acme.seo      expired    2026-01-14  any         |
  | Commerce TRIAL commerce      active     2026-09-30  17.x        |
  | (renewal?)     ???           unreadable    -           -        |
  +----------------------------------------------------------------+
```

Two structural points fell out of discussing it:

- **Authenticity and usability are independent axes.** A key can be perfectly authentic and
  expired; another can be forged. Collapsing these into a single status word loses the
  difference between "someone tampered with this" and "you need to renew" - very different
  admin actions.

```
                          AUTHENTIC?
                      no              yes
                +---------------+---------------+
     USABLE  no | broken /      | expired,      |
                | untrusted     | wrong version |
                +---------------+---------------+
             yes|     n/a       |    active     |
                +---------------+---------------+
```

- **Listing an entry means reading claims out of a key that may not verify.** An inventory
  that silently drops unverifiable entries omits exactly the rows the admin needs. One that
  includes them unmarked presents a forger's numbers as fact. See Q5.

Touches: `license-validation` (substantial addition, possibly its own capability),
`proposal.md`, `tasks.md`.

#### Q1. Where does the human-readable name live?

A name can sit **outside** the key, written by the host, or **inside** the signed payload,
written by the vendor at mint time. These look like the same field but answer different
questions:

- **Host label = an address.** "Which slot do I edit?" Authored by the person who will edit it.
- **Issuer name = an identity.** "What is this thing?" Bound to the other claims by the signature.

Four scenarios discriminate the options, and no single field covers all four:

| # | Scenario | Host label | Issuer name |
|---|---|---|---|
| A | **Mangled paste** - key truncated on copy, nothing inside it is readable | works - the only thing that can name a broken entry | fails - gone with the key |
| B | **Mis-paste** - a valid Commerce key sits in the slot labelled "Forms Pro" | fails - confidently wrong, admin scans past it | works - row can flag the contradiction |
| C | **Renewal drift** - label reads "expires Mar 2026", key was renewed a year ago | fails - hand-written metadata decays | works - regenerated with each key |
| D | **Host's own scheme** - same product across prod/staging, or two sites | works - "prod, renewed by Jane" | fails - every key says "Forms Pro" |

Options:
- **(a) Host label only** - covers A and D, exposed to B and C.
- **(b) Issuer name only** - covers B and C, cannot name an unreadable entry (A) and denies
  the host any naming scheme of their own (D).
- **(c) Both** - the only option that catches a mis-paste ("this slot is labelled Forms Pro
  but contains a key for Commerce"), at the cost of two name fields to display and explain,
  a new claim in the token, and a mismatch-reporting rule.

Complications to weigh:
- A host label is natural in a structured configuration file, but there is **no obvious place
  for it in an environment variable** (one variable, one string - the name would have to live
  in the variable's name or be encoded into its value) or in a vault secret (though a secret's
  own name is a plausible label). The label concept must survive all three sourcing providers.
- An issuer name is **baked in permanently**: a wrong or ugly one cannot be corrected without
  reissuing the key.
- If an issuer name is added, decide what it contains - product name only, or also the
  licensee ("Acme Ltd"). Licensee names are useful for support, but bake organizational data
  into a string that gets pasted into config and may be logged.
- **Third option, for completeness:** no name at all - identity comes from the product ID
  inside the key. Stable and machine-routable, but fails A (unreadable entries) and D
  (multiple keys for one product), and reads poorly to a human.

Touches: `license-key-sourcing` for a host label; `license-generation` and ADR-0001's payload
shape for an issuer name.

#### Q2. Is the store shared across vendors, or per-vendor?

Every package built on this library reads its key from somewhere.

- **Shared store** - the host pastes all keys into one place regardless of vendor. Much better
  for the host, but the store's shape becomes a marketplace-wide contract every vendor must
  agree on and none can change unilaterally.
- **Per-vendor sections** - no coordination needed between vendors, but the host maintains N
  separate lists and learns a different arrangement per package.

This is the decision most likely to be irreversible once packages ship against it.

#### Q3. How does a package find its key, and what wins on a duplicate?

Two models for resolution:
- **Host assigns** - the host declares which entry belongs to which product. More host work,
  one more thing to get wrong.
- **System routes** - the host drops every key in, in any order, and each package finds its
  own by matching product ID. Better experience, but it implies each key's product ID is read
  **before** the signature is trusted, purely for routing. That is acceptable, but it should
  be an explicit requirement rather than an accident of implementation.

Duplicates are near-certain at renewal, when the host pastes the new key and leaves the old
one in place:

```
  Forms Pro (expires 2026-03-01)  <- old, still present
  Forms Pro (expires 2027-03-01)  <- new
```

Candidate rules: prefer the entry that validates; prefer the longest expiry; reject the
ambiguity outright. The worst outcome is silently choosing the stale one - the host renews
and the system still reports expired.

#### Q4. Is the inventory a view of the store, or a view of the products?

- **Store view** - one row per key the host supplied. Shows what is present. Cannot say
  "Commerce has no license" because it does not know Commerce exists.
- **Product view** - one row per product that expects a license. Shows what is present *and
  what is missing*; "Commerce: no license found" is arguably the most important row on the
  page.

Product view is materially more useful to a human, but requires something the library does
not have today: a way for each licensed package to **register itself** as expecting a license.
That changes the adoption contract - today a package only has to *ask* for validation; under
a product view it must also *declare* itself.

#### Q5. How are unverified claims marked in the inventory?

Rows fall into three tiers:

```
  tier 1  unreadable         -> only the host's label is knowable
  tier 2  readable, not      -> claims exist but are ASSERTIONS, not facts.
          authentic             "Expires 2099" on a forged key must never
                                be displayed as truth.
  tier 3  authentic          -> claims are facts
```

The requirement is that a row carries an explicit "these values are unverified" marker,
distinct from its state column. Open: how that is surfaced, and whether tier 2 claims are
returned at all or suppressed in favour of the label alone.

Two smaller questions attached to the inventory:
- **Days remaining** - cheap to include, and any warn-before-expiry behaviour would be built
  on it. Without it, every consumer recomputes it.
- **Raw key values** - license keys are bearer tokens; anyone holding one is licensed. An
  inventory destined for a backoffice screen or a log file probably should not reproduce them
  in full. A short identifying fragment would let a host match a row back to their store
  without exposing the whole key.

#### Suggested order of discussion

Q2 and Q3 first - they change existing spec text. Q1, Q4 and Q5 mostly add to it.

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

#### Correction to the first pass: Q2 and Q4 are coupled

The five questions above were originally recorded as independent. Two of them are not.

R2's site-wide inventory is only possible over a **shared** store. Under per-vendor stores, a
package can only see its own vendor's keys, so "the collection of identified products" becomes
N disjoint per-vendor lists, and Q4's product view ("Commerce: no license found") cannot be
rendered by anyone except Commerce's own package.

```
   PER-VENDOR STORES                    SHARED STORE
   +-------------+ +-------------+      +---------------------------+
   | vendor A    | | vendor B    |      | all keys, all vendors     |
   |  forms key  | |  seo key    |      |  forms, seo, commerce     |
   +-------------+ +-------------+      +---------------------------+
         |               |                    |      |      |
         v               v                    v      v      v
    A sees only     B sees only          any package can see the
    its own         its own              whole site's licensing
```

Answer Q2 "per-vendor" and Q4 largely resolves itself as "store view, scoped to one vendor".

Two further notes on Q2:

- **Choosing "shared" makes the store shape a permanent compatibility surface.** Once two
  vendors ship against it, a single site can run one package built on library v1 and another
  on v2, both reading the same store. The format can then only ever be extended, never
  changed. This is a heavier long-term commitment than any decision in ADR-0001, because
  those are internal to a library version while this one is a contract *between* versions.
- **The "vendor A can read vendor B's keys" objection is not a real cost.** Any package running
  in the site can already read the whole configuration; a shared store introduces no exposure
  that was not already there. Recorded so it is not weighed as a downside it is not.

---

### Further scope raised in exploration (second pass, 2026-09-07)

**Status: open. Not yet reflected in `proposal.md` or any spec.** A second round of
requirements from the Product Owner, recorded on the same basis as the first pass above.

#### Background

The first pass reshaped a single key into a collection. This pass changes what a license
fundamentally *is*:

```
  BEFORE                          AFTER
  license = boolean               license = entitlement set

  "is Forms Pro licensed?"        "is Forms Pro licensed,
       yes / no                    until when,
                                   for which Umbraco versions,
                                   with which features enabled,
                                   and where do I go to change that?"
```

Every requirement specced so far answers a yes/no question. R3-R5 turn a license into a
description of what the customer bought, which is a different thing to model and a different
thing to display.

#### Requirement R3: backoffice UI for managing license keys

An Umbraco backoffice screen for working with the configured collection - viewing each entry's
expiry, the products it permits, and its state, and managing the keys themselves.

#### Requirement R4: per-key renewal / upgrade link

Each key can surface a link that takes the customer to the vendor, to renew an expiring license
or to buy additional features.

#### Requirement R5: feature flags

A license can permit a subset of a product's features, so functionality can be gated per
customer rather than the whole product being all-or-nothing.

#### Q6. Is the backoffice manager view-only, or read-write?

The word "manage" conflicts with the sourcing model already specced. All three planned sources
are effectively read-only at runtime:

```
  appsettings.json   -> often read-only in production (containers,
                        source-controlled, deployed rather than edited)
  environment var    -> not writable at runtime in any meaningful sense
  Key Vault          -> writable in principle, but needs a write permission
                        most hosts will not grant the application
```

A view-only screen fits this perfectly. An editing screen requires a **writable store the site
owns** - a source that does not exist in the current proposal. Adding one creates a precedence
problem:

```
   appsettings.json:  forms.pro = eyJ...OLD
   UI-managed store:  forms.pro = eyJ...NEW
                              |
                        which one wins?
```

Underneath that sits a genuine split in how a site is operated:

- **Config-sourced keys are deployed.** They flow through the pipeline, sit in source control,
  and keep environments in step.
- **UI-managed keys are runtime state.** They live in one environment's data; a key added on
  staging does not exist in production.

Supporting both means choosing a precedence rule and accepting the resulting confusion. A
view-only manager that reports status and points the host at *where* to edit sidesteps the
entire question, at the cost of not being a manager in the fullest sense.

#### Q7. Who ships the UI, and what may it display?

If the UI lives in the core library and five vendors each ship packages built on it, five
copies attempt to register the same backoffice section:

```
   [Forms Pro] --+
   [SEO Toolkit]-+--> core lib --> registers "Licenses" section  x5 ?
   [Commerce] ---+
```

The licensing screen is a **host-level concern, not a per-package one** - a site has one
licensing screen, not one per vendor. That argues for a third shipped package that the host
installs once, alongside the core and Key Vault packages split in ADR-0002. It also reinforces
the shared-store reading of Q2: a single UI over per-vendor stores cannot work.

Separately, the UI is where Q5's "do not reproduce raw keys" rule earns its keep. License keys
are bearer tokens - anyone who can read one can license another site with it. A screen that
displays them in full turns "can log into the backoffice" into "can walk off with the
licenses". Needs a decision on what the screen shows and which backoffice users may see it.

#### Q8. Where does the renewal link come from, and is renewal one link or two?

This is Q1's host-versus-issuer tension again, but it resolves the opposite way, because URLs
decay and names do not.

| Source | Problem |
|---|---|
| Baked into the signed key at mint | **URLs rot.** A vendor rebrands or moves their store and every key ever issued points at a dead link, unfixable without reissuing. |
| Configured by the host | The host does not know the vendor's renewal URL. Wrong party. |
| Declared by the package at runtime | Always current, ships with the vendor's own release, no staleness. |

Package-declared links need the same **package self-registration** mechanism that Q4's product
view requires. Two requirements converging on one missing piece suggests that piece is real and
should be designed deliberately rather than twice.

Two attached points:

- **Do not put the license key in the renewal URL.** It would land in browser history, referrer
  headers and proxy logs. If the renewal page needs to identify the license, the token should
  carry a separate **opaque license reference** (an order or license ID that is safe to expose)
  distinct from the key itself. This is a new claim - cheap to add now, expensive later.
- **Renew and upgrade may be different destinations.** "Another year of what I have" and "sell
  me the tier above" are usually different pages. Decide whether this is one link or two.

#### Q9. Feature model: explicit list or named tier?

```
  A) EXPLICIT FEATURE LIST          B) NAMED TIER
     features: [reports, export,       tier: "pro"
                api, whitelabel]

  + bespoke per-customer deals      + compact
  + no vendor-side mapping needed   + tier->features mapping lives in the
                                      package and ships with releases
  - key grows with each feature     - bespoke deals require inventing tiers
  - ADDING A FEATURE TO A TIER      - less granular
    MEANS REISSUING EVERY KEY
```

The capitalised line is the heaviest consideration. Under an explicit list, the day a vendor
adds a capability to their Pro offering, every existing Pro customer's key lacks it - the
vendor must mint and redistribute keys to their entire customer base to deliver a feature
customers believe they already bought. Under a tier name, a package release does it.

But the tier model forces a **commercial** question that no technical choice can answer:

> When a customer buys "Pro" today, are they buying today's Pro features, or Pro forever,
> including everything added later?

```
   key minted 2026  --> tier: pro
                            |
   package 2028 adds "ai-assist" to Pro
                            |
                    is this customer entitled?
```

Answer yes and a tier name suffices. Answer no and the entitlement must be pinned to what the
tier meant at issue time - pushing back toward an explicit list, or toward using the existing
version-range claim as the boundary (see Q10).

Smaller attached questions: who namespaces feature identifiers across independent vendors, and
does an unrecognised feature name fail open or closed?

#### Q10. Do feature flags and the supported-version range overlap?

The supported-version-range claim was designed to express "which Umbraco versions this license
covers". If new capabilities arrive in new package releases, a version bound already gates
access to future features implicitly. Features plus version range risks being **two overlapping
controls** over the same commercial question, with no defined answer for which governs when
they disagree. Resolve whether they are orthogonal (versions gate compatibility, features gate
entitlement) or redundant.

#### Sequencing: what cannot be deferred

R3-R5 roughly triple the size of this change, but the pieces are not equally urgent:

```
   token claims      <-- MUST be decided now. Changing the payload later
                        breaks every key already issued.

   validation API    <-- can grow
   inventory         <-- can grow
   backoffice UI     <-- can ship later, as a separate package
   renewal links     <-- can ship later, if package-declared
```

Whatever is decided about features, references and links, the **claims** must be settled before
any key is minted in anger. The surfaces built on top can arrive over several releases. This is
the strongest argument for resolving Q8's license-reference claim and Q9's feature model now,
even if the UI becomes a separate change entirely.

#### Suggested order of discussion

1. **Q2 with Q4** - coupled (see correction above), and the shared-store answer is the most
   irreversible decision on the list.
2. **Q9 with Q10, and Q8's license-reference claim** - these determine the token payload, which
   cannot be changed after keys are issued.
3. **Q3** - routing and duplicates; changes existing spec text.
4. **Q1, Q5, Q6, Q7** - largely additive, and Q6/Q7 can plausibly become a separate change.

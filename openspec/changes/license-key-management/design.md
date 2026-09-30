## Context

See `proposal.md` - Why / What Changes for motivation and scope. This is a greenfield library (no existing code or specs to integrate with). Key constraints carried from the proposal:
- Must validate entirely offline (no licensing server dependency).
- Must support key rotation from day one (key ID embedded in the token).
- Machine/domain binding and revocation-before-expiry are explicitly out of scope.
- Target: .NET 10, consumed by Umbraco 17+ packages.

Personas are defined in [`docs/personas.md`](../../../docs/personas.md); the site owner is the primary customer. Terms used below: **issuer** is the vendor in its license-issuing role; **consumer** is the vendor's package calling the library at runtime; **host application** is the Umbraco site as software, which the implementor configures.

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
- **[Risk] Clock rollback can defeat expiry** (whoever runs the server controls its clock) → **Mitigation:** Documented, accepted limitation common to all offline-licensing schemes; not otherwise mitigated in this change.
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

The implementor supplies **1..N license key entries** rather than one. Each entry is fully
independent - its own product, its own expiry, its own supported version range, its own
validity. One expired entry must not affect the evaluation of any other.

Each entry additionally carries a **human-readable name**, so that when a key needs replacing
the implementor can tell which entry to edit.

```
  SITE'S LICENSE STORE
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
site owner or implementor answer "what am I licensed for, and what is about to break?" before it breaks.

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
  actions for the implementor.

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
  that silently drops unverifiable entries omits exactly the rows the implementor needs. One that
  includes them unmarked presents a forger's numbers as fact. See Q5.

Touches: `license-validation` (substantial addition, possibly its own capability),
`proposal.md`, `tasks.md`.

#### Q1. Where does the human-readable name live?

A name can sit **outside** the key, written by the implementor, or **inside** the signed payload,
written by the vendor at mint time. These look like the same field but answer different
questions:

- **Site label = an address.** "Which slot do I edit?" Authored by the implementor, who will edit it.
- **Vendor name = an identity.** "What is this thing?" Bound to the other claims by the signature.

Four scenarios discriminate the options, and no single field covers all four:

| # | Scenario | Site label | Vendor name |
|---|---|---|---|
| A | **Mangled paste** - key truncated on copy, nothing inside it is readable | works - the only thing that can name a broken entry | fails - gone with the key |
| B | **Mis-paste** - a valid Commerce key sits in the slot labelled "Forms Pro" | fails - confidently wrong, implementor scans past it | works - row can flag the contradiction |
| C | **Renewal drift** - label reads "expires Mar 2026", key was renewed a year ago | fails - hand-written metadata decays | works - regenerated with each key |
| D | **Implementor's own scheme** - same product across prod/staging, or two sites | works - "prod, renewed by Jane" | fails - every key says "Forms Pro" |

Options:
- **(a) Site label only** - covers A and D, exposed to B and C.
- **(b) Vendor name only** - covers B and C, cannot name an unreadable entry (A) and denies
  the implementor any naming scheme of their own (D).
- **(c) Both** - the only option that catches a mis-paste ("this slot is labelled Forms Pro
  but contains a key for Commerce"), at the cost of two name fields to display and explain,
  a new claim in the token, and a mismatch-reporting rule.

Complications to weigh:
- A site label is natural in a structured configuration file, but there is **no obvious place
  for it in an environment variable** (one variable, one string - the name would have to live
  in the variable's name or be encoded into its value) or in a vault secret (though a secret's
  own name is a plausible label). The label concept must survive all three sourcing providers.
- A vendor name is **baked in permanently**: a wrong or ugly one cannot be corrected without
  reissuing the key.
- If a vendor name is added, decide what it contains - product name only, or also the
  licensee ("Acme Ltd"). Licensee names are useful for support, but bake organizational data
  into a string that gets pasted into config and may be logged.
- **Third option, for completeness:** no name at all - identity comes from the product ID
  inside the key. Stable and machine-routable, but fails A (unreadable entries) and D
  (multiple keys for one product), and reads poorly to a human.

Touches: `license-key-sourcing` for a site label; `license-generation` and ADR-0001's payload
shape for a vendor name.

#### Q2. Is the store shared across vendors, or per-vendor?

Every package built on this library reads its key from somewhere.

- **Shared store** - the implementor pastes all keys into one place regardless of vendor. Much better
  for the site owner and implementor, but the store's shape becomes a marketplace-wide contract every vendor must
  agree on and none can change unilaterally.
- **Per-vendor sections** - no coordination needed between vendors, but the implementor maintains N
  separate lists and learns a different arrangement per package.

This is the decision most likely to be irreversible once packages ship against it.

#### Q3. How does a package find its key, and what wins on a duplicate?

Two models for resolution:
- **Implementor assigns** - the implementor declares which entry belongs to which product. More work,
  one more thing to get wrong.
- **System routes** - the implementor drops every key in, in any order, and each package finds its
  own by matching product ID. Better experience, but it implies each key's product ID is read
  **before** the signature is trusted, purely for routing. That is acceptable, but it should
  be an explicit requirement rather than an accident of implementation.

Duplicates are near-certain at renewal, when the implementor pastes the new key and leaves the old
one in place:

```
  Forms Pro (expires 2026-03-01)  <- old, still present
  Forms Pro (expires 2027-03-01)  <- new
```

Candidate rules: prefer the entry that validates; prefer the longest expiry; reject the
ambiguity outright. The worst outcome is silently choosing the stale one - the site owner renews
and the system still reports expired.

#### Q4. Is the inventory a view of the store, or a view of the products?

- **Store view** - one row per key the implementor supplied. Shows what is present. Cannot say
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
  tier 1  unreadable         -> only the site label is knowable
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
  in full. A short identifying fragment would let an implementor match a row back to their store
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
description of what the site owner bought, which is a different thing to model and a different
thing to display.

#### Requirement R3: backoffice UI for managing license keys

An Umbraco backoffice screen for working with the configured collection - viewing each entry's
expiry, the products it permits, and its state, and managing the keys themselves.

#### Requirement R4: per-key renewal / upgrade link

Each key can surface a link that takes the site owner to the vendor, to renew an expiring license
or to buy additional features.

#### Requirement R5: feature flags

A license can permit a subset of a product's features, so functionality can be gated per
site owner rather than the whole product being all-or-nothing.

#### Q6. Is the backoffice manager view-only, or read-write?

The word "manage" conflicts with the sourcing model already specced. All three planned sources
are effectively read-only at runtime:

```
  appsettings.json   -> often read-only in production (containers,
                        source-controlled, deployed rather than edited)
  environment var    -> not writable at runtime in any meaningful sense
  Key Vault          -> writable in principle, but needs a write permission
                        most implementors will not grant the application
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
view-only manager that reports status and points the implementor at *where* to edit sidesteps the
entire question, at the cost of not being a manager in the fullest sense.

#### Q7. Who ships the UI, and what may it display?

If the UI lives in the core library and five vendors each ship packages built on it, five
copies attempt to register the same backoffice section:

```
   [Forms Pro] --+
   [SEO Toolkit]-+--> core lib --> registers "Licenses" section  x5 ?
   [Commerce] ---+
```

The licensing screen is a **site-level concern, not a per-package one** - a site has one
licensing screen, not one per vendor. That argues for a third shipped package that the implementor
installs once, alongside the core and Key Vault packages split in ADR-0002. It also reinforces
the shared-store reading of Q2: a single UI over per-vendor stores cannot work.

Separately, the UI is where Q5's "do not reproduce raw keys" rule earns its keep. License keys
are bearer tokens - anyone who can read one can license another site with it. A screen that
displays them in full turns "can log into the backoffice" into "can walk off with the
licenses". Needs a decision on what the screen shows and which backoffice users may see it.

#### Q8. Where does the renewal link come from, and is renewal one link or two?

This is Q1's site-versus-vendor tension again, but it resolves the opposite way, because URLs
decay and names do not.

| Source | Problem |
|---|---|
| Baked into the signed key at mint | **URLs rot.** A vendor rebrands or moves their store and every key ever issued points at a dead link, unfixable without reissuing. |
| Configured by the implementor | The implementor does not know the vendor's renewal URL. Wrong party. |
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

  + bespoke per-owner deals         + compact
  + no vendor-side mapping needed   + tier->features mapping lives in the
                                      package and ships with releases
  - key grows with each feature     - bespoke deals require inventing tiers
  - ADDING A FEATURE TO A TIER      - less granular
    MEANS REISSUING EVERY KEY
```

The capitalised line is the heaviest consideration. Under an explicit list, the day a vendor
adds a capability to their Pro offering, every existing Pro site owner's key lacks it - the
vendor must mint and redistribute keys to every site owner who bought Pro to deliver a
feature they believe they already bought. Under a tier name, a package release does it.

But the tier model forces a **commercial** question that no technical choice can answer:

> When a site owner buys "Pro" today, are they buying today's Pro features, or Pro forever,
> including everything added later?

```
   key minted 2026  --> tier: pro
                            |
   package 2028 adds "ai-assist" to Pro
                            |
                    is this site owner entitled?
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

---

### Third pass: features, dropped requirements, prior art (2026-09-26)

**Status: R7 settled, pending Q11. Not yet reflected in `proposal.md` or any spec.** Three
requirements were raised after comparing this project with an existing library (see Prior art,
below). Each was explored separately. Two were dropped and one was agreed.

#### Requirement R7: product features (agreed)

Recorded in [PDR-0010](../../../docs/decisions/0010-product-features.md).

A license key carries a set of named features with typed values. Vendors use these to gate
capabilities inside their package, and to sell limits as well as switches, e.g. an eCommerce
package controlling `max-orders`.

```
  KEY (signed, fixed at issue)        PACKAGE RELEASE (vendor code, changes)
  +---------------------------+       +----------------------------------+
  | features:                 |       | checkout   requires  ecommerce   |
  |   ecommerce               | ----> | ai-assist  requires  pro   (new) |
  |   pro                     |       | new order  allowed while         |
  |   max-orders: 500         |       |            orders < max-orders   |
  +---------------------------+       +----------------------------------+
```

Agreed rules:

- A key carries 0..N features, scoped to the key's product. There is no cross-vendor naming
  scheme because names never cross product boundaries.
- Each feature is a name plus a **typed** value. Allowed types: switch (true), whole number,
  text.
- A plain name means `true`: `[pro]` is the same as `pro: true`.
- An explicit `false` is **invalid**. Present means granted, with no second way to say "not
  granted".
- Malformed values are rejected when the key is issued. A typo such as `max-orders: "5OO"` must
  fail at the vendor, not months later on a site owner's site.
- "Tier" and "feature" are one concept. Whether `pro` is a bundle or a single capability is a
  vendor convention the library does not model.
- The package maps its own capabilities to feature names. A capability added in a later release
  can be gated on a name existing site owners already hold, so it reaches them without new keys.
  This answers Q9's commercial question: site owners get later additions when the vendor gates
  them on a name they already hold.
- A feature missing from the key is not granted (fails closed). A name in the key that the
  running package does not recognise is ignored.
- Features do not affect validity. A valid key with no features is a valid license. "Does this
  license grant X?" is a separate question, asked after validation, and always answers no for
  an invalid license. The existing "Distinct validation result reasons" requirement is
  unchanged.
- The library reports values; the package enforces them. Counting orders, and deciding whether
  a limit is lifetime, per period or per site, is outside the library.

This resolves Q9: both feature lists and tiers are supported, as a vendor convention. It also
resolves Q10: features and the Umbraco version range are orthogonal. Features gate entitlement
within a product; the range sets which Umbraco versions a license covers.

Touches: `license-generation` (new claim, typed-value and no-`false` input rules),
`license-validation` (feature query, separate from the validity verdict), ADR-0001's payload
shape, `tasks.md`.

#### Q11. Are feature names case-sensitive? (decided in ninth pass)

Recorded in [PDR-0013](../../../docs/decisions/0013-feature-names.md).

If a vendor issues `Pro` and the package checks for `pro`, is that a match? Suggested:
case-insensitive matching, with generation rejecting a key that holds two names differing only
by case. Unconfirmed.

#### Q12. Precision of the Umbraco version range bounds (decided in ninth pass)

Recorded in [PDR-0012](../../../docs/decisions/0012-umbraco-version-range.md).

With R8 dropped, the supported Umbraco version range is the **commercial** boundary (e.g. "an
Umbraco 17-18 license; Umbraco 19 is a paid upgrade"), not a compatibility declaration. The
specs do not define what the bounds contain:

- Majors only (`17`) or full versions (`17.2.1`)?
- Does a max of `18` include `18.4`? Presumably yes if the range is commercial, but it is not
  written down.
- Only a contiguous range is possible; "17 and 19 but not 18" cannot be expressed. Probably
  acceptable, but unconfirmed.

Touches: `license-generation` and `license-validation` range requirements, tasks.md task 2.1
and task 4.5.

#### Dropped: R6, kind of license

Recorded in [PDR-0014](../../../docs/decisions/0014-dropped-kind-of-license.md).

Proposed: a signed license kind (`Trial`, `Standard`). Dropped by the Product Owner.

Why it was considered: to mark trials in the inventory, let packages vary behaviour per kind,
require an expiry on trials, and prefer a standard key over a trial for the same product when
resolving duplicates (Q3).

Consequence of dropping it: a trial is a key with a short expiry. Nothing in the key
distinguishes it from a paid license except the date and the site label. If a vendor needs to
detect a trial, a feature (R7) can carry it by convention. That is a possible fallback, not a
requirement.

#### Dropped: R8, release-date gating

Recorded in [PDR-0015](../../../docs/decisions/0015-dropped-release-date-gating.md).

Proposed: after a license expires, releases published before expiry keep working; only newer
releases are refused. Expiry would mean "end of updates" rather than "stop working". Dropped by
the Product Owner: it adds complication, and the Umbraco version range already gives enough
commercial control.

What it would have required, recorded so the idea is not re-raised without context:

- Expiry's meaning changes from "stop working" to "end of coverage", which means rewriting the
  "Expiry check" requirement in `license-validation`.
- Each package needs a trustworthy, offline, vendor-declared release date.
- Trials would need a hard stop, or a lapsed trial would license the trial-period release
  forever for free.
- A policy is needed for security patches released after a license lapses.
- A new "lapsed" inventory state is needed. The site keeps working until an upgrade breaks it,
  so the failure is tied to a deployment rather than a date.
- It overlaps with the Umbraco version range as a second control over what a license covers.

Consequence of dropping it: expiry remains a hard stop, as specified. The clock-rollback risk
in Risks / Trade-offs stands.

#### Prior art: Standard.Licensing

[junian/Standard.Licensing](https://github.com/junian/Standard.Licensing), v1.3.0 (2026-05), MIT
licence, a fork of Portable.Licensing. Reviewed as a feature comparison, not as a candidate
dependency; build vs. reuse is a propose-phase question.

What it has that this project does not, and what happened to each:

| Its feature | Outcome here |
|---|---|
| Name/value product features | Adopted as R7, with typed values instead of plain text |
| License type (Trial/Standard) | Considered as R6, dropped |
| Build-date validation (runs any release built before expiry) | Considered as R8, dropped |
| Unique license ID | Matches Q8's opaque license reference; still open |
| Licensee name/email/company in the key | Q1's licensee question; still open. Note this puts personal data in a string that gets pasted into config and may be logged. |
| All failures returned together, each with a `HowToResolve` hint | Relevant to R2 (a row wants every reason) and R4 (a hint is a simple renewal link); not yet a requirement |
| Conditional and vendor-defined validation rules | Not pursued |

What this project has that it does not: a product ID claim (it identifies a product by giving
each product its own key pair, so a key cannot be matched to its package by reading it; Q3's
automatic matching needs the claim), key ID and rotation, the Umbraco version range, key
sourcing, and everything in R1-R4.

Deliberately not repeated:

- **A claim that is signed but never checked.** Its `Quantity` claim looks like a restriction
  but is never enforced. Every claim here needs a defined check, or a clear statement that the
  library only reports it (as with R7 limits).
- **Multi-line XML keys.** They are hard to put in an environment variable or a single config
  value, and the signature breaks if the XML is reformatted.
- **Expiry compared against the machine's local time.** Its expiry check uses the local date,
  so a license expires at a different moment depending on the server's time zone.

#### Updated order of discussion

Q9 and Q10 are resolved above. Remaining, in order:

1. **Q2 with Q4**: shared vs. per-vendor store, and store view vs. product view. The primary
   customer is now decided (site owner; see Fourth pass), which favours a shared store and a
   product view. Also, an inventory can only cover packages built on this library, so it can
   look complete when it is not.
2. **Q8's license-reference claim, Q11 and Q12**: remaining key-payload questions. These must
   be settled before any key is issued.
3. **Q3**: routing and duplicates.
4. **Q1, Q5, Q6, Q7**: largely additive.

---

### Fourth pass: personas (2026-09-26)

**Status: primary customer decided; Q13 open. Not yet reflected in `proposal.md` or any spec.**
Personas are now defined in [`docs/personas.md`](../../../docs/personas.md): vendor, site owner,
implementor, backoffice editor, site visitor. Earlier passes used "host", "admin" and "customer"
for people; they have been reworded to the persona that fits each case.

#### Decided: the primary customer is the site owner

Recorded in [PDR-0001](../../../docs/decisions/0001-primary-customer-is-the-site-owner.md).

The site owner buys licenses and gets most of the value from R1-R3. When personas' interests
conflict, the site owner wins, then the implementor, then the vendor.

Effect on open questions:

- **Q2 leans towards a shared store.** A site owner with keys from several vendors should not
  have their implementor learn a different arrangement per package.
- **Q4 leans towards the product view.** "Commerce: no license found" is the row a site owner
  most needs to see. The cost is package self-registration (also needed by Q8).
- **Q8: links must come from the vendor.** Site owners buy "from anywhere" (vendor store,
  marketplace, reseller), so no purchase channel can be assumed.
- **Q1 and R2 have two readers.** The implementor needs to know which configured entry to fix
  and why. The site owner needs to know what they are licensed for and what is about to expire.
  Same data, different questions.
- **Q7 and the backoffice editor.** Editors use the product but do not manage licenses.
  Whether they see licensing warnings, and never full keys, is part of Q7.

#### New requirements from the site visitor persona (not yet specified)

- No license key, license status or licensing message reaches a public visitor.
- A licensing problem (expired, malformed, missing key) never crashes a page. The library
  reports problems; it does not throw in the request path.
- What visitors experience when a license fails is the vendor's decision, not the library's.

Touches: `license-validation`.

#### Q13. Licenses combine: full product, add-on, extra capacity (model decided in seventh pass)

Recorded in [PDR-0008](../../../docs/decisions/0008-a-key-stands-for-one-purchase.md), [PDR-0011](../../../docs/decisions/0011-base-and-add-on-licenses.md).

A site owner can buy a license for a full product, an add-on for a product, or extra capacity.
One product can therefore have several valid keys at once, and they combine. Q3 treated a
second key for a product as a duplicate to resolve; that is now also a normal case.

```
  Commerce
    key 1: base license      ecommerce, max-orders: 500    exp 2027-03
    key 2: add-on            ai-assist                     exp 2026-12
    key 3: capacity pack     max-orders: +1000             exp 2027-03
                                     |
                       effective entitlement = ?
```

The renewal and capacity cases look alike but must combine differently:

```
  RENEWAL (old key left in place)        CAPACITY PACK
    max-orders: 500   exp 2026-03          max-orders: 500
    max-orders: 500   exp 2027-03          max-orders: +1000
    must NOT become 1000                   MUST become 1500
```

First decision: what is an add-on?

- **(a) A feature on the same product.** The add-on key carries the base product's ID and is
  signed with that product's private key. Keys for one product combine.
- **(b) A product of its own.** The add-on has its own product ID and private key, and may
  require the base product's license. Add-ons never combine with the base; only capacity packs
  do.

Then: how keys for one product combine per value type (switches, whole numbers, text), how a
capacity key is told apart from a renewal, and how combined keys with different expiries are
reported. Touches: R7, Q3, R2, `license-generation`, `license-validation`.

Updated order of discussion: Q13 joins item 3, alongside Q3.

---

### Fifth pass: store and inventory decided (2026-09-26)

**Status: Q2, Q4 and Q14 decided (Q14 on 2026-09-28). Not yet reflected in
`proposal.md` or any spec.**

#### Decided: Q2, shared store

Recorded in [PDR-0002](../../../docs/decisions/0002-shared-license-store.md).

All license keys, from all vendors, go in one store per site. This follows from the site owner
being the primary customer. Consequence, accepted: the store's shape is a permanent contract
between library versions and can only be extended, never changed.

#### Decided: Q4, product view

Recorded in [PDR-0003](../../../docs/decisions/0003-inventory-is-a-product-view.md).

The inventory has one row per product that expects a license, so "no license found" can be
reported. Keys that match no registered product are listed separately.

```
                        SHARED STORE
          +------------------------------------+
          | forms key | seo key | commerce key |
          +------------------------------------+
                           |
                           v
   REGISTERED PRODUCTS --> INVENTORY (one row per product)
   Forms Pro   -> active, exp 2027-03
   SEO Toolkit -> expired
   Commerce    -> no license found
   (key with no registered product) -> listed separately
```

Consequence: **package self-registration is now a requirement (R9).** Each licensed package
declares that it is installed and expects a license. Q8's package-supplied links depend on the
same mechanism.

#### Q14. What does a package declare when it registers? (decided)

Recorded in [PDR-0004](../../../docs/decisions/0004-package-registration-declarations.md).

Minimum: its product ID, so it can appear as a row with no key.

| Declaration | Serves | Why |
|---|---|---|
| Display name ("Commerce") | Site owner | Product IDs read badly to people |
| Vendor name | Site owner | "Who do I call?" |
| Renewal / purchase link | Site owner | Q8: links must come from the vendor; this is where they live |
| Features the package recognises | Implementor | Could show "granted, unused", or explain a feature |
| License required or optional | Site owner | A free package with paid add-ons must not show "no license found" as an error |

Decided (2026-09-28):

- **Product ID**: required.
- **Display name**: required.
- **Vendor name**: required.
- **Renewal link**: **one** link, optional. Resolves Q8's "one link or two": no separate upgrade
  link. The library stores and displays it; everything past the link (shop, pricing, checkout)
  is the vendor's. It must be declared because the inventory is a site-level screen with no
  per-vendor hook, so without it a site owner sees an expiry with no way to act on it. Optional
  because a vendor may have no renewal page. Must not contain the license key (Q8).
- **License requirement**: required, two states: `required` or `optional`. It only changes how
  the inventory presents a **missing** key. It does not enforce anything; what the product does
  without a key is the vendor's code, and with no key no features are granted (R7).

```
  Commerce       required   no license found         [!]
  SEO Toolkit    optional   free edition
```

  Why: many Umbraco packages are free with paid extras. Without the flag every keyless package
  shows as a problem, and site owners learn to ignore real warnings. Once a key is present the
  flag is irrelevant: an expired key for an optional product is still reported, because the
  site owner bought something.

  Not included, for now: an "evaluation" state (works unlicensed for a period, then nags). It
  would need an evaluation period and an install date, and is close to the dropped R6. A
  package with an evaluation period declares `required`; "no license found" is accurate for it.

Deferred:

- **Recognised features list**: not declared for now. The inventory shows the raw feature
  names from the key. It would add friendly names, flag granted-but-unused features, and show
  features not held (upsell), but vendors would have to keep it current per release, and R7
  already ignores features a package does not recognise. Safe to add later: registration ships
  with each package release, not in the key, so adding it never affects issued keys.

The required/optional point matters: many Umbraco packages are free with paid add-ons. If every
registered package without a key is shown as a problem, site owners learn to ignore the
inventory.

#### Where to resume

1. Q13: licenses combining. It affects key contents (e.g. a capacity key saying "add 1000"
   rather than "limit is 1000"), so it belongs with the key-contents questions.
2. Q11 and Q12: small key-contents questions.
3. Q8's license-reference claim.
4. Q3: matching keys to products and handling duplicates.
5. Q1, Q5, Q6, Q7.

---

### Sixth pass: vendor issuing tooling (2026-09-28)

**Status: scope of the issuing side and Q15 decided. Not yet reflected in `proposal.md` or
any spec.** Nothing written so far said what the vendor uses to issue keys, or where the vendor
keeps products, signing secrets and issued keys. This pass settles that scope. The technology is
deferred to propose.

#### What the vendor has to keep

```
  +--------------------+   +--------------------+   +------------------------+
  | SIGNING SECRETS    |   | PRODUCTS           |   | ISSUED LICENSE KEYS    |
  | one per product,   |   | product ID, name,  |   | what was issued, when, |
  | plus replaced ones |   | feature names      |   | expiry, features, the  |
  |                    |   |                    |   | key itself             |
  +--------------------+   +--------------------+   +------------------------+
   leaked = anyone can      wrong = bad keys         lost = cannot re-send or
   forge licenses           issued                   renew; may hold customer
   lost = no new keys for                            personal data
   shipped releases
```

Losing issued keys or signing secrets hurts the site owner, who cannot get a key re-sent or
renewed. So vendor-side storage matters to the primary customer.

#### Decided: the core library signs only

Recorded in [PDR-0005](../../../docs/decisions/0005-core-signs-only.md).

The core turns key contents into a signed key string, checks the input (R7's typed values and
so on) and **keeps no records**. Vendors with a shop or CRM call it from their own systems,
including automatic issuance on purchase, and keep their own records.

#### Requirement R10: optional issuing add-on for smaller vendors (agreed)

Recorded in [PDR-0006](../../../docs/decisions/0006-issuing-add-on-without-personal-data.md).

Most Umbraco package authors are small and have no shop system. An optional add-on keeps
product definitions and issued license keys somewhere safe, and calls the core to issue, list,
re-send and renew. The core does not depend on it; a vendor with its own systems never takes it.

```
  CORE (every vendor)                 OPTIONAL ADD-ON (smaller vendors)
  +-----------------------------+     +---------------------------------+
  | sign: contents -> key       | <-- | keeps products + issued keys,   |
  | checks input                |     | calls the core to issue, lists, |
  | holds nothing               |     | re-sends, renews                |
  +-----------------------------+     +---------------------------------+
         ^
         |  vendor with a shop calls the core directly
```

Serves: vendor (small vendors get a working issuing setup), site owner (vendor can re-send
and renew reliably).

#### Decided: signing secrets are stored apart from issued keys

Recorded in [PDR-0007](../../../docs/decisions/0007-signing-secrets-stored-apart.md).

The add-on stores products and issued license keys. It does **not** store signing secrets; the
secret is supplied when a key is issued. The existing "Private key isolation" requirement in
`license-generation` is unchanged. The add-on's guidance must tell the vendor to back up the
signing secret.

Why: a store holding both is a single place from which every product's licenses can be
forged. Separating them later does not undo the exposure, because every earlier backup or
copy still holds the secrets. Removing that exposure means replacing the secret:

```
  1. new secret, new package release that trusts it
  2. old secret still trusted?  --yes--> existing keys keep working, but an old
                                         backup can still forge keys
                                --no---> every existing key stops working until
                                         reissued: cost lands on site owners
```

| Start with | Change later | Cost |
|---|---|---|
| Stored together | Separate | Moving data is cheap; removing the exposure means replacing the secret |
| Stored apart | Offer "together" as an opt-in | Low; nothing newly exposed |

#### Q15. What does the add-on record about each issued key? (decided)

Recorded in [PDR-0006](../../../docs/decisions/0006-issuing-add-on-without-personal-data.md).

An issuance log with an optional order reference. **No customer personal data.**

```
  ISSUED KEY RECORD
  +-------------------------------------------+
  | product, key contents, dates,             |
  | signing secret used, the key              |
  | order reference (optional, free text)     |
  +-------------------------------------------+
  no name, no email, no company
```

- **No personal data by design.** The add-on needs no access, deletion or retention features.
  The vendor's own shop or payment provider already holds the customer and carries those
  duties.
- **Lookup by order reference is required.** A vendor gets from a customer ("I lost my key")
  to the records by finding the order in their own system, then searching the add-on by
  reference.
- **The reference field cannot be policed.** Nothing stops a vendor typing an email into free
  text. The field's label and guidance must say it is for an order reference and must not hold
  personal data. The responsibility is the vendor's.
- **Which signing secret signed each key** is recorded, so a vendor knows which keys are
  affected if a secret leaks.
- **Customer personal details never go into the key.** The key is pasted into config and may
  be logged. This settles the personal-data half of Q1's licensee question; the site label
  in Q1 remains open.

Considered and rejected: optional name, email and company fields. Optional fields lower the
vendor's burden but not the add-on's, which would still have to support finding and removing
personal data because it cannot know whether any was entered.

Serves: vendor (small scope, no personal data duties), site owner (re-send and renewal still
work).

#### Deferred to propose (Architect)

- How the add-on stores its records (e.g. a database or a file).
- What form the issuing tooling takes (e.g. a console app shipped with the library, or a
  global tool).

#### Note on Q13

Walking through the vendor's view (release, sale, runtime, change over time) suggests Q13's
underlying question is **what one key stands for: one purchase, or everything the site owner
is currently entitled to for that product?** Earlier passes assumed the latter without deciding
it. Resume Q13 from that question.

#### Where to resume

Superseded by the list at the end of the seventh pass.

---

### Seventh pass: what a key stands for, and the license reference (2026-09-28)

**Status: Q13's model decided; license reference agreed (Q8's claim); combining rules open.
Not yet reflected in `proposal.md` or any spec.** Session parked at the end of this pass.

#### Decided: Q13, a key stands for one purchase

Recorded in [PDR-0008](../../../docs/decisions/0008-a-key-stands-for-one-purchase.md).

Two models were compared:

```
  (1) ONE PURCHASE                      (2) CURRENT ENTITLEMENT
  a key = what one order bought         a key = everything the site owner
                                        holds for that product, right now
  base      ecommerce, max 500
  add-on    ai-assist                   ecommerce, ai-assist, max 1500
  capacity  max 1000                    (one key; the next purchase
  (3 keys, combined on the site)         replaces it)
```

**(1) now; (2) is a possible future feature.**

Why (1):

- **Buy from anywhere.** Each purchase stands alone, so a marketplace or reseller can sell an
  add-on or capacity without knowing what the site owner already holds.
- **Add-ons can have their own term** (e.g. monthly add-on on a yearly base). One key has one
  expiry, so (2) cannot express this.
- **(2) needs proof of ownership that nothing here provides.** Under (2) every reissue hands over
  the whole entitlement. The vendor must confirm the buyer owns the license: the current key
  (a bearer token, emailed around), the vendor's customer account (absent at marketplaces and
  resellers, and the customer link Q15 kept out of the add-on), or a contact for the original
  buyer (personal data). A non-secret identifier is not enough; see the flaw below.
- Under (1) an add-on key grants only the add-on, so quoting someone else's identifier gets a
  stranger only what they paid for.

Cost of (1): the library must combine keys for one product. The rules are open (Q16).

Add-ons that are separately installed packages, including third-party add-ons (vendor Y
building on vendor X's product, unable to sign with X's secret), are products of their own:
they register (Q14) and carry their own keys. This needs no decision.

#### Agreed: license reference (answers Q8's claim)

Recorded in [PDR-0009](../../../docs/decisions/0009-license-reference.md).

A short identifier, generated when a license is first issued and signed into the key. It stays
the same when that license is reissued (renewal, or a future (2) upgrade). It names **a
license**: one entitlement to one product. Not a key, an order or a customer.

```
  FIRST SALE           RENEWAL              ON THE SITE
  ref LIC-8F3A         ref LIC-8F3A         inventory shows
  new, random          + later expiry       Commerce  LIC-8F3A
  key 1 ------------>  key 2                ecommerce, expires 2028-03
                       (key 1 superseded)
```

Properties:

- Random. Never derived from a customer, order or site, so it reveals nothing.
- Unique per product.
- Signed, so it cannot be edited. Readable before the signature is trusted, like the product
  ID used for routing (Q3).
- **Not secret and not proof of ownership.** It grants nothing, so it may be shown in the
  backoffice, quoted in email or support, put on an order form, or passed in the renewal link
  (Q8 forbids the key in the link; the reference is safe there).

Uses:

| Persona | Use |
|---|---|
| Site owner | "Which license is this?" Quoted for support or renewal without handling the key |
| Implementor | Sees old and new versions of one license; knows which to remove |
| Vendor | Finds one license's history in their own systems or the R10 add-on's records |
| Library | Resolves renewal duplicates (below) |

The R10 add-on stores the reference on every issued-key record, so "renew LIC-8F3A" loads the
latest record for that reference. The order reference (Q15) stays optional and is the vendor's
own bookkeeping.

The flaw that rules it out as authorisation:

```
  stranger sees "LIC-8F3A" (screenshot, support email, agency handover)
    -> buys the cheapest add-on, quoting LIC-8F3A
    -> under (2), vendor reissues "latest for LIC-8F3A + add-on"
    -> stranger receives the WHOLE entitlement
```

#### Agreed: supersede or combine, by license reference

Recorded in [PDR-0009](../../../docs/decisions/0009-license-reference.md).

```
  SAME reference       -> one supersedes the other (latest issued wins)
  DIFFERENT reference  -> separate purchases; they combine

  RENEWAL                      CAPACITY PACK
  LIC-8F3A max 500  exp 2026   LIC-8F3A  max 500
  LIC-8F3A max 500  exp 2027   LIC-21C9  max 1000
  same ref -> latest wins      different refs -> combine to 1500
  = 500, not 1000
```

- **Resolves the renewal vs capacity ambiguity** from the fourth pass, without an "add" marker
  in the key.
- **Gives Q3 its duplicate rule** for renewals: an old key left in place is superseded, not a
  conflict. Q3's routing question is still open.
- **(2) needs no new library behaviour later.** An entitlement key is a reissue under the base
  license's reference with add-ons folded in; latest wins. What (2) needs is vendor-side proof
  of ownership.
- Note for whoever designs (2): if a folded-in key and the original separate add-on key are
  both on the site, the add-on's capacity could be counted twice.

"Latest issued" implies the key records when it was issued. The current token design already
has an issued-at time (see Token format, above).

#### Q16. How do values combine across different licenses for one product? (decided in eighth pass)

Recorded in [PDR-0011](../../../docs/decisions/0011-base-and-add-on-licenses.md).

- Whole numbers presumably add. Should two base licenses bought by mistake double capacity?
- Text values: two licenses give different text. Which wins, or is it both?
- Switches: present in any valid license means granted (presumably).
- Different expiries: the product is partly valid. How is that reported (R2), and does an
  expired license's value drop out of the combination while the others remain?
- Different Umbraco version ranges across licenses for one product.

Touches: R7, R2, Q3, `license-generation` (reference claim), `license-validation` (combining,
superseding), ADR-0001's payload shape.

#### Where to resume

Supersedes all earlier lists.

1. Q16: how values combine across licenses.
2. Q11 and Q12: small key-contents questions.
3. Q3: routing keys to products (duplicate rule for renewals now settled).
4. Q8: remaining part, if any (renewal link settled by Q14; license reference settled above).
5. Q1 (licensee personal data settled by Q15; site label open), Q5, Q6, Q7.

---

### Eighth pass: combining licenses (2026-09-30)

**Status: Q16 decided; Q17 open. Not yet reflected in `proposal.md` or any spec.**

#### Decided: a key's role is base or add-on

Recorded in [PDR-0011](../../../docs/decisions/0011-base-and-add-on-licenses.md).

An add-on without a valid base license makes no sense commercially, so the library must tell
the two apart. Every key carries a signed **role**: `base` or `add-on`.

Three ways of expressing the dependency were compared:

| | How an add-on depends on a base | Verdict |
|---|---|---|
| **A** | Role marker; an add-on counts while **any** valid base for the product is present | **Chosen** |
| B | Add-on names the base license reference it extends | Rejected: keys are bearer tokens with no site binding, so this prevents no sharing. It only adds failures (mistyped reference, base re-bought under a new reference orphans the add-on) and makes resellers collect the base reference. |
| C | Nothing in the key; package code says "`ai-assist` needs `ecommerce`" | Rejected: the inventory (R2) cannot show "inactive, base expired", and every vendor re-implements a commercial rule. |

Limits on the role, so it does not reopen dropped R6 (trial / standard):

- Two values only. No third value for trials, NFR or partner keys; those remain a short expiry
  or a feature (R7), as R6's drop recorded.
- The library uses it only for combining: a base licenses the product; add-ons count only
  while a base is valid.
- The inventory (R2) shows it, so the site owner sees which license holds the product up.
- Packages do not branch on it. A package asks "is the product licensed?" and "is feature X
  granted?", never "is this key an add-on?". Anything needing that is a feature.

Inferring the role from contents (no features = add-on) was rejected: a base may carry no
features, and a capacity pack carries one.

#### Decided: the role is required at issue

Recorded in [PDR-0011](../../../docs/decisions/0011-base-and-add-on-licenses.md).

Issuing refuses a key without a role. A key without one can only be malformed and is invalid.

| Default for an unmarked key | Who pays for a vendor mistake |
|---|---|
| base | Vendor, silently: an add-on licenses the whole product |
| add-on | Site owner, visibly: a paid base license does not work |
| **none (required)** | **Nobody; the mistake fails at the vendor** |

Same principle as R7's "malformed values are rejected when the key is issued". The R10 add-on
can pre-fill the role per product.

#### Decided: capacity packs are add-ons; no subtype

Recorded in [PDR-0011](../../../docs/decisions/0011-base-and-add-on-licenses.md).

Feature add-ons and capacity packs differ only in their features (a switch vs a whole number).
Both need a valid base and never license the product alone. A capacity pack therefore cannot be
sold as a cheap standalone license.

#### Decided: values combine by type

Recorded in [PDR-0010](../../../docs/decisions/0010-product-features.md), [PDR-0011](../../../docs/decisions/0011-base-and-add-on-licenses.md).

R7's allowed types are reduced to two. **Text is dropped.**

| Type | Combined across valid licenses |
|---|---|
| Switch | Granted if any license grants it |
| Whole number | Summed, base included |

```
  LIC-8F3A  base    ecommerce, max-orders 500
  LIC-21C9  add-on  max-orders 500
  LIC-40BE  add-on  max-orders 1000
  LIC-77D0  add-on  ai-assist
  ---------------------------------------------------------
  combined          ecommerce, ai-assist, max-orders 2000
```

Why text was dropped: it has no natural combination (concatenate? latest wins, so a cheap
add-on overrides the base?), and its obvious uses have better forms. An edition name is a switch
(R7: "tier" and "feature" are one concept); a support level is a switch such as
`priority-support`; links and names belong to Q14 and Q1. Every remaining type now has a defined
combination.

#### Decided: two valid base licenses combine

Recorded in [PDR-0011](../../../docs/decisions/0011-base-and-add-on-licenses.md).

Two base licenses with **different** references combine like any other licenses. Same reference
is a renewal and supersedes (seventh pass).

```
  LIC-8F3A  base  ecommerce, max 500
  LIC-C2D1  base  ecommerce, max 500     combined max = 1000
```

- One combining rule for every license.
- A mistaken double purchase gives the site owner what they paid for; the inventory notes
  "2 base licenses" so they can ask for a refund.
- An upgrade must be issued under the existing reference (supersede), not as a new base, or it
  stacks. This matches how the future entitlement-key model (seventh pass, (2)) works.

Rejected: only one base counts, chosen by a rule. That silently discards capacity the site owner
paid for and needs a selection rule.

#### Decided: restrictions by role

Recorded in [PDR-0012](../../../docs/decisions/0012-umbraco-version-range.md).

The library enforces two restrictions. Everything else in a key identifies or entitles.

| Key content | Base | Add-on | Kind |
|---|---|---|---|
| Product ID | required | required (the base product's ID) | identifies |
| Role | required | required | combining |
| License reference | required | required | supersede or combine |
| Issued-at time | required | required | latest wins |
| Expiry | optional | optional | **restriction** |
| Umbraco version range | optional | **rejected at issue** | **restriction** |
| Features (switch, whole number) | 0..N | 0..N | entitlement |

- **Umbraco coverage is set by the base alone.** An add-on is active wherever a valid base is.
  An Umbraco upgrade can take a whole product out of range, never part of one: the inventory
  shows "Commerce: out of range", and one fix restores everything. An add-on with a range is
  rejected at the vendor rather than ignored, so a range never silently has no effect.
- **Add-ons keep their own expiry.** Subscription add-ons and capacity packs stay sellable; this
  is one of the reasons Q13 chose "a key stands for one purchase". Making expiry base-only too
  would make add-ons perpetual.
- Whole-number features such as `max-orders` are entitlements, not restrictions: the library
  reports the combined value, the package enforces it (R7).

Q12 (precision of range bounds) now applies to base licenses only.

Restrictions excluded, for reference: machine binding, domain binding, revocation before
expiry, online validation (out of scope for this change); trial kind (R6) and release-date
gating (R8) dropped in the third pass.

#### Decided: each license is judged on its own, then combined

Recorded in [PDR-0011](../../../docs/decisions/0011-base-and-add-on-licenses.md).

```
  per license                                per product
  superseded (same ref, older)  -> ignored   licensed = at least one VALID base
  invalid (tampered, malformed,              combined = features of VALID bases
    wrong product, expired,                             + ACTIVE add-ons
    base out of Umbraco range)  -> drops out   switches: any grants
  VALID add-on, no VALID base   -> INACTIVE    numbers:  summed
  otherwise                     -> counts
```

- Expiry and range are checked per license; ranges are never merged across licenses.
- An expired add-on drops out; the base and other add-ons still count.
- An add-on outliving its base is **inactive**, a state distinct from expired. It reactivates
  when the base is renewed (same reference supersedes), with no reissue.
- The inventory (R2) lists every license with its role and state (valid, inactive, expired,
  out of range, superseded, invalid) plus the combined result per product.

This answers every Q16 bullet: numbers sum, text is dropped, switches OR, partly valid products
are reported per license, and ranges are base-only.

Serves: site owner (gets what they paid for; one place to see why something stopped), vendor
(one combining rule; mistakes fail at issue), implementor (states say which key to fix). Site
visitor unaffected.

Touches: R7 (text dropped), R2 (role and inactive state in the inventory), Q3,
`license-generation` (role claim, required; range rejected on add-ons; text type removed),
`license-validation` (per-license filter, combining, product-level verdict), ADR-0001's payload
shape.

#### Add-on license vs add-on product

Two different things share the word "add-on":

| | Add-on license | Add-on product |
|---|---|---|
| What | A purchase extending a product | A separate package building on another product |
| Product ID | The base product's | Its own |
| Signed by | The base product's vendor | Its own vendor, possibly a third party |
| Installed | Nothing new; capability is in the package | A separate package |
| Needs | A valid base for the **same** product | Its **own** base license |
| Combines with | The base's features | Nothing; products never combine |

Everything above concerns add-on licenses. Add-on products were settled in the seventh pass:
ordinary products that register (Q14) and carry their own base and add-on licenses. A third
party cannot sign for another vendor's product, so its extension is always a product.

#### Q17. Should the library model dependencies between products? (open, parked)

The "no add-on without a valid base" rule has no equivalent across products:

```
  Commerce            base EXPIRED   -> not licensed
  Commerce Shipping   base valid     -> licensed
```

- **(x) Leave it to the add-on product (leaning).** The library already answers "is product P
  licensed?"; Shipping can ask about Commerce. Nothing new in the key or library. Package
  installation already handles product dependencies.
- (y) Model it: Shipping's key or registration declares it requires Commerce; the library
  enforces it; the inventory shows "Shipping inactive: Commerce not licensed".
- (z) Nothing; each product stands alone.

Open: should the site owner see the reason in the inventory, which only (y) gives?

Touches: R2, R9 / Q14 (registration), `license-validation`.

#### Where to resume

Supersedes all earlier lists.

1. Q11 and Q12: small key-contents questions (Q12 now base licenses only).
2. Q3: routing keys to products (renewal duplicates and multiple bases now settled).
3. Q8: remaining part, if any (renewal link settled by Q14; license reference settled).
4. Q1 (licensee personal data settled by Q15; site label open), Q5, Q6, Q7.
5. Q17: product dependencies, parked.

---

### Ninth pass: feature names and version bounds (2026-09-30)

**Status: Q11 and Q12 decided. Not yet reflected in `proposal.md` or any spec.**

#### Decided: Q11, feature names are restricted, lookups ignore case

Recorded in [PDR-0013](../../../docs/decisions/0013-feature-names.md).

- A feature name is lowercase `a-z`, digits and hyphens, starting with a letter.
- Issuing rejects any other name.
- A package's lookup ignores case, so asking for `Pro` finds `pro`.

Why: combining (eighth pass) makes spelling matter across licenses. A name split by case or
punctuation splits one feature into two, and the site owner silently loses what they paid for:

```
  LIC-8F3A  base    max-orders 500
  LIC-40BE  add-on  Max-Orders 1000
  case-sensitive  -> package sees 500, site owner paid for 1500
```

Restricting the name at issue applies R7's "malformed values are rejected when the key is
issued" to names: the mistake fails at the vendor.

| Option | Rejected because |
|---|---|
| Any characters, matched ignoring case; issue rejects names differing only by case | Misses trailing spaces, `_` vs `-`, look-alike letters from other alphabets, and letters whose case changes with the server's language setting (Turkish dotted / dotless i), which can give different answers on different servers |
| Case-sensitive exact match | Exposes the site owner to the silent split above |

Cost: names such as `AI Assist` are not possible; the inventory shows `ai-assist`. A separate
display name is not requested.

Touches: `license-generation` (name rule), `license-validation` (feature lookup).

#### Decided: Q12, Umbraco range bounds are majors, inclusive, each optional

Recorded in [PDR-0012](../../../docs/decisions/0012-umbraco-version-range.md).

- A base license may carry a minimum and a maximum Umbraco **major** (eighth pass: add-ons
  may not carry a range).
- Both bounds are inclusive. A bound covers every minor, patch and pre-release of its major.
- Either bound may be omitted: `17`..none is 17 and every later major; none..`18` is up to and
  including 18; neither is any version.
- Contiguous only.
- Issuing rejects non-whole-number bounds and a minimum above the maximum.

Why: the range is a commercial boundary ("Umbraco 19 is a paid upgrade"), and Umbraco is sold
and upgraded by major. Majors only means a routine minor or patch update never takes a product
out of range; only a major upgrade, which the site owner plans for, can.

| Option | Rejected because |
|---|---|
| Full-version (semver) bounds, major-only shorthand allowed | Motivating case was a package release depending on a CMS feature added mid-major (e.g. 17.3). That is a **compatibility** fact about one release, already enforced by the package's install requirements. A key is signed once and covers every release, so the fact would be frozen into keys (still saying 17.3 after the dependency is removed), and the site owner on 17.1 would see "license out of range" when the fix is a CMS update. No commercial use for minor precision was identified. |
| List of majors (e.g. `17, 19`) | Allows gaps (an LTS-only license), but cannot express "17 and later". Open-ended coverage was preferred. |

Touches: `license-generation` and `license-validation` range requirements, tasks.md task 2.1
and task 4.5.

#### Where to resume

Supersedes all earlier lists.

1. Q3: routing keys to products (renewal duplicates and multiple bases settled).
2. Q8: remaining part, if any (renewal link settled by Q14; license reference settled).
3. Q1 (licensee personal data settled by Q15; site label open), Q5, Q6, Q7.
4. Q17: product dependencies, parked.

---

### Tenth pass: key schema details (2026-09-30)

**Status: five schema gaps decided. Not yet reflected in `proposal.md` or any spec.** Writing
out the key schema in [`docs/license-examples.md`](../../../docs/license-examples.md) exposed
five questions no earlier pass covered.

#### Decided: numbers are decimals, zero or positive, additive only

Recorded in [PDR-0010](../../../docs/decisions/0010-product-features.md).

- R7's "whole number" becomes "number": decimals allowed, **zero or positive**, at most 4
  decimal places and 15 digits in total. Issuing rejects anything else.
- Numbers are **additive quantities** (counts, limits, amounts). Rates and factors belong in
  package settings.
- Sums are exact at the precision written.

Why: numbers are summed across licenses (eighth pass). A negative value is the only way holding
more licenses gives a site owner less, and can drive a total below zero; corrections are a
reissue under the same reference. Summing suits `storage-gb` (2.5 + 10) but not a rate
(`discount-rate` 0.15 + 0.15 = 0.30). A precision limit gives "exact" a fixed meaning.

Rejected: negatives; no precision limit; 2 decimal places.

#### Decided: a feature name appears at most once per key

Recorded in [PDR-0010](../../../docs/decisions/0010-product-features.md).

Issuing rejects a repeated name, whatever its type. A duplicate is almost always a mistake.
Rejected: summing within the key (hides mistakes), last wins (silent, order-dependent).

#### Decided: `issued` and `expires` are UTC; the core sets `issued`

Recorded in [PDR-0016](../../../docs/decisions/0016-dates-and-times.md).

```
  issued   2026-03-01T09:14Z     UTC date and time, set by the core at signing
  expires  2027-03-01            valid until 2027-03-01 23:59:59 UTC
  inventory shows the expiry moment in the viewer's local time
  issuing rejects an expiry before the current UTC date; today is allowed
```

- `issued` only orders reissues; same-day reissues (a corrected typo) need a time.
- Set by the core so ordering cannot be broken by a backdated or future-dated value. Cost: a
  migrating vendor cannot keep original issue dates in keys.
- UTC for both for consistency. Site owners west of UTC lose up to 12 hours of the stated
  expiry day; contained by the local-time display, advance warning, and vendors adding a day.

Rejected: "anywhere on Earth" expiry; server-local time; exact expiry time; date-only
`issued`; `issued` as "valid from" or a separate "valid from" date (collides with superseding:
a not-yet-started reissue would supersede the current key and leave a gap; early renewal
already works without it).

#### Decided: identifier formats

Recorded in [PDR-0017](../../../docs/decisions/0017-identifier-formats.md).

- **Product ID:** `vendor.product`, two dot-separated parts of lowercase `a-z`, digits and
  hyphens, each starting with a letter. The shared store (PDR-0002) puts independent vendors'
  IDs side by side; a vendor prefix keeps them apart and readable. Rejected: free text
  (collisions), the package's published ID (renamed or split packages; product IDs are
  permanent), random identifiers (unreadable in the inventory).
- **License reference:** 10 random characters from uppercase letters and digits without
  `0 O 1 I L`, shown `LIC-7K3QM-X9P2D`, matched ignoring case, hyphens and spaces. Generated by
  the core at first issue; passed in on reissue. The core keeps no records, so uniqueness comes
  from randomness; the issuing add-on or the vendor's systems check their own records.

Touches: `license-generation` (product ID, reference, number and name rules; `issued` no longer
an input; past-expiry rejection), `license-validation` (expiry evaluated in UTC, exact sums),
R2 (local-time expiry display), ADR-0001's payload shape.

#### Where to resume

Supersedes all earlier lists.

1. Q3: routing keys to products (renewal duplicates and multiple bases settled).
2. Q8: remaining part, if any (renewal link settled by Q14; license reference settled).
3. Q1 (licensee personal data settled by Q15; site label open), Q5, Q6, Q7.
4. Q17: product dependencies, parked.

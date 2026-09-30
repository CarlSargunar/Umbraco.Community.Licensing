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

### Version comparison and clock access are injectable

- Expiry checks use `TimeProvider` (BCL, .NET 8+) rather than `DateTime.UtcNow` directly, so validation logic is deterministically unit-testable without wall-clock dependence.
- The "running Umbraco core version" is supplied to the validator by the caller (or via a small injectable accessor) rather than the validation library reaching into Umbraco's assembly metadata itself. This keeps `license-validation`'s core logic testable in isolation and avoids a hard compile-time dependency from the validation core onto a specific Umbraco assembly shape.

**Why:** Both are standard testability seams; without them, expiry and version-range scenarios from the specs would be difficult to test deterministically.

### Package split to isolate the Azure Key Vault dependency

A BCL-only core package (generation, validation, configuration and environment-variable
sourcing) and a separate Key Vault sourcing package. Rationale and alternatives:
[ADR-0002](../../../docs/adrs/0002-package-split-for-keyvault-dependency.md) (draft). Q7 below
suggests a third package for the backoffice screen.

## Risks / Trade-offs

- **[Risk] No revocation before expiry** (pure offline signed tokens can't be revoked once issued) → **Mitigation:** Document this as an accepted limitation; issuers wanting revocation should favor shorter expiry windows plus a renewal flow. Out of scope per the proposal.
- **[Risk] Clock rollback can defeat expiry** (whoever runs the server controls its clock) → **Mitigation:** Documented, accepted limitation common to all offline-licensing schemes; not otherwise mitigated in this change.
- **[Risk] Private key compromise invalidates trust in every license signed with it** → **Mitigation:** Key rotation (key ID) lets an issuer stop trusting a compromised key going forward, but existing licenses signed with it remain cryptographically valid until they individually expire or the issuer explicitly stops trusting that key ID (accepting that this also invalidates any still-valid legitimate licenses under that key). This trade-off is inherent to offline verification and is documented rather than solved here.
- **[Risk] No machine/domain binding** → **Mitigation:** Explicitly out of scope per the proposal; a valid key can be reused across installs until this is addressed in a future change.

## Migration Plan

Greenfield change - no existing consumers or data to migrate. Initial release ships the core package and the Azure Key Vault sourcing package together as a paired version.

## Technical open questions

- Exact `keyId` derivation scheme (e.g., truncated hash of the public key vs. an issuer-assigned label) can be finalized during implementation; either choice satisfies the specs and design decisions above without changing them.

---

## Exploration since the proposal

`proposal.md`, the three delta specs and `tasks.md` describe **a single license key per site**
and predate everything in this section. Nothing is implemented, so revising them is a clean
rewrite, not a migration. Product decisions are recorded as PDRs in
[`docs/decisions/`](../../../docs/decisions/README.md); the key schema and worked examples are
in [`docs/license-examples.md`](../../../docs/license-examples.md). This section holds what
those do not: the requirement and question index, the open questions, and prior art. The
chronological write-up of the exploration is in git history (commit `8f9c557` and earlier).

All key contents are decided (schema in `license-examples.md`). Of the open questions, only Q17
option (y) and Q18 could change what a key holds; the rest are outside the key.

### Requirements

| # | Requirement | State | Recorded |
|---|---|---|---|
| R1 | Named collection of license keys: 1..N independent entries per site, each with a human-readable name | open: Q1, Q3 | below |
| R2 | Inventory: every registered product with its licenses, their states and the combined entitlement | open: Q5 | below; shape set by PDR-0003, PDR-0004, PDR-0011, PDR-0016 |
| R3 | Backoffice screen for licenses | open: Q6, Q7 | below |
| R4 | Renewal link | decided | PDR-0004 (one optional link, declared by the package); PDR-0009 (the reference, never the key, may go in it) |
| R5 | Feature flags | superseded by R7 | |
| R6 | Kind of license (trial / standard) | dropped | PDR-0014 |
| R7 | Product features: switches, numbers and text | decided | PDR-0010, PDR-0013, PDR-0018 (text; revisited 2026-09-30) |
| R8 | Release-date gating | dropped | PDR-0015 |
| R9 | Package self-registration | decided | PDR-0003, PDR-0004 |
| R10 | Optional issuing add-on for smaller vendors | decided | PDR-0005, PDR-0006, PDR-0007 |
| R11 | Site visitor: no key, status or licensing message reaches a public visitor; a licensing problem never crashes a page; the library reports and does not throw in the request path; what visitors experience on a failed license is the vendor's decision | agreed, not yet specified | [`docs/personas.md`](../../../docs/personas.md), PDR-0001 |

Touches when specified: R1 reshapes `license-key-sourcing` (sources yield entries, not one
string); R2 is a substantial addition to `license-validation`, possibly its own capability; R3
is plausibly a separate change; R11 touches `license-validation`.

### Questions

| Q | Question | Outcome |
|---|---|---|
| Q1 | Where does a stored key's human-readable name live? | open |
| Q2 | Shared or per-vendor store? | PDR-0002: shared |
| Q3 | How does a package find its keys? | open; duplicates settled by PDR-0009, PDR-0011 |
| Q4 | Inventory: store view or product view? | PDR-0003: product view |
| Q5 | How are unverified claims marked in the inventory? | open |
| Q6 | Backoffice screen: view-only or read-write? | open |
| Q7 | Who ships the screen, and what may it display? | open |
| Q8 | Where the renewal link comes from; the license reference | PDR-0004 (declared at registration, one link); PDR-0009 (reference) |
| Q9 | Feature model: explicit list or named tier? | PDR-0010: one concept, a vendor convention |
| Q10 | Do features overlap the Umbraco version range? | PDR-0010, PDR-0012: orthogonal |
| Q11 | Feature name rules | PDR-0013 |
| Q12 | Precision of the version range bounds | PDR-0012 |
| Q13 | What one key stands for | PDR-0008: one purchase |
| Q14 | What a package declares at registration | PDR-0004 |
| Q15 | What the issuing add-on records | PDR-0006 |
| Q16 | How licenses for one product combine | PDR-0011, PDR-0010, PDR-0012; PDR-0018 (text never combines) |
| Q17 | Dependencies between products | parked |
| Q18 | Same reference, different role or product | open |

### R1. A named collection of license keys

The implementor supplies **1..N license key entries** rather than one, in one store shared by
all vendors (PDR-0002). Each entry is independent: its own product, expiry, range, features and
validity. One expired entry must not affect the evaluation of any other. Several entries for
one product is a normal case (PDR-0008, PDR-0011), not an error.

Each entry additionally carries a **human-readable name**, so that when a key needs replacing
the implementor can tell which entry to edit. Where that name lives is Q1.

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

The store's shape is a permanent contract between library versions (PDR-0002). A key must
survive being pasted into an email, a configuration value, an environment variable or a vault
secret.

### R2. Inventory

`license-validation` must be able to return **every registered product** (PDR-0003) with its
licenses, each license's role and state (valid, inactive, expired, out of range, superseded,
invalid; PDR-0011), the combined entitlement per product, the registration data (display name,
vendor, renewal link, required or optional; PDR-0004), and keys that match no registered
product, listed separately. Expiry is shown as the exact moment in the viewer's local time
(PDR-0016). This is what lets a site owner or implementor answer "what am I licensed for, and
what is about to break?" before it breaks.

Two structural points:

- **Authenticity and usability are independent axes.** A key can be perfectly authentic and
  expired; another can be forged. Collapsing these into a single status word loses the
  difference between "someone tampered with this" and "you need to renew", which are very
  different actions for the implementor.

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
  that silently drops unverifiable entries omits exactly the rows the implementor needs. One
  that includes them unmarked presents a forger's numbers as fact. See Q5.

Two readers: the implementor needs to know which configured entry to fix and why; the site
owner needs to know what they are licensed for and what is about to expire. Same data,
different questions. An inventory can only cover packages built on this library, so it can
look complete when it is not.

### R3. Backoffice screen

An Umbraco backoffice screen for working with the configured collection: each entry's expiry,
the products it permits, its state, and managing the keys themselves. "Manage" is the problem
(Q6); who ships it and what it may show is Q7. Can ship later, as a separate package or change;
nothing in the key depends on it.

### Q1. Where does a stored key's human-readable name live? (open)

A name can sit **outside** the key, written by the implementor, or **inside** the signed
payload, written by the vendor at issue. These look like the same field but answer different
questions:

- **Site label = an address.** "Which slot do I edit?" Authored by the implementor, who will
  edit it.
- **Vendor name = an identity.** "What is this thing?" Bound to the other claims by the
  signature.

Four scenarios discriminate the options, and no single field covers all four:

| # | Scenario | Site label | Vendor name |
|---|---|---|---|
| A | **Mangled paste** - key truncated on copy, nothing inside it is readable | works - the only thing that can name a broken entry | fails - gone with the key |
| B | **Mis-paste** - a valid Commerce key sits in the slot labelled "Forms Pro" | fails - confidently wrong, implementor scans past it | works - row can flag the contradiction |
| C | **Renewal drift** - label reads "expires Mar 2026", key was renewed a year ago | fails - hand-written metadata decays | works - regenerated with each key |
| D | **Implementor's own scheme** - same product across prod/staging, or two sites | works - "prod, renewed by Jane" | fails - every key says "Forms Pro" |

Options: (a) site label only, covers A and D; (b) vendor name only, covers B and C; (c) both,
the only option that catches a mis-paste, at the cost of two name fields to display and
explain, a new claim, and a mismatch-reporting rule; (d) no name, identity comes from the
product ID, fails A and D.

Narrowed by later decisions:

- No licensee name, email or company in the key (PDR-0006). A vendor-written name could only
  name the product.
- Registration (PDR-0004) gives every registered product a display name and vendor name, so a
  name inside the key adds information only for keys whose product is not registered, which
  PDR-0003 lists by product ID anyway.
- The license reference (PDR-0009) is signed, survives renewal (scenario C), and can be quoted
  for support. The site label's remaining job is scenario A (an unreadable entry) and D (the
  implementor's own scheme).

Still to decide: whether a site label exists at all, and if so how it survives all three
sources. It is natural in a structured configuration file, but an environment variable is one
string (the label would have to live in the variable's name or be encoded into its value), and
a vault secret's own name is the only plausible label there.

### Q3. How does a package find its keys? (open)

Two models:

- **Implementor assigns**: the implementor declares which entry belongs to which product. More
  work, one more thing to get wrong.
- **System routes**: the implementor drops every key in, in any order, and each package finds
  its own by matching product ID. Better experience, but it means each key's product ID (and
  role and reference) is read **before** the signature is trusted, for routing only.

Two decisions already presuppose system routing: PDR-0009 says the reference is readable before
the signature is trusted "like the product ID used for routing", and PDR-0003 lists keys that
match no registered product, which needs the product ID read from every key. If that is the
decision, record it as a PDR with an explicit requirement: fields read before verification are
used only to route and to label a row, and are never trusted until the signature verifies.

Settled: an old key left in place at renewal is superseded, not a conflict (PDR-0009); several
licenses for one product combine (PDR-0011). Remaining beside the model: what the inventory
shows for a key whose product ID cannot be read (Q5).

### Q5. How are unverified claims marked in the inventory? (open)

Rows fall into three tiers:

```
  tier 1  unreadable         -> only the site label (Q1) is knowable
  tier 2  readable, not      -> claims exist but are ASSERTIONS, not facts.
          authentic             "Expires 2099" on a forged key must never
                                be displayed as truth.
  tier 3  authentic          -> claims are facts
```

A row must carry an explicit "these values are unverified" marker, distinct from its state.
Open: how that is surfaced, and whether tier 2 claims are returned at all or suppressed in
favour of the label alone.

Two attached points, both now largely settled by other records:

- **Days remaining.** PDR-0016 relies on the inventory giving advance warning of expiry, so
  a countdown or warning threshold is needed; without it every consumer recomputes it.
- **Raw key values.** Keys are bearer tokens. `docs/personas.md` says a backoffice editor must
  never be able to read a full key, so the inventory must not reproduce keys in full. A short
  identifying fragment, or the license reference (PDR-0009), lets an implementor match a row
  back to the store.

### Q6. Is the backoffice screen view-only, or read-write? (open)

All three planned sources are effectively read-only at runtime:

```
  appsettings.json   -> often read-only in production (containers,
                        source-controlled, deployed rather than edited)
  environment var    -> not writable at runtime in any meaningful sense
  Key Vault          -> writable in principle, but needs a write permission
                        most implementors will not grant the application
```

A view-only screen fits this. An editing screen requires a **writable store the site owns**, a
source that does not exist in the proposal, and creates a precedence problem:

```
   appsettings.json:  forms.pro = eyJ...OLD
   UI-managed store:  forms.pro = eyJ...NEW
                              |
                        which one wins?
```

Underneath sits a split in how a site is operated. Config-sourced keys are deployed: they flow
through the pipeline, sit in source control, and keep environments in step. UI-managed keys are
runtime state: a key added on staging does not exist in production. Supporting both means a
precedence rule and the resulting confusion. A view-only screen that reports status and points
the implementor at *where* to edit sidesteps the question, at the cost of not being a manager
in the fullest sense.

### Q7. Who ships the screen, and what may it display? (open)

If the screen lives in the core library and five vendors each ship packages built on it, five
copies attempt to register the same backoffice section. The licensing screen is a
**site-level concern**: a site has one, not one per vendor. That argues for a third package the
implementor installs once, beside the core and Key Vault packages (ADR-0002). The shared store
(PDR-0002) makes a single screen possible.

Who may see it: the backoffice editor uses the product but does not manage licenses. Whether
editors see licensing warnings ("expires in 14 days"), and the rule that nobody sees a full key
(Q5), are part of this question.

### Q17. Should the library model dependencies between products? (parked)

An add-on **license** counts only while a base for the same product is valid (PDR-0011). An
add-on **product** (a separately installed package, possibly from a third party) is a product
of its own with its own licenses and never combines. There is no equivalent rule across
products:

```
  Commerce            base EXPIRED   -> not licensed
  Commerce Shipping   base valid     -> licensed
```

- **(x) Leave it to the add-on product (leaning).** The library already answers "is product P
  licensed?"; Shipping can ask about Commerce. Nothing new in the key or library. Package
  installation already handles product dependencies.
- (y) Model it: Shipping's key or registration declares it requires Commerce; the library
  enforces it; the inventory shows "Shipping inactive: Commerce not licensed". If the
  declaration is in the key rather than the registration, this adds a claim and must be settled
  before any key is issued.
- (z) Nothing; each product stands alone.

Open: should the site owner see the reason in the inventory, which only (y) gives?

Touches: R2, R9 / Q14 (registration), `license-validation`.

### Q18. Same reference, different role or product (open)

Superseding is by license reference: latest issued wins (PDR-0009). Two cases are undefined.

- **Same reference, different role.** The core keeps no records (PDR-0005), so nothing stops a
  vendor reissuing an add-on under a base's reference, or the reverse. Latest-wins then
  replaces a base with an add-on (the product stops being licensed) or an add-on with a base
  (the site owner gets a product they did not buy). Options: latest wins regardless, so the
  vendor's mistake lands on the site owner; the site treats the pair as a conflict shown in the
  inventory; the issuing add-on (PDR-0006) refuses a reissue that changes the role, and vendors
  with their own systems are told to do the same, which follows "mistakes fail at the vendor"
  (PDR-0010, PDR-0011) but cannot be enforced by the core.
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
| All failures returned together, each with a `HowToResolve` hint | Relevant to R2 (a row wants every reason) and R4 (a hint is a simple renewal link); not a requirement |
| Conditional and vendor-defined validation rules | Not pursued |

What this project has that it does not: a product ID claim (it identifies a product by giving
each product its own key pair, so a key cannot be matched to its package by reading it; Q3's
routing needs the claim), key ID and rotation, the Umbraco version range, key sourcing, and
everything in R1-R4.

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

1. **Q3 and Q18**: routing, and the superseding edge cases. Both change `license-validation`
   text.
2. **Q1**: the site label, together with R1's sourcing shape.
3. **Q5**: inventory marking; then R2 can be specified.
4. **Q6 and Q7**: R3, plausibly as a separate change.
5. **Q17**: parked.
6. Then revise `proposal.md`, the three delta specs and `tasks.md` against the PDRs and
   `docs/license-examples.md`, and finish ADR-0001's payload.

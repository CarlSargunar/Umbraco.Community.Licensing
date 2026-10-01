# Deferred scope: Umbraco and host-side features

On 2026-10-01 the Product Owner reduced the `license-key-management` change to a library for
license key **generation**, **verification** and **signing-key management**. Everything that
depends on Umbraco, or on the library running inside a host site, was taken out of the change
and parked here. Nothing here is dropped. Each item is a candidate for a later change, which
should start from this file and the records it points to.

The cut line: the core library takes license key strings, a product ID, a set of trusted
public keys and a clock, and returns results. Where the strings come from, which products are
installed, and how results are shown are the host's concern.

## What was removed

| # | Item | Was recorded in | Why it is host-side |
|---|---|---|---|
| D1 | **Umbraco version range claim** (`umbraco.min` / `umbraco.max`) and its validation check | PDR-0012; `license-examples.md` schema, example 10, rejected-at-issue rows; `license-validation` spec | The check needs the running Umbraco major, which only the host knows. The claim is named for Umbraco |
| D2 | **Key sourcing** from .NET configuration, environment variables and Azure Key Vault, and the provider abstraction | `license-key-sourcing` spec; ADR-0002; tasks 1.2, 5, 6 | The library now takes key strings from the caller. Sourcing is host plumbing and is where the store shape (D3) and site label (D7) attach |
| D3 | **Shared license store**: one store per site for all vendors, with a permanent shape | PDR-0002 | A store only exists inside a host site |
| D4 | **Package registration**: each package declares product ID, display name, vendor name, renewal link and `required` / `optional` | PDR-0003, PDR-0004; design.md R9, Q14, Q8 | Registration is a runtime act of an installed package |
| D5 | **Inventory**: one row per registered product, missing-license rows, keys matching no registered product listed separately, local-time expiry display, advance expiry warning, "2 base licenses" note, feature-conflict marking per product row | PDR-0003; design.md R2, Q4, Q5; clauses in PDR-0011, PDR-0016, PDR-0018 | Needs registration (D4) and a store (D3). The core keeps the per-product **evaluation result** (below), which is the inventory's input |
| D6 | **Backoffice screen** for licenses, and who ships it | design.md R3, Q6, Q7 | Umbraco backoffice |
| D7 | **Site label**: an implementor-written name per stored key | design.md R1, Q1 | Lives beside the stored key, outside the signed payload, so it belongs to the store (D3) |
| D8 | **Routing** keys to packages by reading the product ID before the signature is trusted | design.md Q3 | Only needed when one store holds every vendor's keys. In the core, the caller passes the keys it wants evaluated; a key for another product is reported as wrong product and drops out (PDR-0011) |
| D9 | **Dependencies between products** (an add-on product shown inactive when its base product is unlicensed) | design.md Q17 (parked) | Needs registration (D4) to declare the dependency and the inventory (D5) to show it |
| D10 | **Renewal link** | PDR-0004 (declared at registration); design.md R4 | Part of registration (D4) |
| D11 | **Target "Umbraco 17+"** | proposal.md | The library targets .NET 10 only |

## What stays in the core, renamed where needed

- **Evaluation result.** For one product and a set of keys: each key's state (valid, invalid
  with reason, superseded, inactive) and the combined entitlement (licensed or not, switches,
  summed numbers, text values or conflict). PDR-0009, PDR-0011, PDR-0010, PDR-0018 apply as
  written. Where those PDRs say "the inventory shows" or "the inventory flags", read "the
  evaluation result reports"; the inventory (D5) is a host view over evaluation results.
- **License states.** PDR-0011's list loses *out of range* (D1). The remaining states are
  superseded, invalid (tampered, malformed, wrong product, expired), inactive and valid.
- **Reporting, not throwing.** design.md R11: the library returns results and never throws on
  a bad key. This is a core behaviour, not a host one, and remains in scope.
- **Signing-key management.** Key pair creation, key IDs, trusted key sets and rotation. Named
  in the new scope; previously spread across the generation and validation specs.
- **Issuing add-on** (PDR-0005, PDR-0006, PDR-0007). Not Umbraco and not removed, but it was
  never part of this change. It stays a separate future change.

## Records touched by the cut

| Record | Change made on 2026-10-01 |
|---|---|
| PDR-0002, PDR-0003, PDR-0004, PDR-0012 | Status set to Deferred, pointing here. Text kept as the record of the decision |
| PDR-0011 | Amended: *out of range* removed from the license states; the inventory listing clause is deferred (D5) |
| PDR-0016 | Amended: the local-time display and advance-warning clauses are deferred (D5); the UTC rules and the issue-time rule stand |
| PDR-0015 | Unchanged. Its reasoning cites PDR-0012 as giving enough commercial control; with D1 deferred that reasoning is weaker. Revisit if D1 is not restored |
| ADR-0001 | Payload description no longer lists the Umbraco major range |
| ADR-0002 | Status set to Deferred with D2 |
| `license-examples.md` | `umbraco` rows, example 10 and the Umbraco rejected-at-issue rows removed; example 13's Q17 note removed |
| `license-key-sourcing` delta spec | Deleted from the change. Its text is reproduced below |

## Requirement and question index

PDRs cite design.md R and Q numbers. These left the change and are indexed here so the
citations still resolve.

| # | Item | Outcome |
|---|---|---|
| R1 | Named collection of license keys per site | D2, D3, D7 |
| R2 | Inventory across registered products | D5; the per-product evaluation result stays as design.md R12 |
| R3 | Backoffice screen | D6 |
| R4 | Renewal link | D10 |
| R9 | Package self-registration | D4 (PDR-0003) |
| R13 | Umbraco version range | D1 (PDR-0012) |
| Q1 | Where does a stored key's human-readable name live? | D7 |
| Q2 | Shared or per-vendor store? | PDR-0002 (shared), D3 |
| Q3 | How does a package find its keys? | D8 |
| Q4 | Inventory: store view or product view? | PDR-0003 (product view), D5 |
| Q6 | Backoffice screen: view-only or read-write? | D6 |
| Q7 | Who ships the screen, and what may it display? | D6 |
| Q8 | Where the renewal link comes from | PDR-0004, D10. The reference half of Q8 stays in design.md |
| Q10 | Do features overlap the version range? | PDR-0010: orthogonal. Range now D1 |
| Q12 | Precision of the version range bounds | PDR-0012, D1 |
| Q14 | What a package declares at registration | PDR-0004, D4 |
| Q17 | Dependencies between products | D9 |

## Open questions carried out of the change

| Q | Question | Deferred with |
|---|---|---|
| Q1 | Where does a stored key's human-readable name live? | D7, D3 |
| Q3 | How does a package find its keys? | D8 |
| Q5 | How are unverified claims marked in the inventory? The core part, what the evaluation result reports for a key that fails verification, stays open in design.md | D5 |
| Q6 | Backoffice screen: view-only or read-write? | D6 |
| Q7 | Who ships the screen, and what may it display? | D6 |
| Q17 | Dependencies between products | D9 |

## Design notes worth keeping

Condensed from design.md as it stood before the cut (full text in git history, commit `fcda40b`).

- **Site label vs vendor name (Q1).** A site label is an address ("which slot do I edit?"),
  written by the implementor. A vendor-written name is an identity, bound by the signature.
  Four scenarios: mangled paste (only a label helps), mis-paste (only a signed identity catches
  it), renewal drift (hand-written labels decay), implementor's own scheme (prod vs staging).
  Later decisions narrowed this: no personal data in keys (PDR-0006), registration gives every
  product a display name (PDR-0004), the reference is signed and quotable (PDR-0009). The
  label's remaining jobs are the mangled paste and the implementor's own scheme. An environment
  variable is one string, so a label there must live in the variable's name; a vault secret's
  own name is the only plausible label there.
- **Routing (Q3).** Implementor assigns vs system routes. PDR-0009 and PDR-0003 presuppose
  system routing. If adopted, record it with an explicit rule: fields read before verification
  are used only to route and label a row, never trusted until the signature verifies.
- **Unverified claims (Q5).** Three tiers: unreadable (only the label is knowable), readable
  but not authentic (claims are assertions; "expires 2099" on a forged key must never be shown
  as truth), authentic (claims are facts). Authenticity and usability are independent axes.
  Keys are bearer tokens: the inventory must never reproduce a key in full; the reference or a
  short fragment identifies a row.
- **Screen read-write (Q6).** All planned sources are effectively read-only at runtime. An
  editing screen needs a writable site-owned store and a precedence rule against
  config-sourced keys, and splits deployed keys from runtime state. A view-only screen that
  points at where to edit sidesteps this.
- **Who ships the screen (Q7).** If the core shipped it, every vendor's package would register
  the same section. It is a site-level concern: one package the implementor installs once. The
  backoffice editor may see warnings; nobody sees a full key.
- **Product dependencies (Q17).** (x) leave it to the add-on product, which can ask whether its
  base product is licensed (leaning); (y) model it in registration or the key, so the inventory
  can show "Shipping inactive: Commerce not licensed"; (z) nothing. Only (y) gives the site
  owner the reason.
- **Umbraco version range (D1).** If restored, consider generalising it to a host version range
  so the claim is not named for one host. PDR-0012's rules (base only, whole majors, inclusive,
  either bound optional, contiguous) do not depend on the host being Umbraco.

## `license-key-sourcing` delta spec, as removed

```
## Purpose

Provides a pluggable provider abstraction for supplying the raw license key string to a host
application from .NET configuration, environment variables, or Azure Key Vault.

Serves the implementor, who installs the keys a site owner supplies.

### Requirement: Common provider abstraction
The system SHALL define a common abstraction for obtaining a raw license key string, so that
multiple source implementations can be used interchangeably by callers.
  Scenario: Swappable providers. WHEN a host application configures a different license key
  source implementation THEN the rest of the application code that requests the license key
  SHALL be unaffected by the change.

### Requirement: .NET configuration provider
The system SHALL provide a built-in provider that reads the license key from standard .NET
configuration (appsettings.json and any other source layered into IConfiguration).
  Scenario: Key present in configuration. WHEN a license key is present at the configured
  configuration key path THEN the configuration provider SHALL return that value.

### Requirement: Environment variable provider
The system SHALL provide a built-in provider that reads the license key from a configurable
environment variable name.
  Scenario: Key present in environment. WHEN a license key is present in the configured
  environment variable THEN the provider SHALL return that value.

### Requirement: Azure Key Vault provider
The system SHALL provide a built-in provider that reads the license key from a configurable
Azure Key Vault secret, without requiring host applications that do not use this provider to
take on a dependency on Azure Key Vault client libraries.
  Scenario: Key present in Key Vault. WHEN a license key is stored as the configured secret in
  an accessible Azure Key Vault THEN the provider SHALL return that secret's value.
  Scenario: Key Vault dependency isolation. WHEN a host application does not reference the
  Key Vault provider THEN it SHALL NOT be required to depend on Azure Key Vault client libraries.

### Requirement: Missing key handling
The system SHALL clearly report when a configured source has no license key available,
distinguishing "no key configured" from "key present but invalid".
  Scenario: No key configured. WHEN a configured source has no value for the license key THEN
  the provider SHALL report that no key was found rather than returning an empty value.
```

Pre-cut design.md R1 added: sources yield 1..N independent entries, not one string; one
expired entry must not affect any other; several entries for one product is normal.

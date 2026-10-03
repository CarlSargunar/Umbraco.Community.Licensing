# Umbraco Licensing

A shared licensing library for paid Umbraco marketplace packages.

Umbraco package vendors have no common way to license their work: each vendor either rolls an
ad-hoc key scheme or ships unprotected. This library provides one any package can adopt: a
product-scoped license key that is **verifiable entirely offline** (no phone-home, no licensing
server), carrying an optional expiry and product features, with signing key rotation from the
start.

Targets .NET 10. No dependencies beyond the .NET base class library, and none on Umbraco;
Umbraco integration is a later change.

Built primarily for the **site owner** who buys licenses, with the vendor, implementor,
backoffice editor and site visitor also considered. See [`docs/personas.md`](docs/personas.md).

## Status

The `license-key-management` change is implemented: key generation, evaluation and
signing-key management, with tests encoding every worked example in
[`docs/license-examples.md`](docs/license-examples.md). Not yet published to NuGet.

## Usage

Two sides. The **issuer** (the vendor's shop or tooling) holds the private key and signs keys.
The **consumer** (the vendor's product, running on a site) holds only public keys and
evaluates the keys a site owner supplies.

### Create a signing key pair (issuer, once per product)

```csharp
using var pair = SigningKeyPair.Create();

string privatePem = pair.PrivateKey.ExportPkcs8Pem(); // keep in custody, e.g. a vault secret
string publicPem  = pair.PublicKey.ExportPem();       // ship inside the product
string id         = pair.SigningKeyId;                // e.g. "0F8NSYaNR_Q", derived from the public key
```

ECDSA P-256 (ADR-0001). The signing key ID is derived from the public key, so an exported public
key carries it; nothing else needs to travel with it. Never ship the private key inside a
product.

### Issue a key

```csharp
using var signingKey = SigningPrivateKey.FromPem(privatePem);
var issuer = new LicenseIssuer(TimeProvider.System);

IssuedLicenseKey issued = issuer.Issue(new LicenseRequest
{
    ProductId = "acme.commerce",            // vendor.product, lowercase
    Role      = LicenseRole.Base,           // Base or AddOn; required
    Expires   = new DateOnly(2027, 3, 1),   // optional; valid to the end of that day, UTC
    VendorTag = "SHOP-2026-000123",         // optional; your order number. Never personal data
    Features  =
    [
        LicenseFeature.Switch("ecommerce"),
        LicenseFeature.Number("max-orders", 500),
        LicenseFeature.Text("licensed-domain", "example.com"),
    ],
}, signingKey);

issued.KeyString;     // LIC-8F3AK-M7RXB-7Q2D.eyJ...  one line; send to the site owner
issued.Reference;     // LIC-8F3AK-M7RXB                record it to reissue
issued.KeyIdentifier; // LIC-8F3AK-M7RXB-7Q2D           names this key in results
issued.Issued;        // set by the library, UTC, to the second
```

A request that breaks any rule throws `LicenseRequestException`, whose `Problems` lists every
broken rule, and produces no key. Rules: [`docs/license-examples.md`](docs/license-examples.md)
schema and example 19. The issuer keeps no record of what it issued and no key material.

The vendor tag is a label for your own records (PDR-0022). It must never hold a customer's
name, email address, company or other personal data: an issued key cannot be withdrawn.

### Reissue under a reference

Renewals, upgrades and corrections are reissues: pass the license reference of the earlier key.

```csharp
issuer.Issue(new LicenseRequest
{
    ProductId = "acme.commerce",
    Role      = LicenseRole.Base,
    Reference = "LIC-8F3AK-M7RXB",          // case, hyphens and spaces ignored
    Expires   = new DateOnly(2028, 3, 1),
    Features  = [LicenseFeature.Switch("ecommerce"), LicenseFeature.Number("max-orders", 2000)],
}, signingKey);
```

On a site, the key issued latest under a reference supersedes earlier ones; keys with different
references combine (PDR-0009). The vendor tag is not carried forward: pass it again, or a new one.

### Evaluate keys for a product (consumer)

```csharp
var trusted   = new TrustedSigningKeys([SigningPublicKey.FromPem(publicPem)]);
var evaluator = new LicenseEvaluator(trusted, TimeProvider.System);

LicenseEvaluation result = evaluator.Evaluate("acme.commerce", keyStrings);

if (result.Product.IsLicensed && result.Product.IsGranted("ecommerce")) { /* ... */ }
decimal? maxOrders = result.Product.GetFeature("max-orders")?.Number;  // summed across keys, exact
```

`Evaluate` never throws because of a key's content. It returns one row per supplied string, in
order, and the combined entitlement for the product. Whitespace anywhere in a key string is
ignored, so a key wrapped by an email client still reads. Feature lookup ignores the case of
`A`-`Z` and gives the same answer under every server culture. A product that is not licensed
grants no feature.

The product result: `IsLicensed` (at least one valid base), `ValidBaseCount` (more than one
usually means a duplicate purchase), `Features`, and `ConflictingFeatures` (text features whose
counted keys disagree; they answer nothing until the vendor reissues).

### Row states and first actions

Each row has one state; the first check that applies decides (PDR-0019).

| State | Meaning | First action |
|---|---|---|
| `Unreadable` | Not a key, cut off, or a verified key whose contents break a rule | Paste the key again. If it verifies but breaks a rule, the vendor must reissue |
| `WrongProduct` | Claims another product | Move the key to the product it names |
| `SigningKeyNotRecognised` | Signed with a key this product does not trust | Update the product; if that fails, ask the vendor |
| `NotVerified` | Signature does not verify (paste error or edit) | Paste the key again; if that fails, ask the vendor |
| `Duplicate` | Exact copy of an earlier row (`DuplicateOf`) | Nothing; remove either copy |
| `Superseded` | A later key under the same reference counts instead (`SupersededBy`) | Nothing; the old key can be removed |
| `Expired` | Past the end of its expiry date, UTC | Renew |
| `Inactive` | Add-on with no valid base | Renew or add the base license |
| `Valid` | Counts | |

A row is named by `KeyIdentifier`, or by `Position` when the string has no readable identifier.
The first four states are failures: the row reports only the claimed product
(`ClaimedProductId`) and identifier, never the role, dates, features or vendor tag a failed key
claims. From `Duplicate` on, the key is verified and `License` holds its contents as facts,
including its vendor tag.

Flags do not change a state or stop a key counting:

- `TiedWith`, `HasVendorError`: different keys under one reference share the latest issue time.
  All count; the vendor should reissue once (PDR-0009).
- `SupersededRoleChange`: a superseded key carried a different role from the key that
  superseded it, which explains an add-on left `Inactive`.
- `ConflictingFeatures`: this counted key carries a feature that conflicts at product level
  (PDR-0018).

No row or product result contains any part of a key string other than its key identifier.

### Rotate signing keys

1. Create a new key pair; ship its public key in a product release alongside the old one:
   `trusted.Add(newPublicKey)`. Keys signed with the old key keep verifying.
2. Switch issuing to the new private key.
3. Once no valid license depends on the old key, withdraw it: `trusted.Remove(oldSigningKeyId)`.
   Keys still signed with it are reported as `SigningKeyNotRecognised`; reissue them with the new
   key.

`TrustedSigningKeys` rejects a second, different public key under a signing key ID it already
holds.

## Limitations

- **No revocation before expiry.** A key is valid offline until it expires. Issue shorter
  expiries with renewal if you need to stop a license.
- **No machine or domain binding.** A valid key works on any install. A text feature such as
  `licensed-domain` is reported to the product, which decides what to do with it.
- **The site's clock is trusted.** Expiry is checked against the clock the product passes in;
  whoever runs the server controls it. Not mitigated.
- **Private key compromise.** Withdrawing the signing key ID stops trusting it, and also
  invalidates every legitimate key signed with it until reissued.

## Not yet specified

Work that needs a change of its own, not yet explored or proposed:

- **Umbraco integration.** Everything in [`docs/deferred-scope.md`](docs/deferred-scope.md):
  key sourcing, the shared store, package registration, the inventory, the backoffice screen
  and the Umbraco version range.
- **Issuing add-on.** Product and issued-key records for vendors without a shop system
  (PDR-0005 to PDR-0007).
- **Sample app for testing the library.** A host application for exercising the library by
  hand across scenarios (valid, expired, tampered, wrong product, missing, duplicate keys) and
  across multiple products with different features. Scope, shape and relationship to the
  automated tests are undecided.

## How this repository works

This project is specification-driven. **OpenSpec artifacts are the source of truth**, not the
code - work is specified, discussed and agreed before it is implemented.

```
  src/Umbraco.Community.Licensing/  the library
  tests/                            xUnit tests, including every worked example
  openspec/
    config.yaml                     project-wide OpenSpec configuration
    changes/<change-name>/
      proposal.md                   why, what changes, scope boundaries
      design.md                     decisions, alternatives, risks, open questions
      tasks.md                      verifiable implementation steps
      specs/<capability>/spec.md    requirements and scenarios (behaviour, not design)
  docs/personas.md                  who the library serves; primary customer
  docs/license-examples.md          worked examples of license contents and evaluation
  docs/deferred-scope.md            Umbraco and host-side scope removed from the current change
  docs/adrs/                        architecture decision records (technology)
  docs/decisions/                   product decision records (behaviour, and why)
```

Build and test: `dotnet test`. Package: `dotnet pack src/Umbraco.Community.Licensing -c Release`.

A change moves through phases, each driven by a slash command:

| Phase | Command | What happens |
|---|---|---|
| Explore | `/opsx:explore` | Think through the problem. No code, no implementation. |
| Propose | `/opsx:propose` | Create the change and generate proposal, design, specs and tasks. |
| Apply | `/opsx:apply` | Implement the tasks. |
| Archive | `/opsx:archive` | Finalise the change once it has shipped. |

Conventions worth knowing before contributing:

- **Specs describe behaviour, not implementation.** A requirement states what the system SHALL
  do, with scenarios; how it is built belongs in `design.md` or an ADR.
- **Decisions are recorded, not just made.** Anything architecturally significant gets an ADR in
  `docs/adrs/`; product decisions get a PDR in `docs/decisions/` stating why and what was
  rejected. Both are cross-referenced from the design that made the call.
- **Requirements come before technology.** Exploration deliberately stays off the subject of
  frameworks and libraries so the problem is understood on its own terms first.

## Current scope

One active change, `license-key-management`, covering three capabilities:

| Capability | What it does |
|---|---|
| `license-generation` | Issuer-side: check a key's contents and sign them. Used by vendors, never shipped inside a licensed product. |
| `license-validation` | Consumer-side: evaluate a set of keys for one product: verify each, decide its state, combine the entitlement. |
| `signing-key-management` | Create signing key pairs, hold the trusted public keys, rotate. |

Explicitly out of scope for this change: machine or domain binding, revocation before expiry,
and any online or network-based validation.

## Product decisions

Why the library behaves as it does: [`docs/decisions/`](docs/decisions/README.md) holds one record
per product decision (PDR-0001 to PDR-0022), with the reasons and the options rejected.

## Architecture decisions

- [ADR-0001](docs/adrs/0001-license-token-signing-algorithm.md) - license key signing algorithm and key string format
- [ADR-0002](docs/adrs/0002-package-split-for-keyvault-dependency.md) - splitting Azure Key Vault sourcing into a separate package (deferred)

## License

MIT. See [`LICENSE`](LICENSE).

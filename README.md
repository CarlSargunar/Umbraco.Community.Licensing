# Umbraco Licensing

A shared licensing library for paid Umbraco marketplace packages.

Umbraco package vendors have no common way to license their work: each vendor either rolls an
ad-hoc key scheme or ships unprotected. This library gives every package the same scheme: a
signed, product-scoped license key that is **verified entirely offline** (no phone-home, no
licensing server), with optional expiry, product features, add-ons and signing key rotation.

Targets .NET 10. Depends only on the .NET base class library; the core does not reference
Umbraco. Built primarily for the **site owner** who buys licenses
([`docs/personas.md`](docs/personas.md)).

## How it works

### The people involved

```
  VENDOR --issues key--> SITE OWNER --hands key--> IMPLEMENTOR --installs--> SITE
  (builds product)       (buys, pays)              (builds site, configures)    |
                                                                                v
                                              BACKOFFICE EDITOR, SITE VISITOR
                                              (use the product; never manage keys)
```

When their interests conflict, the site owner wins, then the implementor, then the vendor.
Editors may see warnings but never a full key. Visitors never see licensing, and a licensing
problem never breaks a page: the library reports, it does not throw.

### Two halves: issuer and consumer

```
  VENDOR'S SHOP / TOOLING                          VENDOR'S PRODUCT, ON A SITE
  ----------------------                           ---------------------------
  private signing key (kept in custody)            public signing key(s), shipped in the product
           |                                                     ^
           v                                                     |
  LicenseIssuer.Issue(request) --> key string --> site owner --> implementor --> site config
                                                                                    |
                                                                                    v
                                                    LicenseEvaluator.Evaluate(productId, keys)
                                                                                    |
                                                                                    v
                                                    per-key states + combined entitlement
                                                    -> product gates its features
```

- The **issuer** holds the private key and signs. It checks the request and keeps no records
  (PDR-0005); the vendor's shop or CRM already holds orders and customers.
- The **consumer** holds only public keys. It verifies signatures, checks product and expiry,
  combines all keys for the product and answers "is it licensed, and with what?". The private
  key never ships inside a product.

### Lifecycle

1. **Create a signing key pair** per product. The public key ships in the product; the private
   key stays with the vendor.
2. **Issue** a key when a site owner buys: product, role (base or add-on), optional expiry,
   optional features, optional vendor tag (e.g. order number). The library generates the
   license reference and sets the issue time.
3. **Deliver** the key string, typically by email. It is one line and survives being wrapped
   by an email client: whitespace is ignored on reading.
4. **Install.** The implementor puts the key where the product reads it (today: wherever the
   product chooses; later: a shared site store, see [Where it is heading](#where-it-is-heading)).
5. **Evaluate.** At runtime the product passes all key strings it has to the evaluator and
   gates features on the result.
6. **Renew, upgrade or correct** by reissuing under the same license reference. The latest key
   under a reference supersedes earlier ones, so old keys can stay installed harmlessly.
7. **Rotate** the signing key when needed: ship the new public key alongside the old, switch
   issuing, then withdraw the old one.

### What a key holds

```
  LIC-8F3AK-M7RXB-7Q2D.eyJ...........................
  \_____________/ \__/ \____________________________/
     reference    key   signed contents (opaque)
                  part
  \__________________/
     key identifier: safe to show, names the key in every result
```

| Field | Set by | Notes |
|---|---|---|
| Product ID | vendor | `vendor.product`, lowercase. An add-on carries the base product's ID |
| Role | vendor | `base` or `add-on`, required, no default (PDR-0011) |
| License reference | library | `LIC-XXXXX-XXXXX`, generated at first issue, kept on reissue (PDR-0009) |
| Key part | library | 4 characters, new on every issue; with the reference forms the key identifier (PDR-0020) |
| Issued | library | UTC, to the second (PDR-0016) |
| Expires | vendor | Optional date; valid to the end of that day, UTC. Omitted means perpetual |
| Features | vendor | Optional name/value pairs: switches, numbers, text (PDR-0010, PDR-0018) |
| Vendor tag | vendor | Optional label for the vendor's own records. **Never personal data**: a key cannot be withdrawn (PDR-0022) |
| Signing key ID | library | Which public key verifies it; enables rotation (ADR-0001) |

Signed with ECDSA P-256 (ADR-0001). Full schema and rules:
[`docs/license-examples.md`](docs/license-examples.md).

### How a site's keys combine

A site may hold several keys for one product: a base license, add-ons, capacity packs,
renewals, accidental duplicates. Each key is judged on its own, then the counting keys combine.

```
  per key                                       per product
  unreadable / wrong product /
  signing key not recognised /
  not verified / expired        -> drops out    licensed = at least one valid base
  exact copy of an earlier key  -> ignored      features = valid bases + active add-ons
  older key, same reference     -> ignored        switches: granted if any key grants
  add-on with no valid base     -> inactive       numbers:  summed
  otherwise                     -> counts          text:     equal values agree; different
                                                             values conflict and answer nothing
```

- An add-on counts only while a valid base for the same product is present. When the base
  lapses the add-on goes **inactive**, and reactivates when the base is renewed.
- Two bases with different references both count (a duplicate purchase is reported via
  `ValidBaseCount`, not hidden). An upgrade should be a reissue, which supersedes.
- A product that is not licensed grants no feature.

Each key gets exactly one state; the first check that applies decides (PDR-0019).

| State | Meaning | First action |
|---|---|---|
| `Unreadable` | Not a key, cut off, or a verified key whose contents break a rule | Paste again. If it verifies but breaks a rule, the vendor must reissue |
| `WrongProduct` | Claims another product | Move the key to the product it names |
| `SigningKeyNotRecognised` | Signed with a key this product does not trust | Update the product; if that fails, ask the vendor |
| `NotVerified` | Signature does not verify (paste error or edit) | Paste again; if that fails, ask the vendor |
| `Duplicate` | Exact copy of an earlier key | Nothing; remove either copy |
| `Superseded` | A later key under the same reference counts instead | Nothing; the old key can be removed |
| `Expired` | Past the end of its expiry date, UTC | Renew |
| `Inactive` | Add-on with no valid base | Renew or add the base license |
| `Valid` | Counts | |

The first four are failures: the result reports only the claimed product and key identifier,
never the dates, features or tag a failed key claims, because they are unverified. No result
ever contains a key string beyond its identifier; keys are bearer tokens.

### Where it is heading

The core stops at "key strings in, results out". Where keys come from and how results are shown
is host-side work, decided at product level but deferred to a later change
([`docs/deferred-scope.md`](docs/deferred-scope.md)):

```
  .NET config / env vars / Azure Key Vault
                 |
                 v
        ONE SHARED LICENSE STORE PER SITE  (all vendors' keys; PDR-0002)
                 |
                 v
  each installed package registers:        product ID, display name, vendor name,
  (PDR-0003, PDR-0004)                     renewal link, required | optional
                 |
                 v
        core evaluates per product
                 |
                 v
        INVENTORY: one row per registered product, including "no license found",
                   expiring soon, inactive add-ons, keys matching no product
                 |
                 v
        BACKOFFICE SCREEN (one site-level package, never shows a full key)
```

Also deferred: an Umbraco version range claim on base licenses (PDR-0012), dependencies between
products, and an optional issuing add-on that keeps product and issued-key records for vendors
without a shop system (PDR-0005 to PDR-0007).

### Deliberate limits

- **No revocation before expiry.** A key is valid offline until it expires. Use shorter expiries
  with renewal if a license must be stoppable.
- **No machine or domain binding.** A valid key works on any install. A text feature such as
  `licensed-domain` is reported to the product, which decides what to do with it.
- **The site's clock is trusted.** Whoever runs the server controls it. Not mitigated.
- **Private key compromise.** Withdrawing the signing key stops trusting it, and also
  invalidates every legitimate key it signed until they are reissued.

## Usage

### Issuer: create a signing key pair (once per product)

```csharp
using var pair = SigningKeyPair.Create();

string privatePem = pair.PrivateKey.ExportPkcs8Pem(); // keep in custody, e.g. a vault secret
string publicPem  = pair.PublicKey.ExportPem();       // ship inside the product
string id         = pair.SigningKeyId;                // derived from the public key
```

### Issuer: issue a key

```csharp
using var signingKey = SigningPrivateKey.FromPem(privatePem);
var issuer = new LicenseIssuer(TimeProvider.System);

IssuedLicenseKey issued = issuer.Issue(new LicenseRequest
{
    ProductId = "acme.commerce",
    Role      = LicenseRole.Base,
    Expires   = new DateOnly(2027, 3, 1),
    VendorTag = "SHOP-2026-000123",
    Features  =
    [
        LicenseFeature.Switch("ecommerce"),
        LicenseFeature.Number("max-orders", 500),
        LicenseFeature.Text("licensed-domain", "example.com"),
    ],
}, signingKey);

issued.KeyString;     // LIC-8F3AK-M7RXB-7Q2D.eyJ...  send to the site owner
issued.Reference;     // LIC-8F3AK-M7RXB                record it to reissue
issued.KeyIdentifier; // LIC-8F3AK-M7RXB-7Q2D
```

An invalid request throws `LicenseRequestException`, whose `Problems` lists every broken rule.
To reissue (renew, upgrade, correct), set `Reference` to the earlier key's reference. The vendor
tag is not carried forward.

### Consumer: evaluate keys

```csharp
var trusted   = new TrustedSigningKeys([SigningPublicKey.FromPem(publicPem)]);
var evaluator = new LicenseEvaluator(trusted, TimeProvider.System);

LicenseEvaluation result = evaluator.Evaluate("acme.commerce", keyStrings);

if (result.Product.IsLicensed && result.Product.IsGranted("ecommerce")) { /* ... */ }
decimal? maxOrders = result.Product.GetFeature("max-orders")?.Number;  // summed across keys
```

`Evaluate` never throws because of a key's content. It returns one row per supplied string, in
order (`State`, `KeyIdentifier` or `Position`, `License` for verified keys, plus flags such as
`SupersededBy`, `DuplicateOf`, `TiedWith`), and the product result (`IsLicensed`,
`ValidBaseCount`, `Features`, `ConflictingFeatures`). Feature lookup ignores case and server
culture.

### Rotate signing keys

1. Create a new pair; ship its public key alongside the old one: `trusted.Add(newPublicKey)`.
2. Switch issuing to the new private key.
3. When no valid license depends on the old key: `trusted.Remove(oldSigningKeyId)`. Keys still
   signed with it report `SigningKeyNotRecognised` and need reissuing.

More worked scenarios, each encoded as a test:
[`docs/license-examples.md`](docs/license-examples.md).

## Status

| Part | State |
|---|---|
| Key generation, evaluation, signing-key management (`license-key-management` change) | Implemented and tested. Not yet on NuGet |
| Umbraco integration: sourcing, shared store, registration, inventory, backoffice screen | Decided at product level, deferred |
| Issuing add-on | Decided at product level, not yet specified |
| Sample app for exercising the library by hand | Not yet specified |

Build and test: `dotnet test`. Package: `dotnet pack src/Umbraco.Community.Licensing -c Release`.

## Repository

Specification-driven: **OpenSpec artifacts are the source of truth**. Work is explored,
specified and agreed before it is implemented.

```
  src/Umbraco.Community.Licensing/  the library
  tests/                            xUnit tests, including every worked example
  openspec/changes/<change>/        proposal, design, specs (behaviour), tasks
  docs/personas.md                  who the library serves, and priority order
  docs/license-examples.md          key schema and worked evaluation examples
  docs/deferred-scope.md            host-side scope parked for later changes
  docs/decisions/                   PDRs: product decisions, why, what was rejected
  docs/adrs/                        ADRs: technology decisions
```

| Phase | Command | What happens |
|---|---|---|
| Explore | `/opsx:explore` | Requirements only; no technology |
| Propose | `/opsx:propose` | Proposal, design, specs and tasks; technology choices and ADRs |
| Apply | `/opsx:apply` | Implement the tasks |
| Archive | `/opsx:archive` | Finalise the change once shipped |

Conventions:

- Specs describe behaviour; how it is built belongs in `design.md` or an ADR.
- A product decision gets a PDR ([`docs/decisions/`](docs/decisions/README.md)); a technology
  decision gets an ADR ([`docs/adrs/`](docs/adrs/)). Test: if the stack changed, a PDR would
  still hold.
- Cite records with prefix and number: PDR-0011, ADR-0001.

## License

MIT. See [`LICENSE`](LICENSE).

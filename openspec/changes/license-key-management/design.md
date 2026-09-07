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

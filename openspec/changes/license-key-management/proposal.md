## Why

Umbraco marketplace vendors currently have no shared, trustworthy way to license paid packages: each vendor either rolls its own ad-hoc key scheme or ships unprotected. A common licensing library lets any Umbraco package enforce a product-specific, time-bound, version-scoped license using a key that can be validated entirely offline (no phone-home dependency), while giving hosts a familiar, idiomatic way to supply the key via configuration, environment variables, or Azure Key Vault.

## What Changes

- Introduce a license key format based on a signed, offline-verifiable token (asymmetric signature; algorithm to be finalized in design.md) encoding:
  - **Mandatory**: product ID.
  - **Optional**: expiry date (absence means perpetual).
  - **Optional**: supported Umbraco core version range (absence means unrestricted).
  - A key ID identifying which trusted public key was used to sign, to support future key rotation without invalidating already-issued licenses.
- Provide a key **generation** API (issuer-side; used by vendors/marketplace tooling to mint keys, not intended for redistribution inside the licensed product itself) that takes the above claims and a private signing key, and produces a compact, transmissible license key string.
- Provide a key **validation** API (consumer-side; used inside the licensed Umbraco package at runtime) that:
  - Verifies the signature against one or more trusted public keys (selected via the embedded key ID).
  - Checks the product ID matches the expected product.
  - Checks the expiry, if present, against current time.
  - Checks the running Umbraco core version, if a version range is present, falls within that range.
- Provide pluggable license key **sourcing** so a host application can supply the raw key string from:
  - `appsettings.json` / .NET configuration.
  - Environment variables.
  - Azure Key Vault.
  - A common abstraction so additional sources can be added later without changing the validation API.
- Target .NET 10 and Umbraco 17+.

Out of scope for this change (explicitly deferred):
- Hardware/machine/domain binding of a license to a specific install.
- License revocation before expiry (inherent limitation of pure offline signed tokens).
- Online/network-based validation or a licensing server.

## Capabilities

### New Capabilities
- `license-generation`: Issuer-side API to construct and sign a license key from product ID, optional expiry, and optional supported-version range.
- `license-validation`: Consumer-side API to verify a license key's signature and evaluate its claims (product ID match, expiry, version range) against the running product and Umbraco version.
- `license-key-sourcing`: Pluggable provider abstraction for supplying the raw license key string from .NET configuration, environment variables, or Azure Key Vault.

### Modified Capabilities
- None — this is a new library with no pre-existing specs.

## Impact

- New library project(s) targeting .NET 10, consumable by any Umbraco 17+ package.
- New dependency on a cryptographic signing/verification implementation (algorithm decision recorded as an ADR in `docs/adrs/`, per project convention).
- New dependency on Azure Key Vault client libraries for the Key Vault sourcing provider (should be an optional/pluggable dependency, not a hard requirement for consumers who don't use Key Vault).
- Vendors adopting this library will need a private-key custody process for issuing licenses (outside this library's runtime scope, but the generation API's design must support it safely — private key never bundled with the validation/runtime package).

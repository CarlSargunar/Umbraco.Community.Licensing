## Why

Package vendors have no shared, trustworthy way to license paid products: each vendor either rolls its own ad-hoc key scheme or ships unprotected. A common licensing library lets any product enforce a product-specific, time-bound license using a key that can be validated entirely offline (no phone-home dependency).

The primary customer is the **site owner** who buys licenses; the vendor and implementor are also considered. See `docs/personas.md`.

**Scope, reduced on 2026-10-01.** This change is a library for license key **generation**, **verification** and **signing-key management** only. Everything that depends on Umbraco or on running inside a host site (key sourcing, a shared store, package registration, the inventory, a backoffice screen, the Umbraco version range) was taken out and is recorded in [`docs/deferred-scope.md`](../../../docs/deferred-scope.md) for later changes.

## What Changes

- Introduce a license key format based on a signed, offline-verifiable token (asymmetric signature; algorithm in `design.md` and ADR-0001) encoding the schema in `docs/license-examples.md`:
  - **Mandatory**: product ID, role (base or add-on), license reference, issue time (set by the library).
  - **Optional**: expiry date (absence means perpetual), product features.
  - A key ID identifying which trusted public key was used to sign, to support key rotation without invalidating already-issued licenses.
- Provide a key **generation** API (issuer-side; used by the vendor or its store tooling to mint keys, not intended for redistribution inside the licensed product itself) that takes the above claims and a private signing key, and produces a compact, transmissible license key string.
- Provide a key **verification** API (consumer-side; used inside the vendor's licensed product at runtime) that:
  - Verifies the signature against one or more trusted public keys (selected via the embedded key ID).
  - Checks the product ID matches the expected product.
  - Checks the expiry, if present, against current time.
  - Evaluates a set of keys for one product: superseding by reference, base and add-on combining, feature combining, and the resulting entitlement.
  - Returns results and never throws on a bad key.
- Provide **signing-key management**: create a signing key pair with a key ID, hold a set of trusted public keys, and rotate keys.
- Target .NET 10.

Out of scope for this change (explicitly deferred):
- Anything listed in `docs/deferred-scope.md`: key sourcing, shared store, package registration, inventory, backoffice screen, Umbraco version range, site label, routing, product dependencies.
- Hardware/machine/domain binding of a license to a specific install.
- License revocation before expiry (inherent limitation of pure offline signed tokens).
- Online/network-based validation or a licensing server.
- The optional issuing add-on that keeps product and issued-key records (PDR-0005 to PDR-0007); a separate change.

## Capabilities

### New Capabilities
- `license-generation`: Issuer-side API to construct and sign a license key from its claims.
- `license-validation`: Consumer-side API to verify a license key's signature, evaluate its claims against the product and current time, and evaluate a set of keys for one product.
- `signing-key-management`: Create signing key pairs with key IDs, hold trusted public keys, and rotate.

### Modified Capabilities
- None - this is a new library with no pre-existing specs.

## Impact

- One new library project targeting .NET 10, consumable by any .NET product. BCL-only dependencies.
- New dependency on a cryptographic signing/verification implementation (algorithm decision recorded as an ADR in `docs/adrs/`, per project convention).
- Vendors adopting this library will need a private-key custody process for issuing licenses (outside this library's runtime scope, but the generation API's design must support it safely - private key never bundled with the validation/runtime package).

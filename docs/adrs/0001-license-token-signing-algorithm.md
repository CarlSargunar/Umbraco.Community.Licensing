# 0001: License Token Signing Algorithm and Format

## Status

Draft. Not accepted until the open questions in `openspec/changes/license-key-management/design.md` are settled.

## Context

The `license-key-management` change (see `openspec/changes/license-key-management/`) requires a license key that can be validated fully offline, is tamper-evident, and supports future key rotation. This requires choosing a signing algorithm and a token wire format.

Personas affected (see `docs/personas.md`):

- **Vendor**: signs keys with a private key per product, and ships the matching public key inside the product.
- **Site owner**: receives the key, typically by email.
- **Implementor**: pastes the key into configuration, an environment variable or a vault secret, and diagnoses it when it fails.

## Decision

Sign license tokens with **ECDSA using the P-256 curve** (`System.Security.Cryptography.ECDsa`), and use a **custom fixed-algorithm compact token format** — `base64url(JSON payload) + "." + base64url(signature)` — rather than a generic JWT.

The JSON payload carries `keyId` plus the key schema in `docs/license-examples.md` (product ID, role, license reference, issued time, optional expiry, features). The Umbraco major range was removed from the schema on 2026-10-01 (`docs/deferred-scope.md` D1). The JSON property names and encodings are to be fixed when this ADR is accepted. The verifier always verifies with ECDSA P-256; the format has no algorithm-negotiation field for an attacker to manipulate.

## Alternatives Considered

- **RSA (2048/4096-bit):** Built into the BCL like ECDSA, but produces significantly larger keys and signatures (RSA-2048 signatures ~256 bytes vs. ECDSA P-256 ~64-72 bytes) for no additional security benefit at this threat level, resulting in longer license key strings. Not chosen.
- **Ed25519:** Fast and safe by design, but cross-platform built-in .NET support has historically been inconsistent, typically requiring a third-party dependency (BouncyCastle or a libsodium binding) for reliable coverage across the OS versions this library targets. Not chosen, to keep the library dependency-free.
- **Generic JWT via a JWT library:** Rejected because JWT's `alg` header invites downgrade/confusion attacks (e.g., `alg: none`) and would add a dependency for interoperability this library does not need — nothing outside this library ever needs to read the token.

## Consequences

- Verification requires no third-party cryptography package; `System.Security.Cryptography` is sufficient on all .NET 10 target platforms.
- License key strings stay short and single-line, which matters because the vendor emails them to the site owner and the implementor pastes them into config, environment variables or vault secrets.
- Because the format is custom (not a standard like JWT), no external tooling can inspect/decode a license key without this library or a small compatible decoder; this is an accepted trade-off since there's no interoperability requirement. The implementor therefore depends on the library's own reporting (validation reasons, evaluation result) to diagnose a failing key.
- Key rotation is supported by embedding a `keyId` in the payload from the start (see the `license-generation` and `license-validation` specs), so this decision does not need to be revisited to add rotation later.

## Reversal Cost

Changing the signing algorithm or token format after any licenses have been issued is a breaking change for every previously issued license, and the vendor would have to reissue keys to every site owner — the validator would need to support verifying both the old and new formats simultaneously during a transition, or all outstanding licenses would need to be reissued. Get this right before the first real release.

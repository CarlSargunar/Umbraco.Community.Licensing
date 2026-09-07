# 0001: License Token Signing Algorithm and Format

## Status

Accepted

## Context

The `license-key-management` change (see `openspec/changes/license-key-management/`) requires a license key that can be validated fully offline, is tamper-evident, and supports future key rotation. This requires choosing a signing algorithm and a token wire format.

## Decision

Sign license tokens with **ECDSA using the P-256 curve** (`System.Security.Cryptography.ECDsa`), and use a **custom fixed-algorithm compact token format** — `base64url(JSON payload) + "." + base64url(signature)` — rather than a generic JWT.

The JSON payload carries: `keyId`, `productId`, `expiryUtc` (optional), `minVersion`/`maxVersion` (optional), `issuedAtUtc`. The verifier always verifies with ECDSA P-256; the format has no algorithm-negotiation field for an attacker to manipulate.

## Alternatives Considered

- **RSA (2048/4096-bit):** Built into the BCL like ECDSA, but produces significantly larger keys and signatures (RSA-2048 signatures ~256 bytes vs. ECDSA P-256 ~64-72 bytes) for no additional security benefit at this threat level, resulting in longer license key strings. Not chosen.
- **Ed25519:** Fast and safe by design, but cross-platform built-in .NET support has historically been inconsistent, typically requiring a third-party dependency (BouncyCastle or a libsodium binding) for reliable coverage across the OS versions this library targets. Not chosen, to keep the library dependency-free.
- **Generic JWT via a JWT library:** Rejected because JWT's `alg` header invites downgrade/confusion attacks (e.g., `alg: none`) and would add a dependency for interoperability this library does not need — nothing outside this library ever needs to read the token.

## Consequences

- Verification requires no third-party cryptography package; `System.Security.Cryptography` is sufficient on all .NET 10 target platforms.
- License key strings stay short, which matters because they're often hand-delivered (email, pasted into config).
- Because the format is custom (not a standard like JWT), no external tooling can inspect/decode a license key without this library or a small compatible decoder; this is an accepted trade-off since there's no interoperability requirement.
- Key rotation is supported by embedding a `keyId` in the payload from the start (see the `license-generation` and `license-validation` specs), so this decision does not need to be revisited to add rotation later.

## Reversal Cost

Changing the signing algorithm or token format after any licenses have been issued is a breaking change for every previously issued license — the validator would need to support verifying both the old and new formats simultaneously during a transition, or all outstanding licenses would need to be reissued. Get this right before the first real release.

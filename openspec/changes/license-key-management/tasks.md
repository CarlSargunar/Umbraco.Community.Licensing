Scope note (2026-10-01): host-side tasks (Azure Key Vault and configuration sourcing, the package split, the version range) were removed with the scope cut recorded in `docs/deferred-scope.md`. Tasks for roles, references, issue time, features and combining will be added when the specs are revised against the PDRs (design.md, "Where to resume").

## 1. Solution and project setup

- [ ] 1.1 Create the .NET 10 solution with one class library project (generation, validation, signing-key management) and verify `dotnet build` succeeds targeting `net10.0` with no package references beyond the BCL
- [ ] 1.2 Create a test project and verify `dotnet test` runs (even with zero tests) against the new solution

## 2. License token model and cryptography (ADR-0001)

- [ ] 2.1 Implement the license claims model (product ID, optional expiry, key ID, issued-at) and verify it round-trips through `System.Text.Json` serialization in a unit test
- [ ] 2.2 Implement ECDSA P-256 signing of the serialized payload, producing the `base64url(payload).base64url(signature)` token format, and verify a unit test signs a payload and produces a two-part, base64url-safe, single-line string
- [ ] 2.3 Implement signature verification given a token and a trusted public key, and verify unit tests cover: valid signature accepted; signature rejected when payload bytes are altered; signature rejected when verified against the wrong public key

## 3. Signing-key management (`signing-key-management` spec)

- [ ] 3.1 Implement signing key pair creation returning a private key, a public key and a key ID, each exportable on its own; verify a unit test that the exported public key carries the key ID and that the private key is not part of the public export
- [ ] 3.2 Implement the trusted key set addressed by key ID, including rejection of a duplicate key ID with a different public key; verify unit tests for add, resolve, remove and the duplicate rejection

## 4. License generation (`license-generation` spec)

- [ ] 4.1 Implement the generation API accepting product ID (mandatory), expiry (optional), and a private signing key + key ID, producing a signed license key string; verify unit tests for the "minimal license" and "license with expiry" scenarios
- [ ] 4.2 Implement input validation that rejects a missing product ID and a malformed expiry; verify unit tests for both rejection scenarios raise an error and produce no key
- [ ] 4.3 Verify by code review and a unit test that no private key material is stored, cached, or exposed by the API after a generation call returns

## 5. License validation (`license-validation` spec)

- [ ] 5.1 Implement resolution of the trusted public key from the token's embedded key ID against the trusted key set, and verify unit tests for: matching key ID succeeds; unknown key ID is rejected without evaluating claims
- [ ] 5.2 Wire signature verification into the validation pipeline and verify a unit test that a tampered payload is rejected as invalid
- [ ] 5.3 Implement the product ID match check and verify a unit test for the product-mismatch scenario
- [ ] 5.4 Implement the expiry check using an injected `TimeProvider` and verify unit tests for: expired key rejected; unexpired key and perpetual (no-expiry) key both pass this check, using a fixed simulated time rather than the real clock
- [ ] 5.5 Implement a validation result type distinguishing valid / malformed / invalid signature / untrusted key ID / product mismatch / expired, and verify a unit test asserts the specific reason returned for each failure scenario above
- [ ] 5.6 Verify unit tests that validating an empty string, a truncated key and a string with no separator each return a malformed result and never throw

## 6. Cross-cutting verification

- [ ] 6.1 Write an end-to-end test that creates a key pair (Section 3), generates a license key (Section 4), then validates it (Section 5), covering: happy path valid license; expired license; product mismatch; tampered token; and confirm each produces the expected distinct validation result
- [ ] 6.2 Write a key-rotation test that generates a license under one key ID, configures the validator with two trusted key IDs (old and new), and verifies the license still validates; then removes the old key ID from the trusted set and verifies validation now fails with untrusted key ID
- [ ] 6.3 Run `dotnet test` across the full solution and verify all tests pass

## 7. Packaging and documentation

- [ ] 7.1 Add NuGet package metadata (id, version, description, license) to the project and verify `dotnet pack` produces the expected package file
- [ ] 7.2 Write a top-level README covering: how to create a signing key pair, how to generate a license key, how to validate one, how to rotate keys, and the documented limitations (no revocation before expiry, no machine binding, clock-trust caveat) from `design.md`

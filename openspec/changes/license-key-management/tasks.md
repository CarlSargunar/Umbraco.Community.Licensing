## 1. Solution and project setup

- [ ] 1.1 Create the .NET 10 solution with a core class library project (generation, validation, configuration/environment-variable sourcing) and verify `dotnet build` succeeds targeting `net10.0`
- [ ] 1.2 Create the separate Azure Key Vault sourcing project referencing `Azure.Security.KeyVault.Secrets` and `Azure.Identity`, and verify the core project has no reference to it and no transitive Azure SDK dependency (`dotnet list <core-project> package --include-transitive` shows none)
- [ ] 1.3 Create a test project per production project and verify `dotnet test` runs (even with zero tests) against the new solution

## 2. License token model and cryptography (ADR-0001)

- [ ] 2.1 Implement the license claims model (product ID, optional expiry, optional min/max supported version, key ID, issued-at) and verify it round-trips through `System.Text.Json` serialization in a unit test
- [ ] 2.2 Implement ECDSA P-256 signing of the serialized payload, producing the `base64url(payload).base64url(signature)` token format, and verify a unit test signs a payload and produces a two-part, base64url-safe string
- [ ] 2.3 Implement signature verification given a token and a trusted public key, and verify unit tests cover: valid signature accepted; signature rejected when payload bytes are altered; signature rejected when verified against the wrong public key

## 3. License generation (`license-generation` spec)

- [ ] 3.1 Implement the generation API accepting product ID (mandatory), expiry (optional), version range (optional), and a private signing key + key ID, producing a signed license key string; verify unit tests for the "minimal license", "license with expiry", and "license with version range" scenarios
- [ ] 3.2 Implement input validation that rejects a missing product ID and an invalid version range (min > max); verify unit tests for both rejection scenarios raise an error and produce no key
- [ ] 3.3 Verify by code review and a unit test that no private key material is stored, cached, or exposed by the API after a generation call returns

## 4. License validation (`license-validation` spec)

- [ ] 4.1 Implement resolution of the trusted public key from the token's embedded key ID against a caller-supplied set of trusted keys, and verify unit tests for: matching key ID succeeds; unknown key ID is rejected without evaluating claims
- [ ] 4.2 Wire signature verification into the validation pipeline and verify a unit test that a tampered payload is rejected as invalid
- [ ] 4.3 Implement the product ID match check and verify a unit test for the product-mismatch scenario
- [ ] 4.4 Implement the expiry check using an injected `TimeProvider` and verify unit tests for: expired key rejected; unexpired key and perpetual (no-expiry) key both pass this check, using a fixed simulated time rather than the real clock
- [ ] 4.5 Implement the supported-version-range check against a caller-supplied running version and verify unit tests for: version within range passes; version outside range fails; no version range encoded always passes this check
- [ ] 4.6 Implement a validation result type distinguishing valid / invalid signature / untrusted key ID / product mismatch / expired / unsupported version, and verify a unit test asserts the specific reason returned for each failure scenario above

## 5. License key sourcing - configuration and environment variable providers (`license-key-sourcing` spec)

- [ ] 5.1 Define the common license key source abstraction and verify a unit test can substitute a fake implementation without changing calling code
- [ ] 5.2 Implement the .NET configuration provider reading from `IConfiguration` and verify a unit test that a key present at the configured path is returned
- [ ] 5.3 Implement the environment variable provider and verify a unit test that a key present in a configured environment variable name is returned
- [ ] 5.4 Implement "no key found" reporting (distinct from an empty/invalid key) for both providers and verify unit tests that an unset configuration path / unset environment variable reports absence rather than an empty string

## 6. License key sourcing - Azure Key Vault provider (`license-key-sourcing` spec)

- [ ] 6.1 Implement the Key Vault provider reading a configured secret name via `Azure.Security.KeyVault.Secrets`, and verify an integration test (using a fake/mocked secret client) that a present secret value is returned as the raw license key
- [ ] 6.2 Implement "no key found" reporting for the Key Vault provider when the secret does not exist, and verify a unit test with a mocked client covering that case
- [ ] 6.3 Verify by package inspection (`dotnet list package`) that only the Key Vault project - not the core package - references the Azure SDK packages

## 7. Cross-cutting verification

- [ ] 7.1 Write an end-to-end test that generates a license key (Section 3), then validates it (Section 4), covering: happy path valid license; expired license; product mismatch; unsupported version; tampered token; and confirm each produces the expected distinct validation result
- [ ] 7.2 Write a key-rotation test that generates a license under one key ID, configures the validator with two trusted key IDs (old and new), and verifies the license still validates; then removes the old key ID from the trusted set and verifies validation now fails with untrusted key ID
- [ ] 7.3 Run `dotnet test` across the full solution and verify all tests pass

## 8. Packaging and documentation

- [ ] 8.1 Add NuGet package metadata (id, version, description, license) to each shipped project and verify `dotnet pack` produces the expected package files for the core and Key Vault projects
- [ ] 8.2 Write a top-level README covering: how to generate a license key, how to validate one, how to configure each sourcing provider, and the documented limitations (no revocation before expiry, no machine binding, clock-trust caveat) from `design.md`

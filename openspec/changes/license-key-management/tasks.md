**Prerequisite:** ADR-0001 must be revised for the visible key identifier (PDR-0020) and its payload fixed before section 2 starts (design.md, "Technical open questions"). Section 3 onwards does not depend on the wire format except through section 2.

Each worked example in `docs/license-examples.md` is a test case; task 8.1 makes that explicit.

## 1. Solution and project setup

- [ ] 1.1 Create the .NET 10 solution with one class library project (generation, validation, signing-key management) and verify `dotnet build` succeeds targeting `net10.0` with no package references beyond the BCL
- [ ] 1.2 Create an xUnit test project and verify `dotnet test` runs (even with zero tests) against the new solution

## 2. Key string format and cryptography (ADR-0001, revised)

- [ ] 2.1 Implement the key contents model (product ID, role, license reference, key part, issue time to the second, optional expiry, typed features, signing key ID) and verify it round-trips through serialization in a unit test
- [ ] 2.2 Implement ECDSA P-256 signing producing a key string that starts with the key identifier as displayed, followed by the opaque part, with the identifier covered by the signature; verify a unit test that the string is single-line, contains no whitespace and starts with `LIC-XXXXX-XXXXX-XXXX`
- [ ] 2.3 Implement signature verification against a trusted public key; verify unit tests: valid key accepted; rejected when contents are altered; rejected when the visible identifier is altered; rejected against the wrong public key
- [ ] 2.4 Implement reading a supplied string: remove all whitespace, read the identifier from the start, read the contents; verify unit tests: a key wrapped across lines with a trailing newline reads as the original; a key cut off after its identifier yields the identifier and no contents; a string with no identifier yields neither (PDR-0021, PDR-0020)
- [ ] 2.5 Implement the schema check on read contents, reusing the rules in section 3, so a verified key whose contents break a rule is unreadable; verify a unit test with a key signed outside the generation API carrying `max-orders: -200` (PDR-0021)

## 3. Identifiers and value rules

- [ ] 3.1 Implement the product ID rule (`vendor.product`, lowercase) and verify unit tests for `acme.commerce` accepted and `commerce`, `Acme.Commerce` rejected
- [ ] 3.2 Implement license reference and key part generation from a cryptographically secure random source over the 31-character alphabet, and reference parsing ignoring case, hyphens and spaces; verify unit tests for format, alphabet, `lic-8f3ak m7rxb` parsing to `LIC-8F3AK-M7RXB`, and rejection of `O`, `0`, `1`, `I`, `L` and wrong lengths
- [ ] 3.3 Implement the feature rules: name rule, at most once per key, switch / number / text types, no explicit `false`; numbers zero or positive with at most 4 decimal places and 15 digits, held exactly; text non-empty, at most 256 characters, no leading or trailing whitespace, no control characters; verify unit tests for every feature row of `docs/license-examples.md` example 19

## 4. Signing-key management (`signing-key-management` spec)

- [ ] 4.1 Implement signing key pair creation returning a private key, a public key and a signing key ID, each exportable on its own; verify unit tests that the exported public key carries the signing key ID and the private key is not part of the public export
- [ ] 4.2 Implement the trusted key set addressed by signing key ID, including rejection of a duplicate signing key ID with a different public key; verify unit tests for add, resolve, remove and the duplicate rejection

## 5. License generation (`license-generation` spec)

- [ ] 5.1 Implement the generation API taking product ID, role, optional reference, optional expiry, features, a private signing key and its signing key ID, with the issue time from an injected `TimeProvider`; return the key string, reference, key identifier and issue time; verify unit tests for the minimal-license, full-contents, first-issue and reissue scenarios, and that the issue time is truncated to the second
- [ ] 5.2 Implement request checking that collects every broken rule (role required, product ID, reference, expiry not before the current UTC date, feature rules) and raises one error naming all of them, producing no key; verify unit tests for each rule, for expiry today accepted, and for a request breaking two rules reporting both
- [ ] 5.3 Verify by code review and a unit test that no private key material is stored, cached or exposed after a generation call returns, and that the API keeps no record of issued keys

## 6. Per-key evaluation (`license-validation` spec)

- [ ] 6.1 Implement the evaluation entry point taking an ordered list of key strings, a product ID, a trusted key set and a `TimeProvider`, returning one row per string in order plus a product result; verify unit tests for an empty list and for row order
- [ ] 6.2 Implement the failure checks in order (unreadable, wrong product, signing key not recognised, not verified); verify unit tests for each, and that a wrong-product key signed with an untrusted key is reported as wrong product
- [ ] 6.3 Implement failed-row reporting: reason plus claimed product and claimed identifier only; verify a unit test that an edited key's expiry and features appear nowhere in the result
- [ ] 6.4 Implement duplicates (identical signed contents; first counts, later copies name the row they copy); verify unit tests including a copy that differs only in whitespace
- [ ] 6.5 Implement superseding among verified, non-duplicate keys: same reference, strictly earlier issue time; expiry and role play no part; role-changed note; verify unit tests for `docs/license-examples.md` examples 3 to 6
- [ ] 6.6 Implement ties for latest: all count, each flagged as vendor error naming the others, flag cleared once a later key exists; verify unit tests for `docs/license-examples.md` example 7
- [ ] 6.7 Implement the expiry check (valid to the end of the expiry date in UTC) using the injected `TimeProvider`; verify unit tests at 23:59:59 and 00:00:00 the next day, and for a key with no expiry
- [ ] 6.8 Implement base and add-on: add-on inactive with no valid base; product licensed when at least one valid base; count of valid bases reported; verify unit tests for `docs/license-examples.md` examples 11 to 13

## 7. Product result (`license-validation` spec)

- [ ] 7.1 Implement combining over valid rows: switches any, numbers summed exactly, equal text as one value, conflicting text answers nothing and flags the product and each counted row carrying the name; verify unit tests for `docs/license-examples.md` examples 9, 10, 16 and 17
- [ ] 7.2 Implement feature lookup ignoring case of `a`-`z` independent of the current culture, with unknown names and an unlicensed product answering not granted; verify unit tests including one run under the `tr-TR` culture
- [ ] 7.3 Verify unit tests that the product result is the same for every order of the same keys, and that no row or product result contains any part of a key string other than a key identifier
- [ ] 7.4 Verify unit tests that an empty string, a truncated key, a pasted password and random bytes each return an unreadable row and never throw

## 8. Cross-cutting verification

- [ ] 8.1 Encode every site evaluation in `docs/license-examples.md` (examples 1 to 18) as an end-to-end test: create a key pair, issue the keys, evaluate them at the stated time, and assert the rows and product result shown; encode example 19 against generation
- [ ] 8.2 Write a rotation test: issue under signing key ID A, trust A and B, verify the key counts; withdraw A, verify the row is signing key not recognised
- [ ] 8.3 Run `dotnet test` across the full solution and verify all tests pass

## 9. Packaging and documentation

- [ ] 9.1 Add NuGet package metadata (id, version, description, license) and verify `dotnet pack` produces the expected package file
- [ ] 9.2 Write a top-level README covering: creating a signing key pair, issuing a key, reissuing under a reference, evaluating keys for a product, reading row states and their first actions (PDR-0019), rotating signing keys, and the documented limitations (no revocation before expiry, no machine binding, clock-trust caveat) from `design.md`

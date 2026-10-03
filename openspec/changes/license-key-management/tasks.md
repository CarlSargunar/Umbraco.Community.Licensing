**Wire format:** ADR-0001 (decided 2026-10-03) fixes the key string, payload, number encoding, signing key ID derivation and reading algorithm. Section 3 onwards does not depend on the wire format except through section 2.

Each worked example in `docs/license-examples.md` is a test case; task 8.1 makes that explicit.

## 1. Solution and project setup

- [ ] 1.1 Create the .NET 10 solution with one class library project (generation, validation, signing-key management) and verify `dotnet build` succeeds targeting `net10.0` with no package references beyond the BCL
- [ ] 1.2 Create an xUnit test project and verify `dotnet test` runs (even with zero tests) against the new solution

## 2. Key string format and cryptography (ADR-0001)

- [ ] 2.1 Implement the signing key ID derivation (first 8 bytes of SHA-256 over the SubjectPublicKeyInfo DER, base64url, 11 characters), computable from a public key or a private key; verify unit tests that both give the same ID for one key pair and that two key pairs give different IDs
- [ ] 2.2 Implement the payload writer and reader: fields `signingKeyId`, `product`, `role`, `issued` (`yyyy-MM-ddTHH:mm:ssZ`), `expires` (`yyyy-MM-dd`, omitted when none), `features` (omitted when none; `true` switch, number, string text), UTF-8 JSON without whitespace; verify it round-trips every field and feature type, and that `"500"` reads back as text and `500` as a number
- [ ] 2.3 Implement the number encoding: grammar `0` or `[1-9][0-9]*`, optional `.` and 1 to 4 digits, at most 15 digits; issuer strips trailing fractional zeros and writes invariant culture; reader checks the raw token against the grammar, then reads `decimal`, never `double`; verify unit tests: `2.50` written `2.5`, `2.12340` accepted as `2.1234`, `5e2`, `-0`, `007` and `1.23456` rejected on read
- [ ] 2.4 Implement the strict reader rules: missing required field, unknown top-level field, `expires: null`, an inexact date format, a feature value of `false`, `null`, object or array, and a repeated feature name are each unreadable; verify a unit test for each
- [ ] 2.5 Implement signing: signing input is the ASCII bytes of `identifier + "." + base64url(payload)`, ECDSA P-256 SHA-256, 64-byte IEEE P1363 signature, base64url without padding; verify a unit test that the key string is single-line, has no whitespace, has three `.`-separated segments and starts with `LIC-XXXXX-XXXXX-XXXX`
- [ ] 2.6 Implement signature verification against a trusted public key; verify unit tests: valid key accepted; rejected when the payload is altered; rejected when the identifier is altered, including lowercased; rejected against the wrong public key
- [ ] 2.7 Implement reading a supplied string per ADR-0001: remove all whitespace; take the identifier from the first segment only on a full match of the 20-character uppercase pattern; require three non-empty base64url segments and a 64-byte signature; require `product` and `signingKeyId` strings before verification; verify unit tests for every row of ADR-0001's reading table, and that a key wrapped across lines with a trailing newline reads as the original (PDR-0021, PDR-0020)
- [ ] 2.8 Implement the schema check on a verified key's contents, reusing the rules in section 3, so a verified key whose contents break a rule is unreadable; verify a unit test with a key signed outside the generation API carrying `max-orders: -200` (PDR-0021)
- [ ] 2.9 Implement duplicate detection on identical signing input; verify a unit test that two strings with the same identifier and payload and different valid signatures are duplicates

## 3. Identifiers and value rules

- [ ] 3.1 Implement the product ID rule (`vendor.product`, lowercase) and verify unit tests for `acme.commerce` accepted and `commerce`, `Acme.Commerce` rejected
- [ ] 3.2 Implement license reference and key part generation from a cryptographically secure random source over the 31-character alphabet, and reference parsing ignoring case, hyphens and spaces; verify unit tests for format, alphabet, `lic-8f3ak m7rxb` parsing to `LIC-8F3AK-M7RXB`, and rejection of `O`, `0`, `1`, `I`, `L` and wrong lengths
- [ ] 3.3 Implement the feature rules: name rule, at most once per key, switch / number / text types, no explicit `false`; numbers zero or positive with at most 4 decimal places and 15 digits, held exactly; text non-empty, at most 256 characters, no leading or trailing whitespace, no control characters; verify unit tests for every feature row of `docs/license-examples.md` example 19

## 4. Signing-key management (`signing-key-management` spec)

- [ ] 4.1 Implement signing key pair creation returning a private key, a public key and a signing key ID, each exportable on its own; verify unit tests that the exported public key carries the signing key ID and the private key is not part of the public export
- [ ] 4.2 Implement the trusted key set addressed by signing key ID, including rejection of a duplicate signing key ID with a different public key; verify unit tests for add, resolve, remove and the duplicate rejection

## 5. License generation (`license-generation` spec)

- [ ] 5.1 Implement the generation API taking product ID, role, optional reference, optional expiry, features and a private signing key (the signing key ID derived from it, task 2.1), with the issue time from an injected `TimeProvider`; return the key string, reference, key identifier and issue time; verify unit tests for the minimal-license, full-contents, first-issue and reissue scenarios, and that the issue time is truncated to the second
- [ ] 5.2 Implement request checking that collects every broken rule (role required, product ID, reference, expiry not before the current UTC date, feature rules) and raises one error naming all of them, producing no key; verify unit tests for each rule, for expiry today accepted, and for a request breaking two rules reporting both
- [ ] 5.3 Verify by code review and a unit test that no private key material is stored, cached or exposed after a generation call returns, and that the API keeps no record of issued keys

## 6. Per-key evaluation (`license-validation` spec)

- [ ] 6.1 Implement the evaluation entry point taking an ordered list of key strings, a product ID, a trusted key set and a `TimeProvider`, returning one row per string in order plus a product result; verify unit tests for an empty list and for row order
- [ ] 6.2 Implement the failure checks in order (unreadable, wrong product, signing key not recognised, not verified); verify unit tests for each, and that a wrong-product key signed with an untrusted key is reported as wrong product
- [ ] 6.3 Implement failed-row reporting: reason plus claimed product and claimed identifier only; verify a unit test that an edited key's expiry and features appear nowhere in the result
- [ ] 6.4 Implement duplicates (identical signed contents, task 2.9; first counts, later copies name the row they copy); verify unit tests including a copy that differs only in whitespace
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

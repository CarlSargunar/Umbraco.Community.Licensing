> **Stale (2026-10-06).** Written for the purchase-list shape, which design.md "Exploration pass
> 2026-10-06" Q11 replaced. Regenerate with `opsx:update` before `opsx:apply`; section 1 must be
> redone.

Owners: section 1 and tasks 6.3 and 6.4 are Architect documentation tasks; sections 2 to 6 otherwise
belong to `worker-library` (`src/Umbraco.Community.Licensing`, `tests/Umbraco.Community.Licensing.Tests`).
Each worked example in `docs/license-examples.md` is a test case (task 6.1).

## 1. Product and technology records (written during `opsx:propose`)

- [x] 1.1 PDR-0036, PDR-0037, PDR-0038 written; PDR-0008 and PDR-0011 superseded; PDR-0009, PDR-0010, PDR-0018, PDR-0019, PDR-0021, PDR-0022 amended; decision index updated; verify each status line names the amending PDR
- [x] 1.2 `docs/license-examples.md` rewritten for the purchase schema and its "Last checked against" line updated; verify no example mentions *inactive*, a key-level role or a key-level vendor tag
- [x] 1.3 ADR-0001 revised for `purchases`; `issuing-add-on` design.md "Open" and `docs/deferred-scope.md` noted; verify ADR-0001's payload table has no `role` row

## 2. Payload format (ADR-0001, revised 2026-10-05)

- [ ] 2.1 Rename `LicenseRole` to `PurchaseKind` (`Base`, `AddOn`; wire `"base"`, `"add-on"`) and add the internal purchase payload type (kind, name, purchased, vendor tag, features); verify the solution builds
- [ ] 2.2 Change the payload writer to `signingKeyId`, `product`, `issued`, `expires` (omitted when none), `purchases` (array in the order given; each object `kind`, `name`, `purchased` as `yyyy-MM-dd`, `vendorTag` omitted when none, `features` omitted when none); verify a round-trip unit test of every field, two purchases with the same name, and every feature type
- [ ] 2.3 Change the strict reader: `role`, `vendorTag` and `features` at top level are unknown fields; `purchases` missing, `null`, not an array, `[]`, holding a non-object, or with zero or two `base` purchases is unreadable; in a purchase object, an unknown field, a repeated field, a missing `kind`, `name` or `purchased`, an unknown kind, an inexact `purchased` date, and a name breaking the name rule are unreadable; a feature name repeated within one purchase is unreadable and the same name in two purchases is not; verify a unit test for each
- [ ] 2.4 Verify a unit test that a key built with the pre-change payload (top-level `role` and `features`) and signed by a trusted key reads as unreadable with its identifier reported

## 3. Issuing (`license-generation` spec)

- [ ] 3.1 Add `LicensePurchase` and change `LicenseRequest` to a sealed record with `ProductId`, `Reference`, `Expires`, `Purchases`; remove `Role`, `VendorTag`, `Features`; update `LicenseIssuer.Issue` to write purchases; verify the "Generate a license key from its contents" scenarios, including "Minimal base license" and "Full contents"
- [ ] 3.2 Add the purchase rules to request checking: at least one purchase; exactly one `base`; kind required; name rule (1 to 64 characters, no leading or trailing whitespace, no control characters, never trimmed); purchase date required and not after the current UTC date from the injected `TimeProvider`; vendor tag rule per purchase; feature rules per purchase; verify the "Purchases", "Purchase name", "Purchase date", "Optional vendor tag" and "Feature rules" scenarios and every purchase row of `docs/license-examples.md` example 19
- [ ] 3.3 Extract the internal feature combiner (switch any, numbers summed exactly, equal text once, conflict on different text or text beside a switch or number), returning the combined set and the conflicting names; use it in request checking so a cross-purchase conflict is a problem on `features.<name>` naming the purchases; verify the "Features combine across purchases at issue" scenarios
- [ ] 3.4 Report problems with 1-based purchase paths (`purchases`, `purchases[2].name`, `purchases[1].features.max-orders`) and collect all of them; verify the "Rejection reports every problem" scenario
- [ ] 3.5 Use the combiner in the reader's schema check (ADR-0001 step 6) so a verified key with conflicting text across purchases is unreadable; verify a unit test with such a key signed outside the issuing API

## 4. Reissue request from a verified license

- [ ] 4.1 Add `VerifiedPurchase` and change `VerifiedLicense` to carry `Reference`, `Purchases` (in issued order) and `Features` (combined across purchases), removing `Role` and the key-level `VendorTag`; verify the "A verified key reports its claims" scenarios, including "List of purchases"
- [ ] 4.2 Add `LicenseRequest.ReissueOf(VerifiedLicense)` copying product, reference, expiry and every purchase with all fields, in order; verify the "Reissue request from a verified license" scenarios: append a purchase and issue (same reference and expiry, supersedes the earlier key on evaluation), unchanged reissue (same contents, new key part and issue time), and no license offered for a not-verified row

## 5. Evaluation (`license-validation` spec)

- [ ] 5.1 Remove `LicenseKeyState.Inactive`, the base / add-on rule and `LicenseKeyRow.SupersededRoleChange`; verify the "One state per key, first failing check wins" and "Later reissue supersedes" scenarios, including "Reissue changes the role" and "Reissue removes a purchase"
- [ ] 5.2 Replace `ProductLicense.ValidBaseCount` with `LicenseCount` (distinct references among valid rows) and derive `IsLicensed` from it; verify the "Licenses counted" scenarios, including tied keys counting as one license
- [ ] 5.3 Combine valid keys' combined features with the shared combiner (task 3.3); verify the "Combined features" scenarios, including "Base plus add-ons", "Two licenses" and "Conflicting text" between keys
- [ ] 5.4 Verify the "A failed key reports its claimed identifiers only" scenarios: no purchases, names, purchase dates or vendor tags are reported for a failed key

## 6. Examples, documentation and full check

- [ ] 6.1 Rewrite `ExampleTests` so every numbered example in `docs/license-examples.md` (1 to 20, including every row of example 19) is a test; verify `dotnet test` passes
- [ ] 6.2 Add a test or small program that issues ADR-0001's three worked example keys under the new payload (minimal license, a license with expiry, two purchases and all three feature types, and example 20's tagged license) with a fixed key pair and clock, and prints each key string, payload and length; verify each printed key evaluates as valid against the printed public key
- [ ] 6.3 (Architect) Replace ADR-0001's worked example keys, payloads, lengths and public key with task 6.2's output and remove the "Out of date" note; verify by evaluating each key in the ADR with the library
- [ ] 6.4 (Architect) Update `README.md`: issuing example with purchases and `ReissueOf`, schema table (no role), combining section (no *inactive*, licenses counted), state table; verify every code sample in it compiles against the library by copying it into a scratch test
- [ ] 6.5 Run `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` on the solution; verify all succeed with no warnings introduced by this change

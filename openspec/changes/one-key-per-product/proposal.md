## Why

Under PDR-0008 a key stands for one purchase, so a site holds one key per base license, add-on
and capacity pack, and every new add-on means storing one more key in every environment. The
implementor's job grows with each sale (`docs/deferred-scope.md`, "How implementors save
keys"). The Product Owner wants one key per product: each purchase reissues the license with
everything bought so far, and the new key replaces the old.

Prior art: Standard.Licensing (github.com/junian/Standard.Licensing) issues one signed license
per product holding a features dictionary, and handles add-ons and upgrades by reissuing that
license. It has no notion of the purchases inside it. This change keeps one license per product
and adds a signed list of purchases, so the site owner can still see what they bought.

## What Changes

- **BREAKING** A license key holds every purchase for its product: exactly one base purchase
  and zero or more add-on purchases. A new add-on reissues the license under the same reference
  with the purchase appended; the new key supersedes the old (PDR-0009). PDR-0036 supersedes
  PDR-0008.
- **BREAKING** Each purchase carries its kind (`base` or `add-on`), a name, its features, an
  optional vendor tag and its purchase date (PDR-0037). The key-level `role`, `vendorTag` and
  `features` are removed from the key; features and tags live on purchases.
- One expiry per license. Every purchase in it, add-ons included, shares the license's expiry
  or is perpetual with it (PDR-0036).
- Features combine across purchases within a key by the existing rules (switches any, numbers
  summed, text equal-or-conflict); a text conflict within one key is rejected at issue
  (PDR-0037).
- **BREAKING** Combining between keys no longer uses roles: every verified, current, unexpired
  key licenses the product. The *inactive* state and the *role changed* note are removed. Keys
  with different references still combine, and the result reports how many licenses count
  (PDR-0038, superseding PDR-0011).
- New: the evaluation result lists each verified key's purchases, so a license can be turned
  into a list of purchases.
- New: a reissue request can be prefilled from a verified license (reference, expiry and every
  purchase), so a vendor with no records adds a purchase to the key the site owner presents.
- Amended PDRs: PDR-0009, PDR-0010, PDR-0018, PDR-0019, PDR-0021, PDR-0022. ADR-0001 revised
  for the payload. `docs/license-examples.md` rewritten for the new schema.

Out of scope:

- The parked `issuing-add-on` change. Its decisions built on per-purchase keys and must be
  revisited when it resumes: PDR-0024 (type role), PDR-0025 (renewal renews every purchase),
  PDR-0026 (link records become purchases in the key), PDR-0027 (order reference per
  purchase), PDR-0034 (reissue role change), PDR-0035 (alignment becomes automatic). Noted in
  its design.md "Open".
- Add-ons with a term of their own (e.g. a monthly add-on on a yearly base). Not expressible
  with one expiry per license.
- PDR-0029 (expiry stated as a date or perpetual) stays with the `issuing-add-on` change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `license-generation`: a request holds purchases instead of a role, vendor tag and features;
  purchase rules; text conflicts across purchases rejected; a reissue request prefilled from a
  verified license.
- `license-validation`: a verified key reports its purchases; combining between keys without
  roles; *inactive* and the role-change note removed; the result reports the number of licenses
  counted.

## Impact

- Core library `src/Umbraco.Community.Licensing`: `LicenseRequest`, `LicenseIssuer`, payload
  reader and writer, rules, `LicenseEvaluator`, `LicenseEvaluation` (`VerifiedLicense`,
  `LicenseKeyRow`, `ProductLicense`, `LicenseKeyState`). Public API breaks; the library is
  unreleased and no keys exist in the field.
- Key format: payload field `purchases` replaces `role`, `vendorTag` and `features` (ADR-0001
  revision). Keys issued by the current branch become unreadable.
- Tests in `tests/Umbraco.Community.Licensing.Tests`, including the example tests, rewritten
  for the new schema.
- Docs: PDR-0036 to PDR-0038, the amendments above, decision index, ADR-0001,
  `docs/license-examples.md`, `docs/deferred-scope.md` (implementor note), `issuing-add-on`
  design.md "Open".

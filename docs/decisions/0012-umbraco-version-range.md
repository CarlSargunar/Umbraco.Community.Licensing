# PDR-0012: Umbraco version range: base licenses only, majors, inclusive

- **Status:** Decided, 2026-09-30
- **Source:** `openspec/changes/license-key-management/design.md` eighth pass (restrictions by role); Q12, decided in ninth pass
- **Serves:** site owner (a routine update never breaks a license), vendor (sells per major)

## Decision

- A **base** license may carry an optional minimum and maximum Umbraco **major**. An add-on with
  a range is rejected at issue. Add-ons may carry their own expiry.
- Both bounds are inclusive and cover every minor, patch and pre-release of their major.
- Either bound may be omitted: `17`..none is 17 and later; none..`18` is up to 18; neither is
  any version.
- Contiguous only. Issuing rejects non-whole-number bounds and a minimum above the maximum.

The library enforces two restrictions only: expiry and this range.

## Why

- The range is a **commercial** boundary ("Umbraco 19 is a paid upgrade"), and Umbraco is sold
  and upgraded by major. Only a major upgrade, which the site owner plans for, can take a
  product out of range.
- Base-only means an Umbraco upgrade takes a whole product out of range, never part of it: one
  message, one fix.
- Rejected at issue rather than ignored, so a range never silently has no effect.
- Add-ons keep expiry so subscription add-ons and capacity packs stay sellable (PDR-0008).

## Rejected

| Option | Why rejected |
|---|---|
| Range on add-ons | Partial breakage on upgrade; harder for the site owner to diagnose |
| Expiry on base only | Makes add-ons perpetual; no subscription add-ons |
| Full-version (semver) bounds | The motivating case, a package release depending on a CMS feature added mid-major, is a **compatibility** fact about one release, already enforced by the package's install requirements. A key covers every release and is fixed at issue, so the fact would outlive the dependency, and a site owner on 17.1 would see "license out of range" when the fix is a CMS update. No commercial use for minor precision identified |
| List of majors (`17, 19`) | Allows gaps (an LTS-only license) but cannot express "17 and later" |

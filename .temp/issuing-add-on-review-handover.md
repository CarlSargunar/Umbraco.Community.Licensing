# Handover: issuing-add-on spec review (updated 2026-10-05, end of session 2)

Phase: `opsx:explore`, Analyst hat. Resume with `/opsx:explore issuing-add-on` and point at this
file. Work one gap at a time: problem, personas, recommendation, alternatives table, Carl
decides, then save.

## Change state

```
  proposal.md   done (PDR ranges fixed to PDR-0023..PDR-0032)
  specs/        done (6 capabilities)
  design.md     PARTIAL: only "Requirements and open questions" (Q1-Q20 index + Open table).
                openspec status shows it done; other sections are Architect work for opsx:propose
  tasks.md      MISSING
  PDR-0023 .. PDR-0032 decided; ADR-0003 .. ADR-0005 decided
```

All edits uncommitted. `openspec validate issuing-add-on` passes.

## Done this session

| Item | Outcome | Records |
|---|---|---|
| F1 design.md missing | Rebuilt Q1-Q18 from PDR source lines as design.md's Questions table (wording is a summary, not original). Open items listed unnumbered | design.md |
| G1 rotation signed with a key sites don't trust yet (new finding) | Two-step rotation: Rotate -> pending (exportable, signs nothing) -> Make current after shipping a release trusting both. Emergency "make current now" with warning. Discard pending. At most one pending | PDR-0031 (Q19); `product-catalogue`, `activity-logging` specs |
| G2 new machine forces rotation (handover F2) | One location per machine, default `<home>/.<toolname>`, any folder allowed; `data/` + `signing-keys/` siblings. Folders are machine settings (per-user settings file), never in DB; default saved as "default" and resolved against home. Key files named by product + signing key ID, looked up in folder. Checks at setup, every start, at issue; remedies in order: change folder, restore, make pending current, make current now. Export signing keys (backup action, PDR-0007 guards). Import = manual copy, documented later. Move data folder / change keys folder kept | PDR-0032 (Q20); amends PDR-0023, PDR-0028; PDR-0031 consequence updated; `issuing-setup` rewritten, `product-catalogue`, `activity-logging` specs |
| B3 PDR-0006 lacks PDR-0027 mention | Closed, no change: PDR-0022 already amended PDR-0006 for the vendor tag | - |
| Index, examples | Decision index rows for PDR-0031, PDR-0032, PDR-0023 status; `license-examples.md` "Last checked against" updated | - |

## Corrections to the original findings

- **F3 month ends was not undefined.** PDR-0025 "Month ends" defines clamp, then minus a day.
  The gap: unequal periods (one-month terms starting Jan 28-31 all end Feb 27; 12 months from
  2028-02-29 is 364 days) and no spec scenario.
- **F6 aimed at the wrong thing.** The real gap is a correction reissue: the library corrects a
  wrong claim by reissuing under the same reference (PDR-0009, `license-examples.md` example 4),
  the add-on has no way to do it, and the proposal's out-of-scope line wrongly calls correction
  "not possible offline".

## Next: G3 rotation dependants (recommendation already presented, awaiting Carl)

- Problem: "Make the pending key current" counts licenses whose *current* key the retired key
  signed and is not expired. Misses superseded-but-unexpired keys (early renewal not yet
  installed: site still runs the old key) and leaves perpetual unclear (depends forever).
- Recommendation: a license depends on a signing key while **any** of its keys signed by it has
  not expired; perpetual never expires. Report at Make current and on a retired key's details:
  counts (dated, with last end date; perpetual, never ends), the date the old public key can be
  withdrawn from releases, license list on request. Perpetual message points to reissue (G4).
- Alternatives: current keys only with perpetual counted; count without date; leave perpetual
  out; track installed keys per site (out of scope, offline).
- Touches: PDR-0031 amendment (rule, withdrawal date, rotation step 5); `product-catalogue`
  "Make the pending key current" and its scenarios.

## Remaining after G3 (design.md "Open" table), suggested order

1. **G4 correction reissue** (see above). Decide: offer in this change, or defer to
   `docs/deferred-scope.md`. Linked to G3 (perpetual dependants) and PDR-0031 emergency path.
2. **G5 month ends**: keep PDR-0025 rule or end on the target month's last day when the start
   day does not exist; add scenarios for 31st, 30th, 29 Feb and chained renewals.
3. **G6 add-on aligned to base** (handover F7): linked add-on offers base's current expiry as an
   alternative pre-fill.
4. **G7 what the site owner receives** (handover F8): copyable block (product, type, reference,
   expiry, order reference, key) instead of a bare key string; re-send for base + linked add-ons.
5. **F5 reference unique per product**: `license-records` "Find a license" shows every match with
   its product; "Inspect a key" matches key identifier with claimed product.
6. **Smaller items**: data folder move and `logs`; un-retiring a license type; logging a product
   ID prefix change; "Inspect a key" and the deferred Umbraco version range (PDR-0012);
   PDR-0028 rejected table cites re-import but there is no import.

## Saving each decision

PDR (next Q number Q21, next PDR-0033) or amendment; spec edits; a row in design.md Questions
(remove from Open); decision index; `license-examples.md` "Last checked against" line; proposal
PDR ranges. Run `openspec validate issuing-add-on`. Ask before writing.

## Architect follow-ups (opsx:update / opsx:propose, not explore)

- ADR-0004: `SigningKey.PrivateKeyPath` becomes a file name; settings file holds the data and
  signing keys folders (PDR-0032). Signing key state (pending/current/retired) and made-current
  time (PDR-0031).
- Tool name for `<home>/.<toolname>`: decide in `opsx:propose`.
- Rest of design.md (Context, Goals, Decisions, Risks, Migration) and tasks.md.

## Agent setup work (from session 1, unchanged)

- Done: `CLAUDE.md` hat rule for update/sync/archive; apply flow stages each passed block;
  reviewer reviews unstaged + untracked; supervisor reviews everything uncommitted vs `HEAD`.
- Proposed, not yet approved:
  - `openspec/config.yaml`: `operations.apply.guidance` (delegate to agents, do not tick
    directly) and `rules.tasks` (verify clause per task; sections never mix library and web work).
  - Fixes go back to the same worker via SendMessage.
  - `CLAUDE.md` line: hats and coordination apply to the main session only, not subagents.
  - Project `.claude/settings.json` allow list for dotnet build/test/format, openspec, read-only git.
  - `.editorconfig` with style rules plus `EnforceCodeStyleInBuild` (touches build).
  - Move the delta-megalith `autoMode` block out of global `~/.claude/settings.json`.
  - PreToolUse hook enforcing read-only for reviewer and supervisor (optional).
  - Name the ADR-0002 Key Vault package explicitly in `worker-library`.
- Deferred by Carl: untracking `.temp/`; web worker projects come later.

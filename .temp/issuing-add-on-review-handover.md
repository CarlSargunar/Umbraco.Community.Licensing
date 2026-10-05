# Handover: issuing-add-on spec review (2026-10-05)

Phase: `opsx:explore`, Analyst hat. Nothing in `openspec/` or `docs/` has been changed by this
review. Resume with `/opsx:explore issuing-add-on` and point at this file.

## Change state

```
  proposal.md   done
  specs/        done (6 capabilities, 671 lines)
  design.md     MISSING (never in git)
  tasks.md      MISSING
  PDR-0023 .. PDR-0030 decided; ADR-0003 .. ADR-0005 decided
```

Read for context: `openspec/changes/issuing-add-on/proposal.md`, all `specs/*/spec.md`,
`docs/decisions/0023`..`0030`, `docs/personas.md`.

## Findings

Ordered by significance. "Decision" = needs Carl (product). "Fix" = record/spec correction.

### F1. design.md does not exist (Fix, blocks traceability)
- PDR-0023..PDR-0030 cite `issuing-add-on/design.md` Q1..Q18 as source; ADR-0003..ADR-0005
  cite its "Decisions" section. The file is not in the working tree or git history
  (`git log --all -- openspec/changes/issuing-add-on/design.md` is empty).
- Open question for Carl: does the Q list exist anywhere, or was it only in conversation?
- `proposal.md` lists PDR-0023..PDR-0029; omits PDR-0030 (cited by `license-records` spec).
- design.md itself is Architect work (`opsx:propose` / `opsx:update`); the Q list is product record.

### F2. New machine forces key rotation (Decision)
- `issuing-setup` spec, "Change the signing keys folder": each signing key keeps the absolute
  file location recorded at creation.
- Restore on a new machine (other drive letter, user name, or OS) -> every key reports
  "Missing private key file" (`product-catalogue` spec) -> "restore or rotate". Restoring to a
  Windows path on macOS is impossible, so rotation is forced: a product release with a new
  public key, cost borne by implementor and site owner. Conflicts with persona priority.
- Options:
  a. Re-point a signing key to a file; accepted only if it matches the recorded public key
     (the existing check already compares).
  b. Store locations relative to the signing keys folder; changing the folder re-points all keys.
- Likely outcome: amend PDR-0023 or PDR-0007-related rules; edit `issuing-setup` and
  `product-catalogue` specs.

### F3. Month arithmetic at month end undefined (Decision)
- PDR-0025: period runs to "the day before S plus N months". No rule when day S does not exist
  in the target month.
  ```
  new sale 2027-01-31, 1 month:  clamp -> S+1m 2027-02-28 -> expires 2027-02-27
                                 overflow -> 2027-03-03   -> expires 2027-03-02
  renewal then starts 2027-02-28 -> anchor drifts 31st -> 27th
  new sale 2028-02-29, 12 months -> 2029-02-27 or 2029-02-28?
  ```
- No scenario in `license-issuing` starts a period on day 29-31.
- Candidate rule: when the start day does not exist in the target month, the period ends on
  the target month's last day. Needs scenarios for 31st, 30th, 29 Feb, and chained renewals.
- Likely outcome: amend PDR-0025; add scenarios to `license-issuing`; update
  `docs/license-examples.md` if it carries date examples.

### F4. Rotation undercounts dependants on the old key (Decision)
- `product-catalogue` "Rotate the signing key" counts licenses whose current key is "not expired".
- Perpetual licenses: included or not? Undefined. They depend on the old public key forever;
  should be reported separately, with "old key can never be withdrawn while these exist".
- Superseded but unexpired keys: an early renewal leaves the old key valid for weeks; a site
  owner who has not installed the renewal still depends on the old public key. Withdrawing it
  breaks a paying site (primary persona).
- Likely outcome: amend rotation requirement and its scenarios.

### F5. Reference unique per product, search assumes unique overall (Fix)
- PDR-0017 / `license-issuing`: references unique per product, not across records.
- `license-records` "Find a license" scenario: "SHALL show license" (singular). Same reference
  can exist in two products -> show every match with its product.
- "Inspect a key": a key identifier match must also match the claimed product.

### F6. No way to mark a mistaken issue (Decision)
- Correction and revocation are out of scope (offline). A key issued in error and never sent
  stays an active license, counts in rotation dependants (F4), appears in searches.
- Option: records-only "void, never delivered" marker; does not claim revocation.
- Decide: in this change, or defer to `docs/deferred-scope.md`.

### F7. Add-on not aligned to base expiry (Decision)
- Linked add-on pre-fills today + own term. Mid-term add-on sales usually want to end with the
  base, so the site owner renews once a year, not on two dates.
- Option: when linked, offer the base's current expiry as an alternative pre-fill. An issuing
  convention, so it belongs in the add-on (PDR-0023 amendment, PDR-0029).

### F8. Site owner receives a bare key string (Decision, small)
- `license-issuing` "Key shown once issued" is vendor-facing. Site owner persona "needs to
  know what they have".
- Option: copyable block with product, license type, reference, expiry, order reference, key.
  Re-send could produce one block for a base license plus its linked add-ons.

### Smaller items
- `issuing-setup` "Move the data folder": copies the database; the `logs` folder is not mentioned.
- `product-catalogue`: can a retired license type be un-retired? Unspecified.
- `activity-logging`: product ID prefix change after setup not in the logged actions.
- `license-records` "Inspect a key": PDR-0012 Umbraco version range. What does the add-on show
  without a site context?
- PDR-0028 Rejected table cites "easier to re-import"; there is no import (own export or other).

## Suggested order
1. F1 (find or reconstruct the Q list; it is the source for every new PDR).
2. Decisions F2, F3, F4, F6 (bigger product impact), then F7, F8.
3. Fixes F5 and the smaller items.

Per CLAUDE.md: each settled decision gets a PDR or PDR amendment, spec edits, a design.md
cross-reference, and a `docs/license-examples.md` "Last checked against" update. Ask before writing.

## Agent setup work done this session (context)
- `CLAUDE.md`: added `opsx:update / sync / archive` hat rule (hat follows the artifact);
  apply flow step 4-5 now stages each passed block (`git add <paths>`, Architect only, never commit).
- `.claude/agents/reviewer.md`: reviews unstaged diff + untracked files; staged = earlier blocks.
- `.claude/agents/supervisor.md`: reviews everything uncommitted against `HEAD`.
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
- Deferred by Carl: untracking `.temp/` (he will remove it later); web worker projects come later.

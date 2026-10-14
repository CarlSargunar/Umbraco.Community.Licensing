# PDR-0029: A release tag must match its release log row

- Status: Decided
- Date: 2026-10-14
- Source: `add-release-pipeline` design.md Q5. Amends PDR-0027.
- Personas: vendor (a tag pushed by mistake does not rebuild or republish an earlier version).
  No persona harmed: a release started with the release command always passes this check.

## Context

PDR-0027 shares the tag format `release-yyyymmdd` with Carl's personal release-tag command, which
tags without bumping the version or adding a log row. Used in this repository by habit, it would
start a release of the version already released. PDR-0027's refusals let that run build and
test, and only the package feed's duplicate check would stop it.

## Decision

- A release is also refused, and nothing is published, when the release log row for the version
  names a tag other than the one that started the release.
- This is a sixth refusal, added to the five in PDR-0027.

## Reasons

- The release command writes the tag into the log row it adds, so its releases always match.
- A tag that did not come from the release command is refused at the start, with a reason that
  names the mismatch, instead of failing late at the feed.

## Rejected options

| Option | Why rejected |
|---|---|
| Keep PDR-0027's five refusals; the feed refuses a duplicate | The run builds and tests first and fails with the feed's message, which does not name the cause |
| Use a different tag format for this repository | PDR-0027 chose the shared format; both commands keep it |

## Consequences

- A release log row edited by hand to name another tag stops that release.

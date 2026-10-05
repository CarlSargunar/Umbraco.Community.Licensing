# PDR-0021: Whitespace in a key string is ignored; contents that break the schema are unreadable

- **Status:** Decided, 2026-10-02. The second rule is provisional: revisit if vendors meet it. Amended by PDR-0037, 2026-10-05: the schema now includes purchases; a missing base purchase or an unknown purchase kind makes a verified key unreadable
- **Source:** `openspec/changes/license-key-management/design.md` Q20
- **Serves:** implementor (a key wrapped or padded by email or a config file still works), site owner (never shown an entitlement the core would refuse to issue); **Cost to:** vendor (a key its own tool mis-built is reported as unreadable, with "paste again" as the first action)

## Decision

**Whitespace.** Before a key string is read, all whitespace in it is removed: spaces, tabs and
line breaks, at either end or anywhere inside. A key never contains whitespace, so removing it
cannot change a correctly copied key.

**Contents that break the schema.** A key that verifies but whose contents break a rule
checked at issue is reported as *unreadable*. Examples: no base purchase or an unknown purchase kind (PDR-0037), a negative
or malformed number, a feature name in capitals, a name repeated in one key, text with a line
break. Its identifier is reported if its start is readable, as for any unreadable key
(PDR-0019, PDR-0020). It takes no part in superseding or combining.

```
  STRING AS SUPPLIED                         READ AS
  "  LIC-8F3AK-M7RXB-7Q2D.q83KdL...Zx9PzQ\n"  the key, trimmed
  "LIC-8F3AK-M7RXB-7Q2D.q83KdL...\n  Zx9PzQ"  the key, line break removed
  verified, but max-orders: -200              unreadable
```

## Why

- **Whitespace is a paste artifact, never part of a key.** Email clients wrap long lines and
  indent continuations; config files and environment variables pick up trailing newlines. The
  implementor cannot see the difference, and "unreadable" for an invisible character costs a
  support round trip for nothing.
- **Anywhere, not only at the ends.** Trimming the ends misses the commonest damage, a line
  wrapped in an email. Removing every whitespace character covers both with one rule.
- **A verified key with bad contents can only come from a faulty vendor tool.** The core
  rejects such contents at issue (`docs/license-examples.md` example 19), so a trusted
  signature over them means the vendor signed outside the core or with a defective version.
- **Unreadable is the simplest safe answer.** Reporting the key as verified would show and
  grant contents the core would have refused, and every reader of the result would need its
  own rule for a negative number or an unknown role. Reporting it as unreadable reuses an
  existing state and grants nothing.

## Rejected

| Option | Why rejected |
|---|---|
| Whitespace makes the key unreadable | Punishes the implementor for an invisible paste artifact |
| Trim the ends only | Misses a key wrapped across lines by an email client |
| A verified key with bad contents counts, ignoring the bad parts | Grants a partial entitlement from contents the core would refuse; which parts to ignore is a rule per field |
| A new reason, such as *invalid contents* | Correct wording and a better first action (ask the vendor), but a ninth reason for a case only a faulty vendor tool produces. Kept as the option to take if vendors meet it |

## Consequences

- The first action for *unreadable*, "paste the key again" (PDR-0019), is wrong for a key with
  bad contents: pasting it again gives the same result. The vendor must reissue. Accepted while
  the case is rare.
- The generation spec's single-line rule stands: whitespace tolerance is on the reading side
  only.
- Two strings that differ only in whitespace are the same key, and the second is a
  *duplicate* (PDR-0009).

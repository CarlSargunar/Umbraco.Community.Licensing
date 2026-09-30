# PDR-0013: Feature names are restricted; lookups ignore case

- **Status:** Decided, 2026-09-30
- **Source:** `openspec/changes/license-key-management/design.md` Q11, decided in ninth pass
- **Serves:** site owner (never silently loses a paid feature), vendor (typos fail at issue); **Cost to:** vendor (no display-style names)

## Decision

A feature name is lowercase `a-z`, digits and hyphens, starting with a letter. Issuing rejects
any other name. A package's lookup ignores case, so asking for `Pro` finds `pro`.

## Why

Licenses combine (PDR-0011), so a name spelled two ways splits one feature into two:

```
  LIC-8F3A  base    max-orders 500
  LIC-40BE  add-on  Max-Orders 1000
  case-sensitive  -> package sees 500, site owner paid for 1500
```

Restricting names at issue means one spelling per feature everywhere, and the mistake fails at
the vendor, like malformed values (PDR-0010).

## Rejected

| Option | Why rejected |
|---|---|
| Any characters, matched ignoring case | Misses trailing spaces, `_` vs `-`, look-alike letters from other alphabets, and letters whose case changes with the server's language (Turkish dotted / dotless i), which can give different answers on different servers |
| Case-sensitive exact match | Exposes the site owner to the silent split above |

## Consequences

Names such as `AI Assist` are not possible; the inventory shows `ai-assist`.

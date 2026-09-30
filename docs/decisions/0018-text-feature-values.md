# PDR-0018: Product features may carry text; text never combines

- **Status:** Decided, 2026-09-30. Amends PDR-0010, which rejected text values
- **Source:** `openspec/changes/license-key-management/design.md` R7, Q16
- **Serves:** vendor (carry a domain, an external identifier or other vendor data the library
  does not model); implementor (a conflict names the keys involved); **Cost to:** site owner (a
  mis-issued key blanks a text feature until reissued; the inventory says so)

## Decision

A feature value may be **text**, alongside switches and numbers (PDR-0010). A text feature may
appear on a base or an add-on license.

| Type | Example |
|---|---|
| Text | `licensed-domain: example.com`, `tenant: 7f3a-…`, `region: eu` |

Rules checked when the key is issued:

- Non-empty; at most **256 characters**; no leading or trailing whitespace; no line breaks or
  other control characters.
- The type is what the vendor issues. A text value is never interpreted: `"500"` as text is
  text, is never summed, and conflicts with a number under the same name.
- A feature name still appears at most once in a key (PDR-0010).

Combining (PDR-0011):

- Text values **never combine**. Among the counted licenses for a product (valid bases and
  active add-ons), a text feature name must resolve to **one value**.
- **Equal values in several licenses count as one.** Equality is an exact comparison: same
  characters, same case.
- **Different values, or a text value alongside a switch or number under the same name, are a
  conflict.** The feature answers nothing at product level, as if not granted. The inventory
  flags the product and every license carrying that name. The licenses themselves stay valid:
  features do not affect validity (PDR-0010).
- Superseded, invalid and inactive licenses take no part; only counted licenses can conflict.

The library reports the value; the package acts on it. What `licensed-domain` means, and what
happens when the site does not match, is the vendor's decision.

```
  LIC-8F3AK  base    licensed-domain example.com   valid
  LIC-4Z9BE  add-on  licensed-domain example.com   valid
  -> licensed-domain: example.com                  (equal: one value)

  LIC-8F3AK  base    licensed-domain example.com   valid
  LIC-4Z9BE  add-on  licensed-domain other.com     valid
  -> licensed-domain: CONFLICT   both keys flagged; product still licensed
```

## Why

- Some values a vendor needs to carry are not enumerable when the package is built, so a
  switch cannot stand for them: a domain the license is meant for, a tenant or account
  identifier used against the vendor's service, or vendor data the library has no reason to
  model. PDR-0010 rejected text for lack of a combining rule, not for lack of a use.
- The combining objection (concatenate, or latest wins so a cheap add-on overrides the base)
  is answered by not combining. One name, one value; anything else is a vendor mistake and
  fails visibly on the site rather than silently picking a winner.
- Equal values count as one so that a base and an add-on issued for the same site do not
  conflict merely by both stating the domain.
- Exact comparison: the library does not know whether a value is case-sensitive (an
  identifier) or not (a domain). Ignoring case would merge identifiers that differ, and would
  depend on the server's language for letters such as the Turkish dotted / dotless i
  (PDR-0013). A spelling difference shows as a conflict and a reissue fixes it.
- Conflict leaves the licenses valid. A wrong text value on an add-on should never take the
  product away; it takes away one feature, and the inventory names the keys.
- Rejecting leading and trailing whitespace at issue removes the commonest source of
  values that look equal but are not.
- A size limit keeps keys pasteable (PDR-0017 reasoning); 256 covers any domain name and any
  common identifier format.

## Rejected

| Option | Why rejected |
|---|---|
| All distinct values held as a set | Text features stop being "a value" and every package must handle a list; for exclusive values such as an edition or a tenant, a set has no meaning the library can state |
| Base wins over add-on | A selection rule, silent on the site; two bases or two add-ons still need a conflict rule anyway |
| Latest issued wins | Order-dependent; a cheap add-on overrides the base (PDR-0010) |
| No product-level answer; read per license | Packages iterate licenses and re-implement the same rule; the inventory cannot show one entitlement |
| Text on base licenses only | An add-on may legitimately carry an identifier of its own (a second tenant, a service account) |
| Ignore case when comparing | See Why: identifiers, and server-language-dependent results |
| Conflict invalidates the licenses involved | Turns a vendor's feature mistake into a site outage; contradicts "features do not affect validity" |
| Interpret numeric-looking text as a number | Hides the vendor's type mistake; the two types have different combining rules |
| No length limit | Keys must survive email and a single config value (PDR-0017) |

## Consequences

- PDR-0010 is amended: "text values" moves from its Rejected table to this decision. Its
  rules for switches and numbers stand.
- The inventory (R2) gains a **conflict** marking at feature level, distinct from the license
  states in PDR-0011.
- Site or domain binding remains a vendor convention. Keys are bearer tokens (PDR-0011); the
  library does not check a text value against the site.

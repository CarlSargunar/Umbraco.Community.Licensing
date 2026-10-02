# PDR-0020: Every key starts with a visible key identifier

- **Status:** Decided, 2026-10-02
- **Source:** `openspec/changes/license-key-management/design.md` Q5 (how a result is matched back to its key)
- **Serves:** implementor (an exact string to search for; stable when keys are added or removed), site owner (can see which key is which, and which purchase is broken), backoffice editor (nothing secret is shown)

## Decision

Every key string starts with its **key identifier** in plain sight, followed by the opaque
part.

```
  LIC-8F3AK-M7RXB-7Q2D . q83KdL...............Zx9PzQ
  |_____________| |__|   |________________________|
   license        key     opaque part: contents and
   reference      part    signature
```

- **Key identifier = license reference + key part.** The reference names the license
  (PDR-0009). The key part is short and random, set by the core every time a key is issued,
  including a reissue. It is never a vendor input. Format: PDR-0017.
- **It grants nothing and reveals nothing.** It is not secret.
- **It is covered by the signature.** A key whose visible identifier has been edited does not
  verify.
- **The evaluation result names every key by its identifier**, and never contains any other
  part of a key.
- **On a key that fails verification the identifier is a claim** (PDR-0019): "claims to be".
- **Finding where a key is stored is a search.** The identifier appears in the key string as
  the result shows it, so the implementor searches their settings for it and the site owner
  searches their emails. The library does not know or report where a key is stored.
- **Fallback.** Results are returned one per supplied string, in the order supplied. A string
  with no readable identifier, such as a pasted password, can only be identified by position.

## Why

- **Unreadable keys otherwise have no name.** A verified key has its reference; a cut-off key
  has nothing. With the identifier at the start, a key cut off at the end still says which
  key, and which purchase, it is.
- **Position alone is not enough.** It does not tell the implementor or site owner which key
  is meant once the list is out of sight, and positions shift when a key is added or removed
  elsewhere.
- **The reference alone is not unique per key.** A renewal and the key it replaces share a
  reference, so "LIC-8F3AK-M7RXB is superseded" would match two strings. The key part tells
  them apart.
- **Simplest thing that meets the criteria**: identify a key explicitly, expose no part of the
  key that matters, and reveal nothing else. An implementor who can identify the incorrect key
  can correct it.
- **Readable by people.** A site owner with several emailed keys, or an implementor with a
  settings file full of opaque strings, can see which is which without the library.

## Rejected

| Option | Why rejected |
|---|---|
| Position only | See Why |
| A label supplied by the caller for each key, repeated in the result | More for every vendor to do; optional, so nobody can rely on it; brings back part of the deferred site label (`docs/deferred-scope.md` D7) |
| A fragment of the key string in the result | Puts part of a bearer token into results that are logged and displayed; a cut-off key's fragment may not match the original; fragments may not differ between keys |
| The license reference alone as the identifier | Not unique per key (see Why) |
| The result names the place a key is stored | Needs the caller's label or a store. A searchable identifier is enough |

## Consequences

- PDR-0017 gains the key identifier's format.
- The key's encoding must carry the identifier visibly and bind it to the signature. That is a
  technology decision: ADR-0001 must be revised in the propose phase.
- The identifier covers the "mangled paste" job that the deferred site label was kept for
  (`docs/deferred-scope.md` D7). The label's remaining job is the implementor's own naming
  scheme.
- A key whose start is damaged loses its identifier and falls back to position.
- Two supplied keys with the same identifier are the same key supplied twice (or, rarely, a
  clash of key parts under one reference). An exact copy counts once and is reported as
  *duplicate* (PDR-0009).
- Worked example: `docs/license-examples.md` example 18.

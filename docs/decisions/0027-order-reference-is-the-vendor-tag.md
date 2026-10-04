# PDR-0027: The add-on's order reference is signed as the vendor tag

- **Status:** Decided, 2026-10-04
- **Source:** `openspec/changes/issuing-add-on/design.md` Q7
- **Serves:** vendor (one field; from a key to its order), site owner (quotes the order number
  on their receipt); **Cost to:** vendor (order references must meet the vendor tag rules)

## Decision

Each issued key may carry an optional **order reference**, entered at issue. It is stored in the
record and signed into the key as the vendor tag (PDR-0022), so it follows the vendor tag rules:
1 to 64 characters from `A-Z a-z 0-9 - _ . # /`, rejected, never cleaned.

A renewal is a new order: its order reference is entered afresh, not carried forward
(PDR-0022). Lookup by order reference is required (PDR-0006), and matches exactly, ignoring
case.

The prompt for it says it is for an order or invoice number and must never hold a name, email
address, company or other personal data, because a key cannot be withdrawn.

This amends PDR-0006, which described the order reference as free text.

## Why

- **One field.** A separate free-text order reference and vendor tag would hold the same value
  most of the time, and the vendor would have to decide which goes where.
- **In the key.** The site owner and the vendor's support can name the order from a key's
  evaluation result without handling the key (PDR-0022).
- **Same rules.** A value the add-on stores but the library would reject cannot be signed.

## Rejected

| Option | Why rejected |
|---|---|
| Free-text order reference, not signed | Loses PDR-0022's lookup from a key; allows spaces, so names and emails pass in their usual form |
| Free-text order reference plus a separate vendor tag | Two fields for one value |
| Clean the order reference to fit the tag rules | Rejected by PDR-0022: the signed value would differ from the vendor's own record |

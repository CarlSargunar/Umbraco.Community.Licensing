# PDR-0005: The core issuing API signs only and keeps no records

- **Status:** Decided, 2026-09-28
- **Source:** `openspec/changes/license-key-management/design.md` R10
- **Serves:** vendor

## Decision

The core turns key contents into a signed key string and checks the input. It keeps no records
of products, issued keys or customers.

## Why

Vendors with a shop or CRM already hold their customers and orders. They call the core from
their own systems, including automatic issuance on purchase, and keep their own records. A core
that stored records would duplicate those systems and carry their data duties. Vendors without
such systems use the optional add-on (PDR-0006).

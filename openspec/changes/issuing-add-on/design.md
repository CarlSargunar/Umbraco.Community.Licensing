> **Partial.** Only *Requirements and open questions* is written. Context, Goals / Non-Goals,
> Decisions, Risks / Trade-offs and Migration Plan are Architect work for `opsx:propose` /
> `opsx:update`. ADR-0003 to ADR-0005 cite the Decisions section, which does not exist yet.

## Requirements and open questions

Product decisions are PDRs in [`docs/decisions/`](../../../docs/decisions/README.md); the key
schema and worked examples are in [`docs/license-examples.md`](../../../docs/license-examples.md).
This section indexes the questions those records cite, and holds the open ones.

Q1 to Q18 were settled in conversation on 2026-10-04 and 2026-10-05 and numbered in the PDRs'
source lines; this index was reconstructed from those source lines on 2026-10-05. The question
wording below is a summary of what each PDR decides, not the original wording. Q11 to Q18 were
tracked in conversation as U1 to U8.

### Questions

| Q | Question | Outcome |
|---|---|---|
| Q1 | Who runs the add-on, and where? | PDR-0023: one vendor, on their own machine; no server, no accounts |
| Q2 | Menus and prompts, or command verbs? | PDR-0023: interactive; command verbs later if needed |
| Q3 | Where do the records and the private keys live? | PDR-0023: a local database in a data folder; private keys in a separate signing keys folder (PDR-0007). Locations amended by PDR-0032 (Q20) |
| Q4 | How does the vendor describe what it sells? | PDR-0024: license types are templates, editable at issue |
| Q5 | How is a renewal's period calculated? | PDR-0025: continues from the current expiry; after a lapse the vendor chooses old expiry or today |
| Q6 | How are an add-on and its base related in the records? | PDR-0026: optional link in the records, never in the key |
| Q7 | Where does an order or invoice number go? | PDR-0027: optional order reference, signed as the vendor tag |
| Q8 | What does the CSV export contain? | PDR-0028: all records, key strings included; never private keys |
| Q9 | What does the log contain? | PDR-0028: every action with its identifiers; never a key string or private key |
| Q10 | Who decides a key's expiry, and may it be omitted? | PDR-0029: always stated, a date or perpetual; the add-on calculates, the library checks. Amends PDR-0023 (license rule vs issuing convention) and PDR-0025 |
| Q11 | Is a license perpetual because of its type or its key? (U1) | PDR-0025 amendment: its current key; no term stored on the license |
| Q12 | Where does a renewal's features come from? (U2) | PDR-0025, PDR-0024 amendments: the current key when the type is kept, the new type's defaults when it switches; differences shown at the summary |
| Q13 | Which license types does a renewal offer? (U3) | PDR-0024, PDR-0025 amendments: active types of the same product and role only; a retired type must switch |
| Q14 | Does the add-on link belong to a key or a license, and can it change? (U4) | PDR-0026 amendment: the license; kept across renewals; editable any time, to a base of any status; logged |
| Q15 | What statuses does a license have in the add-on? (U5) | PDR-0030: perpetual, active, expiring (30 days), expired; exclusive |
| Q16 | What happens when a folder given for one run holds no database? (U6) | PDR-0023 amendment: ask, default no |
| Q17 | May two copies run on one data folder? (U7) | PDR-0023 amendment: no; different folders may |
| Q18 | How are features exported to CSV? (U8) | PDR-0028 amendment: separate features files, six files in all |
| Q19 | How does a signing key rotation avoid issuing keys sites cannot verify yet? | PDR-0031: two steps, pending then make current; emergency make current now |
| Q20 | How does the add-on find its data and private keys on a new machine? | PDR-0032: one location per machine, default `<home>/.<toolname>`; folders are machine settings; key files by name; checked at setup, start and issue; signing key export |

### Open

From the review of 2026-10-05. Numbered Q21 onward when taken up.

| Topic | Question |
|---|---|
| Rotation dependants | Which licenses count as depending on a retired signing key: perpetual licenses, and superseded keys that have not expired? When can the old public key be withdrawn? |
| Correction reissue | The library corrects a wrong claim by reissuing under the same reference (PDR-0009, `license-examples.md` example 4). Should the add-on offer a correction reissue outside a renewal? The proposal currently calls correction "not possible offline" |
| Month ends | PDR-0025 clamps then subtracts a day, so periods starting on 28 to 31 January all end 27 February, and 12 months from 2028-02-29 is 364 days. Keep, or end on the target month's last day when the start day does not exist? Spec scenarios needed either way |
| Add-on aligned to base | Should a linked add-on offer the base's current expiry as an alternative pre-fill? |
| What the site owner receives | Should the add-on produce a copyable block (product, type, reference, expiry, order reference, key) instead of a bare key string? |
| Reference unique per product | `license-records` "Find a license" assumes a reference is unique overall; it is unique per product (PDR-0017). Show every match with its product; match a key identifier with its claimed product |
| Smaller items | Data folder move and the `logs` folder; un-retiring a license type; logging a product ID prefix change; what "Inspect a key" shows for the Umbraco version range (PDR-0012, deferred); PDR-0028 rejected table cites re-import, but there is no import |

U2. Where a renewal's features come from

Gap: the license-issuing "Renew a license" requirement says the vendor "MAY edit the expiry and features" but never says what is pre-filled. PDR-0025 says "from the current key, or from the newly chosen type".

Why it matters: it decides whether one-off edits survive a renewal.

  Commerce Pro type: max-orders 500     sold with max-orders 750 (negotiated)

  PRE-FILL FROM    RENEWED KEY    EFFECT
  current key      750            negotiated limit kept
  type             500            site owner silently loses 250 orders

There is also a conflict with PDR-0024 ("editing a type affects only keys issued afterwards"). A renewal is issued afterwards. Suppose the vendor raises Commerce Pro to 600. Pre-filling from the key means renewals never pick that up unless the vendor switches type. Pre-filling from the type means the negotiated 750 drops to 600.

Simplest fix: keep the type, pre-fill from the current key. Switch type, pre-fill from the new type. Either way, the summary shows where the features differ from the chosen type's defaults, so the vendor sees both the negotiated value and the type change and decides. This protects the site owner, the primary persona, from losing something silently.

---

U3. Retired types at renewal

Gap: a retired type is "kept for existing licenses and their renewals". But when the vendor switches type at renewal, the spec doesn't say which types are on offer.

  Commerce Pro (retired) --renewing--> offered: ?
                                       Commerce Pro (own type, retired)      yes, by spec
                                       Commerce Pro 2026 (active)            yes
                                       Commerce Basic (another retired one)  ?

Simplest fix: offer the active types of the same product and role, plus the license's own type even if it's retired. A retired type is never a destination for any other license.

---

U4. Add-on links over time

Gap: a link is set only when an add-on is issued, and the record keeps it per key. That leaves three things undefined:
- Late link: the add-on was sold first and the base was found later. Can the vendor link it then?
- Renewal: does a renewed add-on keep its link? The link is per key, so it isn't clear it carries over.
- Expired base: can an add-on link to a base whose current key has expired?

Simplest fix:
- The link belongs to the license (the reference), not to a key, so renewals keep it automatically.
- It can be added, changed or removed at any time.
- Any base license of the same product can be linked, whatever its status.
- Link changes are logged.

The link is record-keeping only (PDR-0026). It is never signed or checked, so being strict about it protects no one.

---

U5. Listing statuses overlap

Gap: the filters are "active, expiring within 30 days, expired, perpetual". A license expiring in 10 days is both active and expiring, and a perpetual one is also active. A list row needs one status, and a filter needs a clear meaning.

Simplest fix: four exclusive statuses, read from the current key. Each license has exactly one, and both filtering and display use that same value.

  perpetual    no expiry
  active       expires in more than 30 days
  expiring     expires today to 30 days from now
  expired      expiry date has passed (UTC)

---

U6. Running against a different data folder with no database

Gap: the issuing-setup requirement "Data folder for one run" says to run setup when that folder has no database. It also says to leave the saved settings unchanged. Normally, setup is what saves settings.

Why it matters: a mistyped folder path at start would land the vendor in setup for an empty folder. If they click through, they issue new licenses into a database that tracks nothing they've issued before. PDR-0023 exists to prevent exactly that.

Simplest fix:
- If the folder has no database, say so and ask "create a new database here?", defaulting to no. The "Missing data at start" requirement already behaves this way.
- If the vendor says yes, setup creates the database in that folder. The keys folder and prefix are stored in that database.
- The saved pointer to the vendor's usual data folder stays unchanged.

---

U7. Two copies open at once

Gap: concurrent use is out of scope, but nothing stops a vendor from opening two terminal windows.

Why it matters: most actions would still work. One sequence loses a key:

  window A: move data folder --> copies DB, switches, deletes old DB
  window B: still on old DB  --> issues key, shows it, records it in old DB
                                 vendor sends key; old DB is deleted
  result:   site owner has a key the records don't know --> can't re-send or renew

The same-second and reference-uniqueness safeguards also assume only one writer.

Simplest fix: refuse to start a second copy against the same data folder, with a message saying another copy is open. Different data folders, like the U6 sandbox, can run side by side.

---

U8. Features in the CSV export

Gap: the issued-keys file has one row per key, but a key has 0 to N features. License types have the same problem with their default features. PDR-0028 also says one file per table is "easier to open and re-import".

Why one cell is awkward: text feature values may contain ;, = and , (PDR-0018 only bans line breaks, control characters and surrounding whitespace). Any separator you pick needs escaping, and the result is hard to read or filter in a spreadsheet.

┌─────────────────────────────────────┬─────────────────────┬─────────────────────────┬───────────────────┐
│               Option                │   Readable in a     │      Re-importable      │       Cost        │
│                                     │     spreadsheet     │                         │                   │
├─────────────────────────────────────┼─────────────────────┼─────────────────────────┼───────────────────┤
│ One cell, encoded list              │ Poor                │ Needs a parser          │ Escaping rules    │
├─────────────────────────────────────┼─────────────────────┼─────────────────────────┼───────────────────┤
│ A column per feature name           │ Good for few names  │ Columns vary between    │ Sparse, shape     │
│                                     │                     │ exports                 │ changes           │
├─────────────────────────────────────┼─────────────────────┼─────────────────────────┼───────────────────┤
│ Separate features file: one row per │ Good, filterable    │ Straightforward         │ One more file per │
│  key and feature                    │                     │                         │  owner            │
└─────────────────────────────────────┴─────────────────────┴─────────────────────────┴───────────────────┘

Simplest fix: two more files, issued key features and license type features, each linked to its parent row by identifier. The export grows from four files to six, which amends PDR-0028.

---

Summary

┌─────┬─────────────────────────────────────────────────────────┬──────────────────────────────────────────┐
│     │                      Proposed fix                       │                 Changes                  │
├─────┼─────────────────────────────────────────────────────────┼──────────────────────────────────────────┤
│ U2  │ Pre-fill from current key (or new type), show           │ license-issuing, PDR-0025 wording        │
│     │ differences                                             │                                          │
├─────┼─────────────────────────────────────────────────────────┼──────────────────────────────────────────┤
│ U3  │ Active types plus own retired type                      │ license-issuing, product-catalogue       │
├─────┼─────────────────────────────────────────────────────────┼──────────────────────────────────────────┤
│ U4  │ Link belongs to the license, editable, any status       │ license-issuing, license-records,        │
│     │                                                         │ PDR-0026                                 │
├─────┼─────────────────────────────────────────────────────────┼──────────────────────────────────────────┤
│ U5  │ Four exclusive statuses                                 │ license-records                          │
├─────┼─────────────────────────────────────────────────────────┼──────────────────────────────────────────┤
│ U6  │ Ask before creating, default no; pointer unchanged      │ issuing-setup                            │
├─────┼─────────────────────────────────────────────────────────┼──────────────────────────────────────────┤
│ U7  │ One copy per data folder                                │ issuing-setup or new requirement,        │
│     │                                                         │ PDR-0023                                 │
├─────┼─────────────────────────────────────────────────────────┼──────────────────────────────────────────┤
│ U8  │ Separate features files                                 │ license-records, PDR-0028                │
└─────┴─────────────────────────────────────────────────────────┴──────────────────────────────────────────┘

U2 has a real trade-off: negotiated features are kept, but type edits reach renewals only when the vendor switches type. Starting there, do you agree with pre-filling from the current key and showing the differences at the summary? Or should type edits flow into renewals?
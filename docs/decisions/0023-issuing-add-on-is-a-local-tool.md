# PDR-0023: The issuing add-on is a local, interactive tool for one vendor

- **Status:** Decided, 2026-10-04. Amended by PDR-0029, 2026-10-04: license rules are the
  library's; issuing conventions such as terms and renewal periods are the add-on's. Amended,
  2026-10-05: one copy per data folder; a database is created in a folder given for one run only
  when the vendor agrees
- **Source:** `openspec/changes/issuing-add-on/design.md` Q1, Q2, Q3; amendment: Q10, Q16, Q17
- **Serves:** vendor (no server to run; guided first use), site owner (keys can be re-sent and
  renewed); **Cost to:** vendor (backs up two folders)

## Decision

The issuing add-on (PDR-0006) is an interactive program one vendor runs on their own machine.
It is driven by menus and prompts, not commands, and keeps its records in a small local
database.

| Aspect | Rule |
|---|---|
| Users | One vendor, one person at a time. No accounts, no sharing over a network |
| One copy per data folder | A second copy started on a data folder already in use refuses to start and says another copy is open. Copies on different data folders may run side by side |
| First run | A guided setup asks for the data folder, the signing keys folder and the vendor's product ID prefix. Nothing else runs until setup is complete |
| Data folder | Chosen by the vendor; defaults to a per-user application folder. Holds the database and the log. Can be moved later, with its records |
| Signing keys folder | Chosen by the vendor. Holds one private key file per signing key. Must not be the data folder or inside it, and the data folder must not be inside it (PDR-0007) |
| Product ID prefix | The vendor part of `vendor.product` (PDR-0017). Pre-fills new product IDs; editable per product |
| Missing data | If the data folder or its database is missing at start, the tool says so and offers to locate it or run setup again. It never silently starts an empty database. A data folder given for one run that holds no database gets one only if the vendor agrees; the default is no |
| Rule checking | Every license rule is the library's. The add-on asks the library and shows the library's reasons |
| Issuing conventions | How the add-on arrives at a key's contents (license types, terms, renewal periods) is the add-on's own. The library checks the result (PDR-0029) |

## Why

- **Local, one vendor.** The vendors PDR-0006 serves have no shop system, so they have no server
  either. A local program needs nothing hosted.
- **Interactive.** A first-time vendor is guided through setup and each issue; a summary is
  confirmed before anything is signed, since a key cannot be withdrawn.
- **Keys folder apart.** PDR-0007: a copy of the data must never be able to forge keys. If the
  folders nest, backing up one copies the other.
- **Never an empty database by accident.** A tool that started fresh when its folder went
  missing (a disconnected drive, a renamed folder) would let a vendor issue new references and
  lose track of existing licenses. A mistyped folder at start is the same risk, so creating a
  database there needs a yes.
- **One copy per data folder.** Two copies on one folder can lose a key's record: one moves the
  data while the other records a key in the old copy, which is then removed. The site owner
  holds a key the records do not know, so it cannot be re-sent or renewed. The same-second and
  unique-reference safeguards (PDR-0009, PDR-0017) also assume one writer.
- **The library decides.** One set of rules, so a key the add-on accepts is a key the library
  issues, and the add-on is a fair example of vendor tooling.
- **License rule or issuing convention.** A license rule decides whether a key's contents are
  acceptable (expiry not before today, product ID format, feature rules). An issuing convention
  is how a vendor arrives at those contents (templates, terms, renewal periods). Conventions
  vary by vendor, so they belong to the vendor's tooling; the library checks what they produce.

## Rejected

| Option | Why rejected |
|---|---|
| Command verbs for scripting | Suits vendors with a shop system, who call the core directly (PDR-0005). Can be added later |
| A hosted web application | Needs hosting, accounts and security for vendors who have none |
| Keys folder anywhere, with a warning | A warning is clicked through once and the exposure is permanent (PDR-0007) |
| Private keys in the database, encrypted | One store from which every product's licenses can be forged once the passphrase leaks (PDR-0007) |
| Several copies on one data folder | Can lose an issued key's record (see Why) |
| Create a database in a one-run folder without asking | A mistyped path starts an empty database |

## Consequences

- The vendor backs up the data folder and the signing keys folder separately. Setup says so.
- Losing a private key file stops issuing for that product until a new signing key is created
  and its public key shipped in a product release (PDR-0007).

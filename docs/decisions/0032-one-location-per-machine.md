# PDR-0032: Data and signing keys live under one location, chosen per machine

- **Status:** Decided, 2026-10-05
- **Source:** `openspec/changes/issuing-add-on/design.md` Q20
- **Serves:** vendor (one setup question; a new machine needs no path fixing), site owner and
  implementor (a machine change never forces a signing key switch that breaks keys on their
  site, PDR-0031); **Cost to:** vendor (must not sync or share the location as a whole)

## Decision

**One location.** Setup asks one question: where to keep the tool's data and signing keys. The
default is `<home>/.<toolname>` (the tool's name is settled with its technology choices). The
vendor may choose any other folder. The tool creates two sibling folders under it, readable only
by the current user where the operating system supports it:

```
  <location>/
     data/           the database and the activity log  (the data folder)
     signing-keys/   one private key file per signing key  (the signing keys folder)
```

```
  default location on each machine
  Windows   C:\Users\carl\.<toolname>
  macOS     /Users/carl/.<toolname>
  Linux     /home/carl/.<toolname>
```

**Saved per machine, never in the database.**

| Setting | Where | How |
|---|---|---|
| Data folder, signing keys folder | A per-user settings file on the machine | A default folder is saved as "default" and resolved against the home directory at each start. A folder the vendor chose is saved as its path |
| Product ID prefix | The database | Travels with the records |
| Private key file of each signing key | The database holds its **file name** only, made from the product ID and signing key ID | Looked up in the signing keys folder in use |

A restored or reused database never brings back another machine's paths.

**Key files checked.** A signing key that can sign (current or pending, PDR-0031) is *usable*
when its file is in the signing keys folder, is readable, and matches the recorded public key.
Retired keys never sign; a missing retired key file is reported but blocks nothing.

| When | If a current or pending key file is not usable |
|---|---|
| Setup, using an existing database | Lists the keys; asks for the signing keys folder again, or continues without them |
| Every start | A notice at the menu naming the products that cannot issue and the folder searched |
| Issuing | Refuses and names the file, then offers, in this order: set the signing keys folder; restore the file from backup and retry; make the pending key current, if one exists; make a new key current now (PDR-0031, last, with its warning) |

Finding, showing, re-sending, inspecting and exporting never need a private key and always work.

**After setup.** The vendor can move the data folder (for example into a synced folder) or
change the signing keys folder. The PDR-0007 rule still applies: neither folder may be the other,
inside it, or contain it. Changing the signing keys folder means "my key files are here now":
the tool moves no private key file, and checks the current and pending key files in the new
folder before accepting it.

**Export signing keys.** A separate backup action copies every signing key's private key file
to a folder the vendor chooses, and lists each file's product, signing key ID and state. It
refuses a target that is the data folder, is inside it, or contains it (PDR-0007), warns that the
files can sign keys for every product, asks before overwriting, and is logged without any key
contents. This is not the records export: the CSV export never holds private keys (PDR-0028).

**Import** is a manual copy of the files into the signing keys folder; the start check finds
them. It is described in the vendor documentation.

**Moving to a new machine**, the basis for vendor documentation:

1. On the old machine, back up the data folder, and the signing keys (copy `signing-keys/` or
   use Export signing keys).
2. On the new machine, install the tool and start it. Setup asks for the location; choose the
   same option as before (usually the default).
3. Put the backed-up `data/` contents and private key files into the new location's `data/` and
   `signing-keys/` folders, or point setup at the folder holding the restored data.
4. Setup finds the existing database, reports how many products and licenses it holds, and
   checks the key files. Issuing works once every current key file is found.

**Backup guidance**, shown at setup:

- Back up the data folder often; it changes with every key issued.
- Back up the signing keys rarely and keep the copy offline; they change only when a signing
  key is created. Losing a private key file stops issuing for that product until a new key is
  made current (PDR-0031).
- Never sync, share or send the whole location: it holds both folders. To keep records in a
  synced folder, move the data folder there.

## Why

- **The keys folder is a fact about the machine, not about the records.** Paths differ between
  machines (drive letter, user name, operating system). A path stored in the database or per
  key is wrong after a restore, every key reports a missing file, and the only remedy left is a
  signing key switch that breaks keys on sites without the new release (PDR-0031).
- **Home-relative default.** `<home>/.<toolname>` resolves on every operating system, follows the
  convention vendors know from `~/.ssh`, and needs no answer from a first-time vendor.
- **One location, two folders.** One place to find, document and restore, while the data folder
  and signing keys folder stay apart (PDR-0007): backing up the data folder never copies a
  private key.
- **File names, not paths.** The tool names the files itself, so the signing keys folder is
  enough to find them on any machine.
- **Checked early, safe remedies first.** A missing file is almost always a path or restore
  problem, not a lost key. Reporting it at start avoids finding out at a sale; offering the
  folder and backup first keeps the signing key switch for when it is the only option.
- **Export signing keys.** Dot folders are hidden in file managers and most file dialogs. A
  vendor who has never opened one needs a visible copy to back up.

## Rejected

| Option | Why rejected |
|---|---|
| A separate prompt for each folder at setup, keys folder with no default | Two path questions for a first-time vendor; the safety it adds is kept by the sibling layout and the PDR-0007 checks |
| Default in the operating system's per-user application data folder | A different path on each operating system to document; hidden in different ways |
| Default next to the installed program | The tool's program folder is replaced on update and removed on uninstall; records and private keys would be lost |
| Data and keys in one folder, or keys in the database | Rejected by PDR-0007 |
| Absolute path recorded per signing key, with a "locate this file" action | One prompt per key per product on every machine change, found only when issuing |
| Search the folder for a file matching each public key | Reads every file on every lookup; unnecessary when the tool names the files |
| Signing keys folder stored in the database | A restored database brings back the old machine's path |
| Import signing keys action | A file copy plus the start check does the same; can be added later |

## Consequences

- PDR-0023: setup asks for one location instead of two folders; both folders are machine
  settings. The other PDR-0023 rules (one copy per data folder, never an empty database without
  the vendor agreeing, PDR-0007 separation) are unchanged.
- PDR-0028: private keys are never in the records export; Export signing keys is a separate,
  deliberate backup of the key files.
- A data folder given for one run uses the machine's signing keys folder.
- A vendor who syncs the whole location to cloud storage exposes the private keys. The backup
  guidance says not to; the tool cannot prevent it.

# 0003: Issuing Add-on as a .NET Tool with Spectre.Console

## Status

Decided, 2026-10-04. Source: `openspec/changes/issuing-add-on/design.md`, Decisions.

## Context

The issuing add-on (PDR-0006, PDR-0023) is an interactive, local program for one vendor: menus,
prompts, a first-run wizard, tables of licenses, a confirmation summary before signing. It must
run on Windows, macOS and Linux, be installable without cloning the repo, and depend on the core
library, which stays BCL-only.

Personas: **vendor** only. Site owners and implementors never run it.

## Decision

- A .NET 10 console application, `src/Umbraco.Community.Licensing.Issuing`, packed as a .NET
  tool (`PackAsTool`), package ID `Umbraco.Community.Licensing.Issuing`, command
  `umbraco-license-issuer`. Installed with `dotnet tool install -g`.
- **Spectre.Console** for prompts, selection lists, tables and the confirmation panels.
- Startup options parsed by hand (`--data <folder>`, `--verbose`, `--help`); there are three.
- Dependency injection with `Microsoft.Extensions.DependencyInjection` on a plain
  `ServiceCollection`; no generic host.
- Layering: UI (menus and prompts) calls application services; services hold all behaviour and
  are tested without a console. Services take `TimeProvider`.
- The project references the core library by project reference; the core never references it.

## Alternatives Considered

- **Plain `Console.ReadLine`:** no dependency, but selection lists, validation loops, masked
  reveal and tables would be hand-built and worse. Rejected.
- **Terminal.Gui (full-screen TUI):** richer, but heavier, harder to test and to copy from as an
  example. Rejected.
- **System.CommandLine verbs:** suits scripting, which PDR-0023 rejects for now. Can be added
  later over the same services.
- **Generic host (`Microsoft.Extensions.Hosting`):** brings configuration and logging wiring,
  but also a lifetime model meant for services. Three options and two folders do not need it.
- **Self-contained single-file executables:** no .NET install needed, but one build per platform
  to publish. The vendor already has the SDK to build its product. Rejected for now.

## Consequences

- A vendor needs the .NET 10 runtime.
- Spectre.Console is the only UI dependency; services are UI-free, so a later web or command
  front end reuses them.
- Tests drive services, plus a thin layer of Spectre.Console `TestConsole` tests for the setup
  wizard.

## Reversal Cost

Low. The UI is a thin layer over services; replacing Spectre.Console or adding command verbs
touches only the UI layer.

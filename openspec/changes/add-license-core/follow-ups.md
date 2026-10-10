# Follow-ups: add-license-core

Minors and nits from reviews. Read at the start of each apply session.

| Source | Severity | Item | Status |
|---|---|---|---|
| Review of tasks.md 1.1–1.3 | nit | `tests/Umbraco.Community.Licensing.Core.Tests/Support/SupportTests.cs` holds two test classes; split one file per class | Open |
| Review of tasks.md 1.1–1.3 | nit | `PackageTags` in the library csproj not asked for by ADR-0003 (ADR-0003 lists tags in package metadata; harmless) | Open |
| Review of tasks.md 1.1–1.3 | note | `AssemblyMarker` (internal) exists only to give the empty library a type; remove once real types exist and point the smoke test at one | Open |
| Review of tasks.md 1.1–1.3 | note | Tests run on VSTest (`IsTestingPlatformApplication=false`); Microsoft.Testing.Platform needs a `global.json` opt-in. Carl to decide | Open |
| Supervisor, section 1 | minor | ADR-0003 requires a package README; no task wires `PackageReadmeFile` to `README.md` (`dotnet pack` warns). Fix via `opsx:update`: extend task 8.2 or add a task | Open |
| Supervisor, section 1 | minor | `tests/.../Support/CultureScope.cs`: add a doc remark that the scope must be created at the top of the test method (culture does not flow out of an awaited helper). Fold into a section 2 or 6 block | Open |
| Supervisor, section 1 | note | Package `Version`, `Authors`, `RepositoryUrl` unset (defaults to 1.0.0). Decide before first publish | Open |
| Supervisor, section 1 | note | Task 6.6 timings: use `Stopwatch`, not `TimeProvider` (`FixedTimeProvider` does not override `GetTimestamp`) | Open |

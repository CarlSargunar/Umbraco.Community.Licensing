# Follow-ups: add-license-core

Minors and nits from reviews. Read at the start of each apply session.

| Source | Severity | Item | Status |
|---|---|---|---|
| Review of tasks.md 1.1–1.3 | nit | `tests/Umbraco.Community.Licensing.Core.Tests/Support/SupportTests.cs` holds two test classes; split one file per class | Done (block 2.3–2.4) |
| Review of tasks.md 1.1–1.3 | nit | `PackageTags` in the library csproj not asked for by ADR-0003 (ADR-0003 lists tags in package metadata; harmless) | Done: conforms, ADR-0003 lists tags |
| Review of tasks.md 1.1–1.3 | note | `AssemblyMarker` (internal) exists only to give the empty library a type; remove once real types exist and point the smoke test at one | Done (section 2 cleanup) |
| Review of tasks.md 1.1–1.3 | note | Tests run on VSTest (`IsTestingPlatformApplication=false`); Microsoft.Testing.Platform needs a `global.json` opt-in. Carl to decide | Done: tasks.md 1.4 (ADR-0006) |
| Supervisor, section 1 | minor | ADR-0003 requires a package README; no task wires `PackageReadmeFile` to `README.md` (`dotnet pack` warns). Fix via `opsx:update`: extend task 8.2 or add a task | Done: tasks.md 1.5, 8.2 (ADR-0006) |
| Supervisor, section 1 | minor | `tests/.../Support/CultureScope.cs`: add a doc remark that the scope must be created at the top of the test method (culture does not flow out of an awaited helper). Fold into a section 2 or 6 block | Done (block 2.3–2.4) |
| Supervisor, section 1 | note | Package `Version`, `Authors`, `RepositoryUrl` unset (defaults to 1.0.0). Decide before first publish | Done: tasks.md 1.5 (ADR-0006, PDR-0024) |
| Supervisor, section 1 | note | Task 6.6 timings: use `Stopwatch`, not `TimeProvider` (`FixedTimeProvider` does not override `GetTimestamp`) | Open |
| Review of tasks.md 2.1–2.2 | minor | `tests/.../Format/FormatTests.cs` `Seam_FixesValues` asserts only a hand-written fake, so it cannot fail; replace with a test that issuing uses `IRandomSource` (fold into task 5.2 or 7.1) | Open |
| Review of tasks.md 2.1–2.2 | nit | `FormatTests.cs` `Generated_ValuesDiffer` has a ~31^-10 false-failure chance; accepted | Open |
| Review of tasks.md 2.3–2.4 | nit | `src/.../Format/ContentRules.cs` redundant `surrogateError` declaration before the `out` assignment; `EnumerateScalars` allocates a `List<Rune>` per check where a count-and-flag loop would do | Done (section 2 cleanup) |
| Review of tasks.md 2.3–2.4 | note | Task 5.1: map "not given" display name and vendor tag to `null`, never `""`; a default `ExpiryStatement` means no expiry stated | Open |
| Worker, tasks.md 2.3 | question | `NumberRule.IsCanonical` accepts `2.50` on read per ADR-0002 grammar, while issue always writes `2.5`. Carl to decide: keep, or tighten read to canonical form (ADR-0002 amendment via `opsx:update`) | Done: kept, ADR-0002 and design.md Q21 |
| Supervisor, section 2 | minor | `tests/.../Support/FixedTimeProviderTests.cs:1-4`: two leading blank lines, none after `namespace`; `dotnet format whitespace --verify-no-changes` fails. Fold into next block | Done (section 2 cleanup) |
| Supervisor, section 2 | minor | design.md Components says "Format → nothing public", but `ContentRules`, `ExpiryRule`, `ReferenceParser` return public `LicenseProblem`. Amend design.md via `opsx:update` to allow Format to use the `LicenseProblem` value record | Done: design.md amended |
| Supervisor, section 2 | minor | `src/.../Format/ContentRules.cs:142`: invalid feature name problem does not name the feature; include it (truncated, "(none)" for null/empty), field stays `features`. Fold into a section 5 block | Open |
| Supervisor, section 2 | minor | Remove `AssemblyMarker`; point `AssemblySmokeTests` at `typeof(LicenseProblem)`. Fold into next block | Done (section 2 cleanup) |
| Supervisor, section 2 | nit | `src/.../Format/KeyPrefixRule.cs:3` summary says null means no prefix, but `IsValid(null)` is false; reword | Done (section 2 cleanup) |
| Supervisor, section 2 | nit | `tests/.../Format/ContentRulesTests.cs:25,124`: `Kelvin` field unused; case `"aKb"` holds an invisible U+212A; use `"a" + Kelvin + "b"` | Done (section 2 cleanup) |
| Supervisor, section 2 | nit | `src/.../Format/ReferenceParser.cs:20`, `KeyIdentifierFormat.DisplayReference`: remark that "no prefix" is `null`, never `""` (`KeyPrefix.None.Value` is `""` per ADR-0004) | Done (section 2 cleanup) |
| Supervisor, section 2 | note | Task 5.1: map `KeyPrefix.None` to `null` before calling Format | Open |
| Supervisor, section 2 | note | Task 4.3: escaped lone surrogate (`"\uD800"`) can make `Utf8JsonReader.GetString()` throw; strict reader must map it to *not supported* without the outer guard; add a test | Open |
| Supervisor, section 2 | note | Tasks 5.4, 6.1: add one Format helper to extract the 10-character reference from an identifier, rather than slicing in Issuing and Evaluation | Open |
| Supervisor, section 2 | note | Task 6.5: keep order whitespace removal → identifier pattern → length cap | Open |
| Review of section 2 cleanup | nit | `src/.../Format/ContentRules.cs`: `hasSurrogateError` local could inline to `if (!TryScan(...))` | Open |
| Supervisor, section 1 (re-review) | nit | `src/.../Umbraco.Community.Licensing.Core.csproj:19`: README pack item uses backslashes (`..\..\README.md`, `PackagePath="\"`); works on macOS, optional switch to forward slashes | Open |
| Supervisor, section 1 (re-review) | note | Do not publish to NuGet before task 8.2: the packed README has no beta warning yet (PDR-0024) | Open |
| Supervisor, section 1 (re-review) | note | `Version` is hard-coded; add a release checklist item or CI-supplied version when CI is added (ADR-0006) | Open |
| Supervisor, section 1 (re-review) | note | nuspec records HEAD commit; pack only from a clean commit | Open |
| Supervisor, section 1 (re-review) | note | CI must support Microsoft.Testing.Platform and call `dotnet test --solution` (ADR-0006) | Open |
| Review of tasks.md 3.1–3.3 | minor | `tests/.../Signing/SigningKeyTests.cs`: worker deviations untested: `SigningPublicKey.Parse` whitespace trim (leading/trailing newline on a valid export) and `FromSpki` rejection of trailing bytes / non-canonical SPKI (`SigningPublicKey.cs:105-115`); add tests | Done (section 3 rework) |
| Review of tasks.md 3.1–3.3 | minor | `src/.../Signing/SigningPublicKey.cs:54`: `Base64Url.DecodeFromChars` may accept embedded whitespace or non-canonical trailing bits, so several texts import to one key (no security impact; SPKI re-export check holds). Accept or pin by test | Open |
| Review of tasks.md 3.1–3.3 | minor | `src/.../Signing/SigningPrivateKey.cs:34`: `PemEncoding.TryFind` takes the first PEM block and ignores trailing text; document or reject | Open |
| Review of tasks.md 3.1–3.3 | minor | `src/.../Signing/SigningPublicKey.cs:104-106`: P-256 check via `Curve.Oid.Value` or friendly name `nistP256`; unverified on Windows (CNG). Confirm when CI runs Windows | Done: byte-prefix check, OS-independent (section 3 rework) |
| Review of tasks.md 3.1–3.3 | nit | No concurrency test for `TrustedSigningKeys.Verify`; add a parallel verify test (fold into 6.5 or 6.6) | Open |
| Review of tasks.md 3.1–3.3 | nit | `src/.../Signing/SigningPrivateKey.cs:60-64`: `catch (FormatException)` dispose-and-rethrow could merge into one `catch` with `throw` | Open |
| Supervisor, section 3 | major | Non-`FormatException` escapes from `SigningPublicKey.Parse` / `SigningPrivateKey.FromPem` on explicit-curve keys | Done (section 3 rework) |
| Supervisor, section 3 | minor | `SigningPrivateKey.FromPem`: decoded PKCS#8 bytes not zeroed | Done (section 3 rework) |
| Supervisor, section 3 | nit | `TrustedSigningKeys.Verify` threw on null ID | Done (section 3 rework) |
| Supervisor, section 3 | note | Task 6.6: Windows CNG per-call `ECDsa` import cost unmeasured (macOS arm64: 0.12 ms import+verify); log platform. Off-curve rejection to confirm when CI runs Windows/Linux | Open |
| Re-review of section 3 rework | nit | `src/.../Signing/SigningPublicKey.cs` `FromSpki`: `read != length` and canonical re-export comparison effectively unreachable after the 91-byte prefix guard; keep or drop | Open |
| Re-review of section 3 rework | nit | `src/.../Signing/SigningPrivateKey.cs` `FromPem`: `ArgumentException` catch is broad (limited to the import call) | Open |

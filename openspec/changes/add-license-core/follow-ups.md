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
| Review of tasks.md 2.1–2.2 | minor | `tests/.../Format/FormatTests.cs` `Seam_FixesValues` asserts only a hand-written fake, so it cannot fail; replace with a test that issuing uses `IRandomSource` (fold into task 5.2 or 7.1) | Done (block 5.2) |
| Review of tasks.md 2.1–2.2 | nit | `FormatTests.cs` `Generated_ValuesDiffer` has a ~31^-10 false-failure chance; accepted | Open |
| Review of tasks.md 2.3–2.4 | nit | `src/.../Format/ContentRules.cs` redundant `surrogateError` declaration before the `out` assignment; `EnumerateScalars` allocates a `List<Rune>` per check where a count-and-flag loop would do | Done (section 2 cleanup) |
| Review of tasks.md 2.3–2.4 | note | Task 5.1: map "not given" display name and vendor tag to `null`, never `""`; a default `ExpiryStatement` means no expiry stated | Done (block 5.1) |
| Worker, tasks.md 2.3 | question | `NumberRule.IsCanonical` accepts `2.50` on read per ADR-0002 grammar, while issue always writes `2.5`. Carl to decide: keep, or tighten read to canonical form (ADR-0002 amendment via `opsx:update`) | Done: kept, ADR-0002 and design.md Q21 |
| Supervisor, section 2 | minor | `tests/.../Support/FixedTimeProviderTests.cs:1-4`: two leading blank lines, none after `namespace`; `dotnet format whitespace --verify-no-changes` fails. Fold into next block | Done (section 2 cleanup) |
| Supervisor, section 2 | minor | design.md Components says "Format → nothing public", but `ContentRules`, `ExpiryRule`, `ReferenceParser` return public `LicenseProblem`. Amend design.md via `opsx:update` to allow Format to use the `LicenseProblem` value record | Done: design.md amended |
| Supervisor, section 2 | minor | `src/.../Format/ContentRules.cs:142`: invalid feature name problem does not name the feature; include it (truncated, "(none)" for null/empty), field stays `features`. Fold into a section 5 block | Done (block 5.2) |
| Supervisor, section 2 | minor | Remove `AssemblyMarker`; point `AssemblySmokeTests` at `typeof(LicenseProblem)`. Fold into next block | Done (section 2 cleanup) |
| Supervisor, section 2 | nit | `src/.../Format/KeyPrefixRule.cs:3` summary says null means no prefix, but `IsValid(null)` is false; reword | Done (section 2 cleanup) |
| Supervisor, section 2 | nit | `tests/.../Format/ContentRulesTests.cs:25,124`: `Kelvin` field unused; case `"aKb"` holds an invisible U+212A; use `"a" + Kelvin + "b"` | Done (section 2 cleanup) |
| Supervisor, section 2 | nit | `src/.../Format/ReferenceParser.cs:20`, `KeyIdentifierFormat.DisplayReference`: remark that "no prefix" is `null`, never `""` (`KeyPrefix.None.Value` is `""` per ADR-0004) | Done (section 2 cleanup) |
| Supervisor, section 2 | note | Task 5.1: map `KeyPrefix.None` to `null` before calling Format | Done: `KeyPrefix.ToFormatPrefix` (block 5.1) |
| Supervisor, section 2 | note | Task 4.3: escaped lone surrogate (`"\uD800"`) can make `Utf8JsonReader.GetString()` throw; strict reader must map it to *not supported* without the outer guard; add a test | Done: `PayloadJson` pre-scan (block 4.2–4.3) |
| Supervisor, section 2 | note | Tasks 5.4, 6.1: add one Format helper to extract the 10-character reference from an identifier, rather than slicing in Issuing and Evaluation | Done: `KeyIdentifierFormat.ReferenceOf` (block 4.4) |
| Supervisor, section 2 | note | Task 6.5: keep order whitespace removal → identifier pattern → length cap | Done in `KeyEnvelope.Read` (block 4.4); 6.5 to keep it |
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
| Review of tasks.md 4.1 | nit | `tests/.../Format/PayloadWriterTests.cs:134-139`: `Write_OffSwitchOrMissingText_Throws` holds two cases in one `[Fact]`; split or `[Theory]` | Open |
| Review of tasks.md 4.1 | nit | `src/.../Format/PayloadWriter.cs:28,31`: null `SigningKeyId`/`Product` throws `ArgumentNullException` from `WriteString`, not `InvalidOperationException`; doc covers names/text only, issuer validates first | Open |
| Worker, tasks.md 4.4 | nit | `tests/.../Format/KeyIdentifierFormatTests.cs` holds class `KeyIdentifierReferenceOfTests` (name clash with `KeyIdentifierFormatTests` in `FormatTests.cs`); merge into `FormatTests.cs` or rename the file | Open |
| Review of tasks.md 4.4 | nit | `src/.../Format/KeyEnvelope.cs:106`: `value.Any(char.IsWhiteSpace)` via LINQ; acceptable | Open |
| Review of tasks.md 4.2–4.3 | minor | `tests/.../Format/PayloadRoutingTests.cs`: routing `MaxDepth = 16` boundary untested; add depth 16 reads, 17 unreadable | Open |
| Review of tasks.md 4.2–4.3 | minor | `tests/.../Format/PayloadRoutingTests.cs`: no test for a lone surrogate escape in an ignored field (e.g. `{"\uD800":1,"signingKeyId":"k","product":"a.b"}` → true) | Open |
| Review of tasks.md 4.2–4.3 | minor | `src/.../Format/PayloadReader.cs`: `signingKeyId` length 11 checked on read but not by `ContentRules`/`PayloadWriter`; task 5.2 issuer must take the ID from the signing key, so issue and read cannot disagree | Done (block 5.2) |
| Review of tasks.md 4.2–4.3 | nit | `src/.../Format/PayloadReader.cs`: `Advance`/`AtEnd` near-duplicates | Open |
| Review of tasks.md 4.2–4.3 | note | Task 6.1: `PayloadReader` failure text embeds payload-controlled names; evaluator maps failure to *not supported* and must not surface the text as a fact | Open |
| Worker, tasks.md 4.3 | question | `expires` earlier than `issued` is accepted on read (no rule covers it; issuer cannot produce it). Carl to decide: keep (note in ADR-0002) or refuse (PDR + ADR-0002 amendment) | Done: accept, ADR-0002 and design.md Q25 (Carl, 2026-10-12) |
| Supervisor, section 4 | minor | `src/.../Format/KeyEnvelope.cs:42-50`: `Compose` accepts any identifier (lowercase, 17-char prefix, non-ASCII silently `?`); throw `InvalidOperationException` when `IdentifierPattern` does not match, add a test. Fold into the 5.2 block | Done (block 5.2) |
| Supervisor, section 4 | nit | `src/.../Format/KeyEnvelope.cs:66-68`: `text[..firstDot]` allocated before the pattern check; match on a span or refuse `firstDot > 33` first. Fold into 6.5 or 6.6 | Open |
| Supervisor, section 4 | nit | `src/.../Format/PayloadReader.cs:272-299`: repeat set and name check duplicate `ContentRules.CheckFeatures`; keep, or comment it is deliberate | Open |
| Supervisor, section 4 | note | Task 6.1: use the envelope's *missing* state (no second whitespace check); claims = envelope identifier + routing product only; copy `LicensePayload.Features` into an immutable collection for `VerifiedLicense`; reference fact via `ReferenceOf` | Open |
| Supervisor, section 4 | note | Tasks 6.1 / 7.2: no test yet runs the whole chain (writer → compose → envelope → routing → verify → strict read); ensure at least one end-to-end test | Open |
| Supervisor, section 4 | note | Task 6.6: if the 1 ms budget is missed, cheap wins are the re-encode canonical check (test unused trailing bits instead) and building `features.<name>` in `ContentRules.CheckFeatureValue` only on a problem | Open |
| Supervisor, section 4 | note | Task 6.5: routing and strict read differ on a lone surrogate escape in an ignored property name (unreadable vs *not supported*); neither throws nor is valid | Open |
| Review of tasks.md 5.1 | minor | `tests/.../Issuing/*.cs`: snake_case test names (`Default_is_LIC`); project style is `Subject_Behaviour` PascalCase, scenario names kept. Rename (Architect: fold into block 5.2) | Done (block 5.2) |
| Review of tasks.md 5.1 | minor | `src/.../Issuing/FeatureList.cs:84`: `ToInput` throws `InvalidOperationException` on a null `LicenseFeature.Value` (non-nullable, programming error); task 5.2 to document or tolerate | Done (block 5.2) |
| Review of tasks.md 5.1 | nit | `tests/.../Issuing/FeatureListTests.cs:50`: invisible literal U+212A; use `"K"` | Done (block 5.2) |
| Review of tasks.md 5.1 | nit | `tests/.../Issuing/FeatureListTests.cs:169`: redundant parentheses | Done (block 5.2) |
| Review of tasks.md 5.1 | note | ADR-0004 "Issuing" sketch lacks `KeyPrefix.ToString()` (returns `Value`) and `FeatureList.Of` `ArgumentException` on a null entry; add via `opsx:update` before archive | Open |
| Review of tasks.md 5.2 | major | Clock read twice (`ExpiryRule` and issue time): key could be issued with `expires` < `issued` | Done (block 5.2 rework): one read; `ExpiryRule.TryResolve` takes the instant |
| Review of tasks.md 5.2 | minor | `LicenseIssuer.cs:39`: wrong `ParamName` for null `Features` | Done (block 5.2 rework) |
| Worker, tasks.md 5.2 | note | Task 6.1: read the clock once per evaluation and pass the instant (`ExpiryRule` now takes `DateTimeOffset now`) | Open |
| Review of tasks.md 5.3 | minor | `tests/.../Issuing/LicenseIssuerTests.cs`: spec scenario "Invalid text" (under "Optional feature definitions") covered only by `ContentRulesTests`; add `Definitions_InvalidText_NamesTheFeature` through `Issue` with a text definition. Fold into block 5.4 | Done (block 5.4) |
| Review of tasks.md 5.3 | nit | `LicenseIssuerTests.cs`: `Definitions_Null_DoesNotRejectInvalidRequestsDifferently` vague name; scenario names not mirrored one for one in `FeatureDefinitionsTests` | Done: renamed (block 5.4); scenario mirroring accepted |
| Review of tasks.md 5.4 | minor | tasks.md 6.2 says "no segment of the key string appears in `ToString`", but spec "No key text in results" allows the identifier; `VerifiedLicense` default `ToString` prints `KeyIdentifier`. Reword 6.2 via `opsx:update` to match the spec (identifier allowed) before section 6 | Done: opsx:update 2026-10-12, tasks.md 6.2 reworded |
| Review of tasks.md 5.4 | nit | `Issue(null, key)` ambiguous between the two ADR-0004 overloads; optional README note (task 8.2) | Open |
| Review of tasks.md 5.4 | nit | Malformed reissue reference message uses the fixed example `LIC-8F3AK-M7RXB`, not the issuer's prefix; accepted | Open |
| Supervisor, section 5 | major | Issue can produce a key longer than the 32,767 read cap: default `Utf8JsonWriter` encoder escapes non-ASCII as `\uXXXX` (6 bytes, 12 outside the BMP) and `" & ' + < >`; ADR-0002 and PDR-0017 worst case assumed ASCII. ~15 features of 256 non-ASCII characters pass every issue rule but read as *unreadable*. Product decision for Carl (recommended: reject at issue when the composed key exceeds 32,767, naming `features`); then `opsx:update` (PDR-0017 amendment, spec scenario, ADR-0002 arithmetic, task) and a worst-case test. Section 5 not approved until fixed | Done: opsx:update 2026-10-12: PDR-0025, ADR-0007, design.md Q26, tasks.md 4.5 and 5.5 |
| Supervisor, section 5 | minor | `design.md:80` dependency direction omits Issuing ↔ Evaluation shared values (`ReissueRequest.From(VerifiedLicense)`, `VerifiedLicense.Features` is `FeatureList`); fix in the same `opsx:update` | Done: opsx:update 2026-10-12, design.md dependency direction |
| Supervisor, section 5 | nit | `FeatureList` has no `Equals`/`ToString`: equal `VerifiedLicense`/request records compare unequal, `ToString` shows the type name. Add sequence equality and short `ToString`, or accept. Section 6 block | Open |
| Supervisor, section 5 | nit | Task 6.1: add internal `FeatureList.FromInputs` next to `ToInputs` for the reverse mapping | Open |
| Supervisor, section 5 | note | Task 7.1 brief: `Compose` now throws on a non-matching identifier, so the `...-7Q2O` and relabelled `BETA-` variants must be made by editing the signed string. Task 8.2: `VerifiedLicense` has a public constructor; README must not claim only the evaluator can produce one. Task 6.6: escaping makes non-ASCII-heavy keys 3–6× longer | Open |

# ADR-0008: CI host, runners and package publishing

- Status: Decided
- Date: 2026-10-14
- Source: `add-release-pipeline` design.md Q3, Q4, Q6, Q9. Amends ADR-0006 (repository URL).

## Context

The repository is public on GitHub, under Carl's personal account `CarlSargunar`, and moves
to Carl's organisation `carlcod-es` before the first release (Carl, 2026-10-14). PDR-0027 publishes from a `release-yyyymmdd` tag on `main`;
PDR-0028 tests each release once on whichever test machine is available, and the full suite on
Windows, Linux and macOS once before 1.0. Pull requests may come from anyone. Tests run on
Microsoft.Testing.Platform and need `dotnet test --solution` (ADR-0006).

## Decision

- **Repository location**: `https://github.com/carlcod-es/Umbraco.Community.Licensing`. Carl
  transfers the repository before any runner, policy or variable below is set up, and before the
  first release, so the first package already names the final URL. The library project's
  `RepositoryUrl` and the README's absolute links use this URL (replaces ADR-0006's
  `CarlSargunar` URL).
- **CI host**: GitHub Actions. Three workflows under `.github/workflows/`:

  | Workflow | Trigger | Runner | Does |
  |---|---|---|---|
  | `ci.yml` | `pull_request` to `main`, `push` to `main` | GitHub-hosted `ubuntu-latest` | Tool tests, build, test |
  | `release.yml` | `push` of tags `release-*`; `workflow_dispatch` (rehearsal) | Self-hosted, labels `self-hosted`, `licensing-release` | Checks, tool tests, build, test, pack, publish (tag only) |
  | `all-platforms.yml` | `workflow_dispatch` | GitHub-hosted matrix `windows-latest`, `ubuntu-latest`, `macos-latest`, `fail-fast: false` | Build, test; publishes nothing |

- **Runners** (Carl, 2026-10-14): pull requests and the all-platform check on GitHub-hosted
  runners; releases on Carl's own machines registered as self-hosted runners with the label
  `licensing-release`. Any online machine with the label takes the job ("whichever test machine
  is available", PDR-0028). Every workflow prints the runner's operating system, version and
  architecture before testing.
- **Pull request trust**: fork pull requests run on GitHub-hosted runners with a read-only token
  and no secrets. The repository setting "Require approval for all external contributors" is on,
  because a fork pull request runs its own copy of the workflow files and could retarget a job
  to the self-hosted label. Carl reads any workflow change in a pull request before approving
  its run.
- **Publishing**: nuget.org Trusted Publishing (Carl, 2026-10-14). `release.yml` has
  `permissions: { contents: read, id-token: write }`; the `NuGet/login` action exchanges the
  run's OIDC token for a short-lived API key, used once by `dotnet nuget push` to
  `https://api.nuget.org/v3/index.json`. The nuget.org profile name is the repository variable
  `NUGET_USER`. The nuget.org policy names owner `carlcod-es`, repository
  `Umbraco.Community.Licensing`, workflow `release.yml`. No `--skip-duplicate`: a duplicate
  must fail the run.
- **Release job order**: checkout with full history and tags; release checks (PDR-0027 refusals
  1, 3, 4, PDR-0029, and "on `main`" by `git merge-base --is-ancestor` against `origin/main`);
  tool tests; `dotnet build -c Release -p:ContinuousIntegrationBuild=true`;
  `dotnet test --solution ... -c Release --no-build`; `dotnet pack -c Release --no-build`;
  login and push. Checks and push run only on a tag push; a `workflow_dispatch` rehearsal builds,
  tests and packs on a release machine and publishes nothing. One job, so the package pushed is
  the one tested. `concurrency: release`, not cancelled in progress.
- **Toolchain**: `actions/setup-dotnet` with `global-json-file: global.json`; `actions/setup-node`
  at Node 22. Run steps are single `dotnet` or `node` commands, so they work in the default shell
  on every runner operating system.
- **Action dependencies**: `actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`,
  `NuGet/login`. Each pinned to a full commit SHA with its version in a comment; latest stable
  release at least two weeks old at apply time. Approved by Carl with this change.
- **Workflow permissions**: `contents: read` everywhere; `id-token: write` only in `release.yml`.

## Alternatives Considered

| Option | Why not |
|---|---|
| GitHub-hosted runners for releases too | Recommended; Carl chose his own machines for releases |
| Self-hosted runners for pull requests | Fork pull requests would run code on Carl's machines |
| Azure Pipelines | A second service and project for a GitHub repository; Carl's other repositories use tag-triggered GitHub Actions |
| Publish from Carl's machine with a local script | No clean checkout guaranteed; the API key would live on the machine |
| nuget.org API key as a repository secret | A long-lived secret to store and rotate (max 365 days) |
| `--skip-duplicate` on push | Hides a republish of a released version as success |
| Separate build and publish jobs | The package would cross jobs as an artifact; one job keeps tested and pushed the same file |
| All-platform matrix on every release | PDR-0028 chose one test run per release |
| Leave the all-platform check to the 1.0 change | Carl chose to build it now; it also closes the `add-license-core` Windows/Linux follow-ups |
| Actions pinned by tag | A moved tag changes the code the release runs |
| Keep the repository under `CarlSargunar` | Carl chose his organisation |
| Move after the first release | The first package would name the old URL, kept only by GitHub's redirect |

## Consequences

- A release needs one of Carl's runner machines online. A queued job waits and GitHub fails it
  after 24 hours. Re-running a release run that never reached its tests is allowed (same tag,
  same version); a run that failed its tests is a failed release (PDR-0027), and the next release
  takes a new number.
- Self-hosted runners keep state between jobs (tool cache, NuGet cache); `actions/checkout` cleans
  the working copy.
- Trusted Publishing policies on nuget.org are set by Carl outside the repository.
  Renaming `release.yml` breaks publishing until the policy is updated.
- GitHub redirects the old URL after the transfer; local clones switch their remote to the new
  URL. Runners, settings and variables are set up after the transfer, on the organisation's
  repository.
- SourceLink in the .NET SDK records the repository and commit in the package;
  `ContinuousIntegrationBuild` makes paths deterministic.

## Reversal Cost

Low: workflow files and repository settings. Moving releases to hosted runners is a one-line
`runs-on` change.

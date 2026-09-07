# 0002: Split Azure Key Vault Sourcing into a Separate Package

## Status

Accepted

## Context

The `license-key-management` change (see `openspec/changes/license-key-management/`) requires supporting three ways to source the raw license key string: .NET configuration, environment variables, and Azure Key Vault. The `license-key-sourcing` spec explicitly requires that a host application not using the Key Vault provider must not be forced to take a dependency on Azure Key Vault client libraries (`Azure.Security.KeyVault.Secrets`, `Azure.Identity`).

## Decision

Ship the license-key-sourcing capability as at least two packages:

- A core package containing license generation, license validation, and the .NET configuration and environment-variable sourcing providers — BCL-only dependencies.
- A separate package (e.g. `*.AzureKeyVault`) containing only the Key Vault sourcing provider, depending on the Azure SDK packages it needs.

## Alternatives Considered

- **Single package with all providers, Azure SDK as a hard dependency:** Simpler to publish and version, but forces every consumer — including ones that only ever read from `appsettings.json` or an environment variable — to pull in the Azure Identity/Key Vault SDKs transitively. Rejected: violates the sourcing spec's dependency-isolation requirement.
- **Single package with Azure SDK as an optional/lazy-loaded dependency (reflection-based):** Avoids a hard reference but adds runtime complexity (reflection, assembly-load fallback handling) to work around a problem NuGet's package-reference model already solves cleanly. Rejected as unnecessary complexity.

## Consequences

- Two packages to version and release together for a given feature set, rather than one. Version skew between them needs a compatibility policy (e.g., the Key Vault package targets a compatible core package version range) — to be defined during implementation, not blocking this decision.
- Consumers who need Key Vault sourcing take one extra package reference, which is the expected and minimal cost for that capability.
- Consumers who don't need it get a lean dependency footprint, satisfying the sourcing spec directly.

## Reversal Cost

Low to moderate: providers could be merged back into a single package later without breaking the public API shape (the provider abstraction stays the same either way), though it would reintroduce the transitive Azure SDK dependency for all consumers, which is the exact outcome this decision avoids.

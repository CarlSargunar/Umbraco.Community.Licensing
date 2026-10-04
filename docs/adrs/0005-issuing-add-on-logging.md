# 0005: Issuing Add-on Logging with Microsoft.Extensions.Logging and Serilog

## Status

Decided, 2026-10-04. Source: `openspec/changes/issuing-add-on/design.md`, Decisions.

## Context

The add-on logs every action to a daily file in the data folder, kept 30 days, optionally echoed
to the console (`activity-logging` spec). Key strings and private keys must never reach the log
(PDR-0028).

## Decision

- Services log through `Microsoft.Extensions.Logging` (`ILogger<T>`), with message templates
  and named properties.
- Provider: Serilog via `Serilog.Extensions.Logging`, sinks `Serilog.Sinks.File`
  (`logs/issuing-.log`, `rollingInterval: Day`, `retainedFileCountLimit: 30`, UTC timestamps)
  and, with `--verbose`, `Serilog.Sinks.Console`.
- The log is opened only once the data folder is known; setup before that logs nothing to file.
- **Secrets kept out by construction:** no service logs a key string, `IssuedLicenseKey`,
  `SigningPrivateKey` or PEM. Log calls pass identifiers only. Exceptions from the library carry
  no key material (`LicenseRequestException` lists rule problems). A test issues, reveals,
  inspects and exports, then scans the log for the key string's payload and signature segments.
- EF Core's own logging is filtered to `Warning`, so SQL parameter values (key strings) are
  never logged; `EnableSensitiveDataLogging` is never turned on.

## Alternatives Considered

- **A hand-written file `ILoggerProvider`:** no dependency, but daily rolling and retention would
  be ours to get right. Rejected.
- **NLog:** equivalent. Serilog's file sink configures in code with no XML. Chosen on simplicity.
- **A redacting enricher that scrubs key-shaped strings:** a second line of defence, but pattern
  matching gives false confidence and costs every log call. The test is the guard instead.

## Consequences

- Dependencies in the add-on only: `Serilog.Extensions.Logging`, `Serilog.Sinks.File`,
  `Serilog.Sinks.Console`.
- With `--verbose`, log lines interleave with prompts on the console. Accepted for a diagnostic
  option.

## Reversal Cost

Low. Services depend only on `ILogger<T>`; the provider is configured in one place.

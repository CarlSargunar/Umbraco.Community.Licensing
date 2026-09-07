## Purpose

Provides a pluggable provider abstraction for supplying the raw license key string to a host application from .NET configuration, environment variables, or Azure Key Vault.

## ADDED Requirements

### Requirement: Common provider abstraction
The system SHALL define a common abstraction for obtaining a raw license key string, so that multiple source implementations can be used interchangeably by callers.

#### Scenario: Swappable providers
- **WHEN** a host application configures a different license key source implementation
- **THEN** the rest of the application code that requests the license key SHALL be unaffected by the change

### Requirement: .NET configuration provider
The system SHALL provide a built-in provider that reads the license key from standard .NET configuration (for example, `appsettings.json` and any other source layered into `IConfiguration`).

#### Scenario: Key present in configuration
- **WHEN** a license key is present at the configured configuration key path
- **THEN** the configuration provider SHALL return that value as the raw license key

### Requirement: Environment variable provider
The system SHALL provide a built-in provider that reads the license key from a configurable environment variable name.

#### Scenario: Key present in environment
- **WHEN** a license key is present in the configured environment variable
- **THEN** the environment variable provider SHALL return that value as the raw license key

### Requirement: Azure Key Vault provider
The system SHALL provide a built-in provider that reads the license key from a configurable Azure Key Vault secret, without requiring host applications that do not use this provider to take on a dependency on Azure Key Vault client libraries.

#### Scenario: Key present in Key Vault
- **WHEN** a license key is stored as the configured secret in an accessible Azure Key Vault
- **THEN** the Key Vault provider SHALL return that secret's value as the raw license key

#### Scenario: Key Vault dependency isolation
- **WHEN** a host application does not reference the Key Vault provider
- **THEN** the host application SHALL NOT be required to take a dependency on Azure Key Vault client libraries

### Requirement: Missing key handling
The system SHALL clearly report when a configured source has no license key available, distinguishing "no key configured" from "key present but invalid" (the latter is a validation concern, not a sourcing concern).

#### Scenario: No key configured
- **WHEN** a configured source has no value for the license key
- **THEN** the provider SHALL report that no key was found rather than returning an empty or default license key value

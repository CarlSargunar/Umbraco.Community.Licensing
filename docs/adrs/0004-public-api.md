# ADR-0004: Public API surface

- Status: Decided
- Date: 2026-10-09
- Source: `add-license-core` design.md Q6, Q10, Q11 (`CLEAN-PROJECT-PROMPT.md` section 10,
  technology item 2)

## Context

Vendors call the issuer from their own tooling; products call the evaluator on every request
that gates a feature. Request types must report missing values as problems, not compile errors.
Read-side types must not be nullable where a verified key guarantees a value.

## Decision

Namespace `Umbraco.Community.Licensing.Core`. All public types sealed unless noted. Records for
values.

**Signing keys**

```csharp
public sealed class SigningKeyPair : IDisposable
{
    public static SigningKeyPair Create();
    public SigningPrivateKey PrivateKey { get; }
    public SigningPublicKey PublicKey { get; }
    public string SigningKeyId { get; }
}
public sealed class SigningPrivateKey : IDisposable
{
    public static SigningPrivateKey FromPem(string pem);   // FormatException if not P-256 PKCS#8
    public string ExportPem();
    public SigningPublicKey PublicKey { get; }
    public string SigningKeyId { get; }
}
public sealed class SigningPublicKey : IEquatable<SigningPublicKey>
{
    public static SigningPublicKey Parse(string text);     // FormatException; ID mismatch included
    public string Export();                                 // "<id>.<base64url SPKI>"
    public string SigningKeyId { get; }
}
public sealed class TrustedSigningKeys                       // immutable
{
    public static TrustedSigningKeys Empty { get; }
    public static TrustedSigningKeys Create(params IEnumerable<SigningPublicKey> keys);
    public TrustedSigningKeys With(SigningPublicKey key);   // ArgumentException on ID held by a different key
    public TrustedSigningKeys Without(string signingKeyId);
    public bool Contains(string signingKeyId);
    public IReadOnlyCollection<string> SigningKeyIds { get; }
}
```

**Issuing**

```csharp
public sealed record LicenseRequest
{
    public string? Product { get; init; }
    public string? Reference { get; init; }
    public LicenseExpiry? Expiry { get; init; }
    public string? DisplayName { get; init; }
    public string? VendorTag { get; init; }
    public FeatureList Features { get; init; } = FeatureList.Empty;
    public static LicenseRequest ReissueOf(VerifiedLicense license);
}
public abstract record LicenseExpiry                          // closed: private constructor
{
    public static LicenseExpiry On(DateOnly date);            // 23:59:59Z that day
    public static LicenseExpiry At(DateTimeOffset instant);   // fraction rejected at issue
    public static LicenseExpiry Perpetual { get; }
}
public abstract record FeatureValue;                          // closed: Switch, Number, Text
public sealed record SwitchFeature(bool IsOn) : FeatureValue; // read side always true
public sealed record NumberFeature(decimal Value) : FeatureValue;
public sealed record TextFeature(string Value) : FeatureValue;
public sealed record LicenseFeature(string Name, FeatureValue Value);
public sealed class FeatureList : IReadOnlyList<LicenseFeature>   // immutable, keeps order
{
    public static FeatureList Empty { get; }
    public static FeatureList Of(IEnumerable<LicenseFeature> features); // keeps duplicates, so issue reports them
    public FeatureList With(string name, bool on);        // replaces a same-name entry, else appends
    public FeatureList With(string name, decimal value);
    public FeatureList With(string name, string text);
    public FeatureList Without(string name);
}
public enum FeatureType { Switch, Number, Text }
public sealed class FeatureDefinitions : IReadOnlyCollection<KeyValuePair<string, FeatureType>>  // immutable
{
    public static FeatureDefinitions None { get; }        // empty: the product has no features
    public FeatureDefinitions Switch(string name);        // ArgumentException: invalid or repeated name
    public FeatureDefinitions Number(string name);
    public FeatureDefinitions Text(string name);
}
public sealed class LicenseIssuer(TimeProvider? timeProvider = null)
{
    public IssuedLicense Issue(LicenseRequest request, SigningPrivateKey signingKey,
        FeatureDefinitions? definitions = null);          // null: feature rules only
}
public sealed record IssuedLicense(string KeyString, string Reference, string KeyIdentifier,
    DateTimeOffset Issued);                               // ToString omits KeyString
public sealed class LicenseIssueException : Exception
{
    public IReadOnlyList<LicenseProblem> Problems { get; }
}
public sealed record LicenseProblem(string Field, string Message);
```

`Reference` and `KeyIdentifier` are returned in display form (`LIC-8F3AK-M7RXB`,
`LIC-8F3AK-M7RXB-7Q2D`). `With`/`Without` match names with the lookup rule (ignore `a`-`z` case).

**Evaluating**

```csharp
public enum LicenseState
{
    Missing, Unreadable, WrongProduct, SigningKeyNotRecognised, NotVerified, Expired, Valid
}
public sealed class LicenseEvaluator
{
    public LicenseEvaluator(string productId, TrustedSigningKeys trustedKeys,
        TimeProvider? timeProvider = null);               // ArgumentException on invalid productId
    public string ProductId { get; }
    public LicenseResult Evaluate(string? keyString);     // never throws
}
public sealed class LicenseResult
{
    public LicenseState State { get; }
    public bool IsLicensed { get; }                       // State == Valid
    public string? ClaimedProduct { get; }                // failed states only, when readable
    public string? ClaimedKeyIdentifier { get; }          // failed states only, when readable
    public VerifiedLicense? License { get; }              // Valid and Expired only
    public bool HasFeature(string name);                  // any type; false unless Valid
    public decimal? GetNumber(string name);               // null unless Valid and a number
    public string? GetText(string name);                  // null unless Valid and a text
    public override string ToString();                    // state + identifier, never key text
}
public sealed record VerifiedLicense(
    string Product, string Reference, string KeyIdentifier, string SigningKeyId,
    DateTimeOffset Issued, DateTimeOffset? Expires, string? DisplayName, string? VendorTag,
    FeatureList Features)
{
    public bool IsPerpetual => Expires is null;
}
```

`LicenseResult` and `VerifiedLicense` hold no key text, so default and custom `ToString` cannot
leak it.

## Alternatives Considered

| Option | Why not |
|---|---|
| `Result`/`Try` pattern for issue | Vendors issue in tooling, not hot paths; one exception with every problem is simpler to surface |
| Exceptions for evaluation failures | Breaks "never throws on key content"; host code must not need try/catch per request |
| `IsGranted(name)` separate from `HasFeature` | A switch is presence; one method covers both |
| `bool TryGetNumber(name, out decimal)` | Nullable return is shorter at call sites; no behavioural difference |
| Mutable trusted set | Concurrent mutation during evaluation; immutable makes "unchanged on error" trivial |
| Product ID per `Evaluate` call | An invalid ID throws in the request path on every call (design.md Q10) |
| Feature definitions on the issuer constructor | One issuer may serve several products |
| Feature definitions as a request field | A reissue request built from a key carries no definitions, so the check would be silently skipped |
| `null` and empty definitions both meaning "no check" | An empty product definition could not be expressed |
| Separate `ExpiresOn`/`ExpiresAt`/`IsPerpetual` request fields | "Both stated" becomes representable and must be rejected; one closed type cannot hold both |
| `Dictionary<string, FeatureValue>` for features | Cannot hold the duplicate a vendor tool might send, so it could not be reported |

## Consequences

"Neither or both expiries" reduces to "not stated" (`Expiry` null); "both" cannot be expressed.
`issued` and the key part have no request field, so "supplied" cannot be expressed either.

## Reversal Cost

Medium before 1.0 (callers recompile); high after.

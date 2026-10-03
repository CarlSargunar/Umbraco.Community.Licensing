using Umbraco.Community.Licensing.Internal;

namespace Umbraco.Community.Licensing;

/// <summary>
/// The contents of a key to issue. The issue time and key part are set by the library and
/// cannot be supplied.
/// </summary>
public sealed class LicenseRequest
{
    /// <summary>Required. <c>vendor.product</c>, lowercase (PDR-0017).</summary>
    public string? ProductId { get; init; }

    /// <summary>Required. No default (PDR-0011).</summary>
    public LicenseRole? Role { get; init; }

    /// <summary>
    /// The license reference of an earlier key, for a reissue; read ignoring case, hyphens and
    /// spaces. Omit for a first issue and a new reference is generated (PDR-0009).
    /// </summary>
    public string? Reference { get; init; }

    /// <summary>
    /// Optional label of the vendor's own, such as an order number: 1 to 64 characters from
    /// <c>A-Z a-z 0-9 - _ . # /</c>, signed exactly as supplied (PDR-0022). Not unique and not
    /// carried forward on reissue. It must never hold a name, email address, company or other
    /// personal data: a key cannot be withdrawn once issued.
    /// </summary>
    public string? VendorTag { get; init; }

    /// <summary>Optional. Valid until the end of this date in UTC; omit for never (PDR-0016).</summary>
    public DateOnly? Expires { get; init; }

    /// <summary>Zero or more features; each name at most once (PDR-0010).</summary>
    public IReadOnlyList<LicenseFeature>? Features { get; init; }
}

/// <summary>A newly issued key and the identifiers the vendor records against it.</summary>
/// <param name="KeyString">The single-line key string to send to the site owner.</param>
/// <param name="Reference">The license reference, <c>LIC-XXXXX-XXXXX</c>; pass it to reissue.</param>
/// <param name="KeyIdentifier">The key identifier, <c>LIC-XXXXX-XXXXX-XXXX</c>, at the start of the key string.</param>
/// <param name="Issued">The issue time, UTC, to the second.</param>
public sealed record IssuedLicenseKey(string KeyString, string Reference, string KeyIdentifier, DateTimeOffset Issued);

/// <summary>One rule a license request breaks.</summary>
/// <param name="Field">The request field: <c>ProductId</c>, <c>Role</c>, <c>Reference</c>, <c>VendorTag</c>, <c>Expires</c> or <c>Features</c>.</param>
/// <param name="Message">The rule broken.</param>
public sealed record LicenseRequestProblem(string Field, string Message)
{
    /// <inheritdoc />
    public override string ToString() => $"{Field}: {Message}";
}

/// <summary>A license request broke one or more rules. No key was produced.</summary>
public sealed class LicenseRequestException : ArgumentException
{
    /// <summary>Creates the exception with every broken rule.</summary>
    public LicenseRequestException(IReadOnlyList<LicenseRequestProblem> problems)
        : base("The license request is invalid: " + string.Join("; ", problems))
    {
        Problems = problems;
    }

    /// <summary>Every rule the request breaks.</summary>
    public IReadOnlyList<LicenseRequestProblem> Problems { get; }
}

/// <summary>
/// Checks a key's contents and signs them into a key string (issuer side; never ship it, or a
/// private key, inside a product). Keeps no record of what it issues and no key material.
/// </summary>
/// <param name="timeProvider">The clock for the issue time and the expiry check.</param>
public sealed class LicenseIssuer(TimeProvider timeProvider)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <summary>Creates an issuer on the system clock.</summary>
    public LicenseIssuer()
        : this(TimeProvider.System)
    {
    }

    /// <summary>Issues a key signed with <paramref name="signingKey"/>.</summary>
    /// <exception cref="LicenseRequestException">The request breaks one or more rules; every one is named.</exception>
    public IssuedLicenseKey Issue(LicenseRequest request, SigningPrivateKey signingKey)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(signingKey);

        var now = _timeProvider.GetUtcNow().ToUniversalTime();
        var issued = new DateTimeOffset(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond, TimeSpan.Zero);
        var problems = new List<LicenseRequestProblem>();

        if (!ProductIdRule.IsValid(request.ProductId))
        {
            problems.Add(new(nameof(request.ProductId),
                "must be vendor.product: two dot-separated parts of lowercase a-z, digits and hyphens, each starting with a letter"));
        }

        if (request.Role is null)
        {
            problems.Add(new(nameof(request.Role), "is required"));
        }
        else if (!Enum.IsDefined(request.Role.Value))
        {
            problems.Add(new(nameof(request.Role), "must be base or add-on"));
        }

        var reference = "";
        if (request.Reference is null)
        {
            reference = LicenseReferenceRule.NewReference();
        }
        else if (!LicenseReferenceRule.TryNormalize(request.Reference, out reference))
        {
            problems.Add(new(nameof(request.Reference),
                "must be 10 characters from uppercase letters and digits without 0 O 1 I L, written LIC-XXXXX-XXXXX"));
        }

        if (request.VendorTag is not null && !VendorTagRule.IsValid(request.VendorTag))
        {
            problems.Add(new(nameof(request.VendorTag),
                "must be 1 to 64 characters from A-Z a-z 0-9 - _ . # /"));
        }

        if (request.Expires is { } expires && expires < DateOnly.FromDateTime(issued.UtcDateTime))
        {
            problems.Add(new(nameof(request.Expires), "must not be before the current UTC date"));
        }

        var features = CheckFeatures(request.Features ?? [], problems);

        if (problems.Count > 0)
        {
            throw new LicenseRequestException(problems);
        }

        var payload = new LicensePayload(
            signingKey.SigningKeyId,
            request.ProductId!,
            request.Role!.Value,
            request.VendorTag,
            issued,
            request.Expires,
            features);
        var keyIdentifier = LicenseReferenceRule.KeyIdentifier(reference, LicenseReferenceRule.NewKeyPart());
        var keyString = KeyString.Sign(keyIdentifier, PayloadFormat.Write(payload), signingKey);
        return new IssuedLicenseKey(keyString, reference, keyIdentifier, issued);
    }

    private static List<KeyValuePair<string, FeatureValue>> CheckFeatures(
        IReadOnlyList<LicenseFeature> requested,
        List<LicenseRequestProblem> problems)
    {
        const string field = nameof(LicenseRequest.Features);
        var features = new List<KeyValuePair<string, FeatureValue>>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        var repeated = new HashSet<string>(StringComparer.Ordinal);

        foreach (var feature in requested)
        {
            if (feature is null)
            {
                problems.Add(new(field, "must not contain null"));
                continue;
            }

            var name = feature.Name;
            var label = name is null ? "a feature with no name" : $"'{name}'";
            if (!FeatureRules.IsValidName(name))
            {
                problems.Add(new(field,
                    $"{label}: names are lowercase a-z, digits and hyphens, starting with a letter"));
            }
            else if (!names.Add(name!) && repeated.Add(name!))
            {
                problems.Add(new(field, $"{label}: a name appears at most once per key"));
            }

            FeatureValue? value = null;
            string? problem = null;
            switch (feature.Kind)
            {
                case FeatureKind.Switch:
                    if (feature.SwitchGranted)
                    {
                        value = FeatureValue.Switch;
                    }
                    else
                    {
                        problem = "explicit false is not allowed; leave the feature out instead";
                    }

                    break;
                case FeatureKind.Number:
                    problem = feature.NumberValue is { } number
                        ? NumberRule.Check(number, out var wire)
                        : NumberRule.Check(feature.NumberText, out wire);
                    if (problem is null)
                    {
                        NumberRule.TryRead(wire, out var canonical);
                        value = FeatureValue.FromNumber(canonical);
                    }

                    break;
                default:
                    problem = FeatureRules.TextProblem(feature.TextValue);
                    if (problem is null)
                    {
                        value = FeatureValue.FromText(feature.TextValue!);
                    }

                    break;
            }

            if (problem is not null)
            {
                problems.Add(new(field, $"{label}: {problem}"));
            }
            else if (name is not null)
            {
                features.Add(new(name, value!));
            }
        }

        return features;
    }
}

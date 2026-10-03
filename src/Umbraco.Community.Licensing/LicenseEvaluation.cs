namespace Umbraco.Community.Licensing;

/// <summary>
/// A key's state. The first check that applies decides (PDR-0019). The first four are failure
/// reasons; from <see cref="Duplicate"/> on, the key is verified.
/// </summary>
public enum LicenseKeyState
{
    /// <summary>The string cannot be read as a key, or a verified key's contents break a rule checked at issue. First action: paste the key again.</summary>
    Unreadable,

    /// <summary>The key claims another product. First action: move the key to the product it names.</summary>
    WrongProduct,

    /// <summary>The key's signing key is not trusted by this product. First action: update the product; if that fails, ask the vendor.</summary>
    SigningKeyNotRecognised,

    /// <summary>The signature does not verify. First action: paste the key again; if that fails, ask the vendor.</summary>
    NotVerified,

    /// <summary>An exact copy of a key earlier in the list. Counts once; nothing to fix.</summary>
    Duplicate,

    /// <summary>A key under the same reference was issued later and counts instead.</summary>
    Superseded,

    /// <summary>Past the end of its expiry date in UTC. First action: renew.</summary>
    Expired,

    /// <summary>An add-on with no valid base for the product.</summary>
    Inactive,

    /// <summary>Counts toward the product.</summary>
    Valid,
}

/// <summary>The contents of a verified key, as facts.</summary>
/// <param name="ProductId">The product ID.</param>
/// <param name="Role">Base or add-on.</param>
/// <param name="Reference">The license reference, <c>LIC-XXXXX-XXXXX</c>.</param>
/// <param name="KeyIdentifier">The key identifier, <c>LIC-XXXXX-XXXXX-XXXX</c>.</param>
/// <param name="VendorTag">The vendor's label, or <c>null</c> when the key has none.</param>
/// <param name="Issued">The issue time, UTC, to the second.</param>
/// <param name="Expires">The expiry date (valid to its end in UTC), or <c>null</c> for never.</param>
/// <param name="Features">The key's features, by name.</param>
/// <param name="SigningKeyId">The signing key ID it verified against.</param>
public sealed record VerifiedLicense(
    string ProductId,
    LicenseRole Role,
    string Reference,
    string KeyIdentifier,
    string? VendorTag,
    DateTimeOffset Issued,
    DateOnly? Expires,
    IReadOnlyDictionary<string, FeatureValue> Features,
    string SigningKeyId);

/// <summary>
/// The result for one supplied key string. Contains no part of the key string other than its
/// key identifier.
/// </summary>
public sealed class LicenseKeyRow
{
    internal LicenseKeyRow(int position, LicenseKeyState state, string? keyIdentifier)
    {
        Position = position;
        State = state;
        KeyIdentifier = keyIdentifier;
    }

    /// <summary>Zero-based position of the string in the list supplied. Names a row with no readable identifier.</summary>
    public int Position { get; }

    /// <summary>The key's state.</summary>
    public LicenseKeyState State { get; }

    /// <summary>Whether the signature verified, so <see cref="License"/> holds facts.</summary>
    public bool IsVerified => State >= LicenseKeyState.Duplicate;

    /// <summary>
    /// The key identifier at the start of the string, when readable. For a key that is not
    /// verified this is a claim.
    /// </summary>
    public string? KeyIdentifier { get; }

    /// <summary>
    /// The product ID a key that failed claims, when readable. Always <c>null</c> for a verified
    /// key; read <see cref="License"/> instead.
    /// </summary>
    public string? ClaimedProductId { get; internal init; }

    /// <summary>The verified contents; <c>null</c> for a key that failed (PDR-0019).</summary>
    public VerifiedLicense? License { get; internal init; }

    /// <summary>For a <see cref="LicenseKeyState.Duplicate"/>, the position of the row it copies.</summary>
    public int? DuplicateOf { get; internal init; }

    /// <summary>For a <see cref="LicenseKeyState.Superseded"/> key, the position of the latest key under its reference.</summary>
    public int? SupersededBy { get; internal init; }

    /// <summary>
    /// Set on a superseded key whose role differed from the key that superseded it: the role
    /// this key carried (PDR-0009).
    /// </summary>
    public LicenseRole? SupersededRoleChange { get; internal init; }

    /// <summary>
    /// Vendor error: the key identifiers of the other keys tied with this one for latest under
    /// its reference. Empty when there is no tie. The key still counts (PDR-0009).
    /// </summary>
    public IReadOnlyList<string> TiedWith { get; internal init; } = [];

    /// <summary>Whether this key is flagged as a vendor error (tied for latest).</summary>
    public bool HasVendorError => TiedWith.Count > 0;

    /// <summary>
    /// Feature names this counted key carries that conflict at product level (PDR-0018). The key
    /// stays valid.
    /// </summary>
    public IReadOnlyList<string> ConflictingFeatures { get; internal init; } = [];
}

/// <summary>The combined entitlement for one product.</summary>
public sealed class ProductLicense
{
    internal ProductLicense(
        string productId,
        int validBaseCount,
        IReadOnlyDictionary<string, FeatureValue> features,
        IReadOnlyList<string> conflictingFeatures)
    {
        ProductId = productId;
        ValidBaseCount = validBaseCount;
        Features = features;
        ConflictingFeatures = conflictingFeatures;
    }

    /// <summary>The product evaluated.</summary>
    public string ProductId { get; }

    /// <summary>Whether at least one valid base license counts.</summary>
    public bool IsLicensed => ValidBaseCount > 0;

    /// <summary>How many valid base licenses count. More than one usually means a duplicate purchase (PDR-0011).</summary>
    public int ValidBaseCount { get; }

    /// <summary>
    /// The combined features, by lowercase name, in ordinal name order. Excludes conflicting
    /// features. Empty when the product is not licensed.
    /// </summary>
    public IReadOnlyDictionary<string, FeatureValue> Features { get; }

    /// <summary>Feature names that answer nothing because counted keys disagree (PDR-0018), in ordinal order.</summary>
    public IReadOnlyList<string> ConflictingFeatures { get; }

    /// <summary>
    /// Looks up a feature ignoring the case of <c>A-Z</c>, with the same answer under every
    /// culture. Returns <c>null</c> when not granted: unknown, conflicting, or the product is
    /// not licensed.
    /// </summary>
    public FeatureValue? GetFeature(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return Features.TryGetValue(FoldAsciiCase(name), out var value) ? value : null;
    }

    /// <summary>Whether <paramref name="name"/> is granted, whatever its type. See <see cref="GetFeature"/>.</summary>
    public bool IsGranted(string name) => GetFeature(name) is not null;

    // Only A-Z are folded. Unicode case mapping would match look-alikes such as the Kelvin
    // sign and depends on culture for the Turkish dotted and dotless i (PDR-0013).
    private static string FoldAsciiCase(string name) =>
        string.Create(name.Length, name, static (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                span[i] = char.IsAsciiLetterUpper(c) ? (char)(c + 32) : c;
            }
        });
}

/// <summary>The result of evaluating a list of key strings for one product.</summary>
/// <param name="Rows">One row per supplied string, in the order supplied.</param>
/// <param name="Product">The combined entitlement; does not depend on the order of the strings.</param>
public sealed record LicenseEvaluation(IReadOnlyList<LicenseKeyRow> Rows, ProductLicense Product);

using System.Globalization;

namespace Umbraco.Community.Licensing;

/// <summary>Whether a key licenses its product on its own (PDR-0011).</summary>
public enum LicenseRole
{
    /// <summary>Licenses the product on its own.</summary>
    Base,

    /// <summary>Counts only while a valid base for the same product is present.</summary>
    AddOn,
}

/// <summary>The type of a feature value (PDR-0010, PDR-0018).</summary>
public enum FeatureKind
{
    /// <summary>Present means granted.</summary>
    Switch,

    /// <summary>A zero or positive additive quantity; summed across licenses.</summary>
    Number,

    /// <summary>Text; never combined. Different values under one name conflict.</summary>
    Text,
}

/// <summary>A feature value read from a verified key or combined for a product.</summary>
public sealed record FeatureValue
{
    private FeatureValue(FeatureKind kind, decimal? number, string? text)
    {
        Kind = kind;
        Number = number;
        Text = text;
    }

    /// <summary>A granted switch.</summary>
    public static FeatureValue Switch { get; } = new(FeatureKind.Switch, null, null);

    /// <summary>The value's type.</summary>
    public FeatureKind Kind { get; }

    /// <summary>The value when <see cref="Kind"/> is <see cref="FeatureKind.Number"/>; exact.</summary>
    public decimal? Number { get; }

    /// <summary>The value when <see cref="Kind"/> is <see cref="FeatureKind.Text"/>.</summary>
    public string? Text { get; }

    internal static FeatureValue FromNumber(decimal value) => new(FeatureKind.Number, value, null);

    internal static FeatureValue FromText(string value) => new(FeatureKind.Text, null, value);

    /// <inheritdoc />
    public override string ToString() => Kind switch
    {
        FeatureKind.Switch => "granted",
        FeatureKind.Number => Number!.Value.ToString(CultureInfo.InvariantCulture),
        _ => Text!,
    };
}

/// <summary>
/// A feature to issue into a key. Construction never fails; the rules are checked when the key
/// is issued, and every broken rule is reported together.
/// </summary>
public sealed class LicenseFeature
{
    private LicenseFeature(string? name, FeatureKind kind)
    {
        Name = name;
        Kind = kind;
    }

    /// <summary>The feature name: lowercase <c>a-z</c>, digits and hyphens, starting with a letter (PDR-0013).</summary>
    public string? Name { get; }

    /// <summary>The value's type.</summary>
    public FeatureKind Kind { get; }

    internal bool SwitchGranted { get; private init; }

    internal decimal? NumberValue { get; private init; }

    internal string? NumberText { get; private init; }

    internal string? TextValue { get; private init; }

    /// <summary>A granted switch.</summary>
    public static LicenseFeature Switch(string name) => Switch(name, true);

    /// <summary>
    /// A switch from a flag. <c>false</c> is rejected at issue: absence means not granted (PDR-0010).
    /// </summary>
    public static LicenseFeature Switch(string name, bool granted) =>
        new(name, FeatureKind.Switch) { SwitchGranted = granted };

    /// <summary>
    /// A number: zero or positive, at most 4 decimal places and 15 digits. Trailing fractional
    /// zeros are removed (<c>2.50</c> is issued as <c>2.5</c>).
    /// </summary>
    public static LicenseFeature Number(string name, decimal value) =>
        new(name, FeatureKind.Number) { NumberValue = value };

    /// <summary>
    /// A number given as invariant-culture text, such as a value from a form or a file.
    /// Malformed text is rejected at issue; the feature is issued as a number, not text.
    /// </summary>
    public static LicenseFeature Number(string name, string value) =>
        new(name, FeatureKind.Number) { NumberText = value };

    /// <summary>
    /// Text: non-empty, at most 256 characters, no leading or trailing whitespace, no line
    /// breaks or control characters (PDR-0018). Never read as a number.
    /// </summary>
    public static LicenseFeature Text(string name, string value) =>
        new(name, FeatureKind.Text) { TextValue = value };
}

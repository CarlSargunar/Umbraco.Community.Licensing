using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Umbraco.Community.Licensing.Internal;

/// <summary>Product ID rule (PDR-0017): <c>vendor.product</c>, lowercase.</summary>
internal static partial class ProductIdRule
{
    [GeneratedRegex(@"^[a-z][a-z0-9-]*\.[a-z][a-z0-9-]*\z")]
    private static partial Regex Pattern();

    public static bool IsValid(string? productId) => productId is not null && Pattern().IsMatch(productId);
}

/// <summary>
/// License reference and key part (PDR-0017, PDR-0020): 31-character alphabet without
/// <c>0 O 1 I L</c>; reference shown <c>LIC-XXXXX-XXXXX</c>, key identifier <c>LIC-XXXXX-XXXXX-XXXX</c>.
/// </summary>
internal static partial class LicenseReferenceRule
{
    public const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
    private const int ReferenceLength = 10;
    private const int KeyPartLength = 4;

    [GeneratedRegex(@"^LIC-[2-9A-HJKMNP-Z]{5}-[2-9A-HJKMNP-Z]{5}-[2-9A-HJKMNP-Z]{4}\z")]
    private static partial Regex KeyIdentifierPattern();

    public static string NewReference() => Format(RandomNumberGenerator.GetString(Alphabet, ReferenceLength));

    public static string NewKeyPart() => RandomNumberGenerator.GetString(Alphabet, KeyPartLength);

    /// <summary>
    /// Reads a reference ignoring case, hyphens and spaces, with or without the <c>LIC</c> prefix,
    /// and returns it as displayed. The prefix cannot be confused with reference characters:
    /// <c>L</c> and <c>I</c> are not in the alphabet.
    /// </summary>
    public static bool TryNormalize(string? input, out string reference)
    {
        reference = "";
        if (input is null)
        {
            return false;
        }

        // ASCII-only uppercasing: a culture or Unicode case mapping could turn a non-ASCII
        // letter into an alphabet character.
        var compact = string.Concat(input
            .Where(c => c is not ('-' or ' '))
            .Select(c => char.IsAsciiLetterLower(c) ? (char)(c - 32) : c));
        if (compact.StartsWith("LIC", StringComparison.Ordinal))
        {
            compact = compact[3..];
        }

        if (compact.Length != ReferenceLength || !compact.All(Alphabet.Contains))
        {
            return false;
        }

        reference = Format(compact);
        return true;
    }

    public static bool IsKeyIdentifier(string? text) => text is not null && KeyIdentifierPattern().IsMatch(text);

    /// <summary>The reference part (<c>LIC-XXXXX-XXXXX</c>) of a key identifier.</summary>
    public static string ReferenceOf(string keyIdentifier) => keyIdentifier[..15];

    public static string KeyIdentifier(string reference, string keyPart) => reference + "-" + keyPart;

    private static string Format(string tenCharacters) => $"LIC-{tenCharacters[..5]}-{tenCharacters[5..]}";
}

/// <summary>Vendor tag rule (PDR-0022, ADR-0001), shared by issue and read.</summary>
internal static partial class VendorTagRule
{
    // \z, not $: in .NET $ also matches before a final \n. No IgnoreCase: the class is ordinal.
    [GeneratedRegex(@"^[A-Za-z0-9_.#/-]{1,64}\z")]
    private static partial Regex Pattern();

    public static bool IsValid(string? tag) => tag is not null && Pattern().IsMatch(tag);
}

/// <summary>Feature name and value rules (PDR-0010, PDR-0013, PDR-0018), shared by issue and read.</summary>
internal static partial class FeatureRules
{
    public const int MaxTextLength = 256;

    [GeneratedRegex(@"^[a-z][a-z0-9-]*\z")]
    private static partial Regex NamePattern();

    public static bool IsValidName(string? name) => name is not null && NamePattern().IsMatch(name);

    /// <summary>Returns the rule a text value breaks, or <c>null</c> when it is valid.</summary>
    public static string? TextProblem(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "text must not be empty";
        }

        var length = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                i++;
            }
            else if (char.IsSurrogate(c))
            {
                return "text contains an invalid character";
            }
            else if (char.IsControl(c) || char.GetUnicodeCategory(c) is UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator)
            {
                return "text must not contain line breaks or other control characters";
            }

            length++;
        }

        if (length > MaxTextLength)
        {
            return $"text must be at most {MaxTextLength} characters";
        }

        if (char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[^1]))
        {
            return "text must not have leading or trailing whitespace";
        }

        return null;
    }
}

/// <summary>
/// Number encoding (PDR-0010, ADR-0001): <c>0</c> or <c>[1-9][0-9]*</c>, optionally <c>.</c> and
/// 1 to 4 digits; at most 15 digits; no sign or exponent. Held as <see cref="decimal"/>, never
/// <see cref="double"/>.
/// </summary>
internal static partial class NumberRule
{
    public const int MaxDecimalPlaces = 4;
    public const int MaxDigits = 15;

    [GeneratedRegex(@"^(0|[1-9][0-9]*)(\.[0-9]{1,4})?\z")]
    private static partial Regex WirePattern();

    [GeneratedRegex(@"^-?(0|[1-9][0-9]*)(\.[0-9]+)?\z")]
    private static partial Regex InputPattern();

    /// <summary>Reads a raw JSON number token. Returns <c>false</c> when it breaks the grammar.</summary>
    public static bool TryRead(string raw, out decimal value)
    {
        value = 0;
        if (!WirePattern().IsMatch(raw) || CountDigits(raw) > MaxDigits)
        {
            return false;
        }

        value = Normalize(decimal.Parse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture));
        return true;
    }

    /// <summary>
    /// Checks an issuer's value and returns its wire form: trailing fractional zeros removed,
    /// invariant culture. Returns the rule broken, or <c>null</c>.
    /// </summary>
    public static string? Check(decimal input, out string wire)
    {
        wire = "";
        if (input < 0)
        {
            return "numbers must be zero or positive";
        }

        var value = Normalize(input);
        if (value.Scale > MaxDecimalPlaces)
        {
            return $"numbers must have at most {MaxDecimalPlaces} decimal places";
        }

        var text = value.ToString(CultureInfo.InvariantCulture);
        if (CountDigits(text) > MaxDigits)
        {
            return $"numbers must have at most {MaxDigits} digits";
        }

        wire = text;
        return null;
    }

    /// <summary>Parses an issuer's number given as text, then applies <see cref="Check(decimal, out string)"/>.</summary>
    public static string? Check(string? input, out string wire)
    {
        wire = "";
        if (input is null || !InputPattern().IsMatch(input)
            || !decimal.TryParse(input, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var value))
        {
            return "number is malformed";
        }

        return Check(value, out wire);
    }

    /// <summary>Removes trailing fractional zeros and the sign of negative zero.</summary>
    public static decimal Normalize(decimal value) =>
        value == 0 ? 0m : value / 1.0000000000000000000000000000m;

    private static int CountDigits(string text) => text.Count(char.IsAsciiDigit);
}

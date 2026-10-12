using System.Text.RegularExpressions;

namespace Umbraco.Community.Licensing.Core;

/// <summary>Display forms and the text pattern of a reference and a key identifier. A null prefix means no prefix.</summary>
internal static partial class KeyIdentifierFormat
{
    /// <summary>The text before the first <c>.</c> of a key string must match this in full (ADR-0001).</summary>
    [GeneratedRegex(@"^(?:[A-Z0-9]{1,16}-)?[2-9A-HJKMNP-Z]{5}-[2-9A-HJKMNP-Z]{5}-[2-9A-HJKMNP-Z]{4}\z", RegexOptions.CultureInvariant)]
    public static partial Regex IdentifierPattern { get; }

    /// <summary>Shows a 10-character reference as <c>PREFIX-XXXXX-XXXXX</c>, or <c>XXXXX-XXXXX</c> with no prefix. No prefix is passed as null, never an empty string.</summary>
    public static string DisplayReference(string? prefix, string reference)
    {
        var groups = $"{reference[..5]}-{reference[5..]}";
        return prefix is null ? groups : $"{prefix}-{groups}";
    }

    /// <summary>The 10-character reference (no hyphens, no prefix) inside an identifier that matches <see cref="IdentifierPattern"/>.</summary>
    public static string ReferenceOf(string identifier)
    {
        var groups = identifier[^16..];
        return groups[..5] + groups[6..11];
    }

    /// <summary>Shows the key identifier: the displayed reference, <c>-</c> and the key part.</summary>
    public static string DisplayIdentifier(string? prefix, string reference, string keyPart) =>
        $"{DisplayReference(prefix, reference)}-{keyPart}";
}

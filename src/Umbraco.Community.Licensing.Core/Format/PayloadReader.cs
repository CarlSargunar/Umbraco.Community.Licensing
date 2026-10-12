using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Umbraco.Community.Licensing.Core;

/// <summary>
/// The strict read (ADR-0002), run only on a verified payload. Refuses an unknown or repeated field, a repeated
/// feature name, a date not in the exact format, an unsupported feature token, a number outside the grammar and any
/// <see cref="ContentRules"/> breach. A failure is a result (false, with a reason for tests and diagnostics), never an
/// exception; the evaluator maps it to <i>not supported</i> (PDR-0018). Every bad input has its own check; the only
/// catch is around <see cref="Utf8JsonReader.Read"/>, which reports malformed JSON by throwing and has no other API.
/// </summary>
/// <remarks>
/// Repeated feature names are compared ordinally on the decoded name (an escaped form repeats its plain form). That
/// agrees with <see cref="ContentRules"/>, whose names are lowercase-only, so a name differing only in case is
/// refused by the name rule rather than counted as a repeat.
/// </remarks>
internal static class PayloadReader
{
    private const string DateFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private const int SigningKeyIdLength = 11;

    // Root object (1) and features object (2); a nested value under a feature (3) is refused by its token type anyway.
    private const int MaxDepth = 3;

    public static bool TryRead(ReadOnlySpan<byte> payload, out LicensePayload? result) =>
        TryRead(payload, out result, out _);

    public static bool TryRead(ReadOnlySpan<byte> payload, out LicensePayload? result, out string? failure)
    {
        result = null;
        failure = null;

        if (!PayloadJson.IsValidUtf8(payload))
        {
            failure = "The payload is not valid UTF-8.";
            return false;
        }

        if (PayloadJson.HasUnpairedSurrogateEscape(payload))
        {
            failure = "The payload holds an unpaired surrogate escape.";
            return false;
        }

        var reader = new Utf8JsonReader(payload, new JsonReaderOptions { MaxDepth = MaxDepth });
        if (!Advance(ref reader) || reader.TokenType != JsonTokenType.StartObject)
        {
            failure = "The payload is not a JSON object.";
            return false;
        }

        string? signingKeyId = null;
        string? product = null;
        DateTimeOffset? issued = null;
        DateTimeOffset? expires = null;
        string? displayName = null;
        string? vendorTag = null;
        List<FeatureInput>? features = null;

        while (true)
        {
            if (!Advance(ref reader))
            {
                failure = "The payload is malformed or truncated.";
                return false;
            }

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            var name = reader.GetString()!;
            if (!Advance(ref reader))
            {
                failure = "The payload is malformed or truncated.";
                return false;
            }

            switch (name)
            {
                case "signingKeyId":
                    if (!ReadString(ref reader, name, signingKeyId, out signingKeyId, out failure))
                    {
                        return false;
                    }

                    break;

                case "product":
                    if (!ReadString(ref reader, name, product, out product, out failure))
                    {
                        return false;
                    }

                    break;

                case "issued":
                    if (!ReadDate(ref reader, name, issued is not null, out var issuedValue, out failure))
                    {
                        return false;
                    }

                    issued = issuedValue;
                    break;

                case "expires":
                    if (!ReadDate(ref reader, name, expires is not null, out var expiresValue, out failure))
                    {
                        return false;
                    }

                    expires = expiresValue;
                    break;

                case "displayName":
                    if (!ReadString(ref reader, name, displayName, out displayName, out failure))
                    {
                        return false;
                    }

                    break;

                case "vendorTag":
                    if (!ReadString(ref reader, name, vendorTag, out vendorTag, out failure))
                    {
                        return false;
                    }

                    break;

                case "features":
                    if (features is not null)
                    {
                        failure = "The field features is repeated.";
                        return false;
                    }

                    if (!ReadFeatures(ref reader, out features, out failure))
                    {
                        return false;
                    }

                    break;

                default:
                    failure = $"The field {name} is not known.";
                    return false;
            }
        }

        // Nothing may follow the object; trailing content makes the reader throw.
        if (!AtEnd(ref reader))
        {
            failure = "The payload has content after the object.";
            return false;
        }

        if (signingKeyId is null || product is null || issued is null)
        {
            failure = "The payload lacks signingKeyId, product or issued.";
            return false;
        }

        if (signingKeyId.Length != SigningKeyIdLength)
        {
            failure = $"The signingKeyId must be {SigningKeyIdLength} characters.";
            return false;
        }

        var featureList = (IReadOnlyList<FeatureInput>?)features ?? [];
        var problems = ContentRules.Check(product, displayName, vendorTag, featureList);
        if (problems.Count > 0)
        {
            failure = $"{problems[0].Field}: {problems[0].Message}";
            return false;
        }

        result = new LicensePayload(signingKeyId, product, issued.Value, expires, displayName, vendorTag, featureList);
        return true;
    }

    // Utf8JsonReader reports malformed JSON by throwing JsonException; there is no non-throwing form.
    private static bool Advance(ref Utf8JsonReader reader)
    {
        try
        {
            return reader.Read();
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // True when the input ends cleanly; trailing content or a truncation makes the reader throw.
    private static bool AtEnd(ref Utf8JsonReader reader)
    {
        try
        {
            return !reader.Read();
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool ReadString(ref Utf8JsonReader reader, string field, string? existing, out string? value, out string? failure)
    {
        value = existing;
        failure = null;
        if (existing is not null)
        {
            failure = $"The field {field} is repeated.";
            return false;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            failure = $"The field {field} must be a string.";
            return false;
        }

        value = reader.GetString();
        return true;
    }

    private static bool ReadDate(ref Utf8JsonReader reader, string field, bool repeated, out DateTimeOffset value, out string? failure)
    {
        value = default;
        failure = null;
        if (repeated)
        {
            failure = $"The field {field} is repeated.";
            return false;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            failure = $"The field {field} must be a string.";
            return false;
        }

        if (!DateTimeOffset.TryParseExact(
                reader.GetString(),
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out value))
        {
            failure = $"The field {field} must be exactly yyyy-MM-ddTHH:mm:ssZ.";
            return false;
        }

        return true;
    }

    private static bool ReadFeatures(ref Utf8JsonReader reader, out List<FeatureInput>? features, out string? failure)
    {
        features = null;
        failure = null;
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            failure = "The field features must be an object.";
            return false;
        }

        var list = new List<FeatureInput>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (true)
        {
            if (!Advance(ref reader))
            {
                failure = "The payload is malformed or truncated.";
                return false;
            }

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                features = list;
                return true;
            }

            var name = reader.GetString()!;
            if (!seen.Add(name))
            {
                failure = $"The feature name {name} appears more than once.";
                return false;
            }

            if (!ContentRules.IsValidFeatureName(name))
            {
                failure = $"The feature name {name} is not valid.";
                return false;
            }

            if (!Advance(ref reader))
            {
                failure = "The payload is malformed or truncated.";
                return false;
            }

            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    list.Add(FeatureInput.Switch(name, true));
                    break;

                case JsonTokenType.String:
                    list.Add(FeatureInput.OfText(name, reader.GetString()));
                    break;

                case JsonTokenType.Number:
                    if (!TryReadNumber(reader.ValueSpan, out var number))
                    {
                        failure = $"The number {name} breaks the number rule.";
                        return false;
                    }

                    list.Add(FeatureInput.OfNumber(name, number));
                    break;

                default:
                    failure = $"The feature {name} has an unsupported value type ({reader.TokenType}).";
                    return false;
            }
        }
    }

    // Raw token bytes against the grammar, then decimal; the value is stored in its canonical scale (2.50 reads as 2.5).
    private static bool TryReadNumber(ReadOnlySpan<byte> raw, out decimal value)
    {
        value = 0m;
        if (raw.Length > 32)
        {
            return false; // longer than any number the grammar allows
        }

        Span<char> chars = stackalloc char[32];
        chars = chars[..raw.Length];
        for (var i = 0; i < raw.Length; i++)
        {
            chars[i] = (char)raw[i];
        }

        return NumberRule.TryParse(chars, out value)
            && NumberRule.TryNormalise(value, out var text, out _)
            && decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }
}

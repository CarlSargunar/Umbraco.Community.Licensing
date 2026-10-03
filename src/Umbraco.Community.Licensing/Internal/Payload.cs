using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Umbraco.Community.Licensing.Internal;

/// <summary>The signed contents of a key, other than the key identifier (ADR-0001, Payload).</summary>
internal sealed record LicensePayload(
    string SigningKeyId,
    string ProductId,
    LicenseRole Role,
    string? VendorTag,
    DateTimeOffset Issued,
    DateOnly? Expires,
    IReadOnlyList<KeyValuePair<string, FeatureValue>> Features);

/// <summary>
/// The result of reading a payload. <see cref="ProductId"/> and <see cref="SigningKeyId"/> are
/// the routing claims (ADR-0001 step 4); <see cref="Payload"/> is set only when every schema
/// rule holds (step 6).
/// </summary>
internal sealed record PayloadReadResult(string? ProductId, string? SigningKeyId, LicensePayload? Payload)
{
    public bool HasRoutingClaims => ProductId is not null && SigningKeyId is not null;
}

internal static partial class PayloadFormat
{
    private const string IssuedFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";
    private const string ExpiresFormat = "yyyy-MM-dd";

    private static readonly HashSet<string> KnownFields =
        ["signingKeyId", "product", "role", "vendorTag", "issued", "expires", "features"];

    [GeneratedRegex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z\z")]
    private static partial Regex IssuedPattern();

    [GeneratedRegex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}\z")]
    private static partial Regex ExpiresPattern();

    [GeneratedRegex(@"^[A-Za-z0-9_-]{11}\z")]
    private static partial Regex SigningKeyIdPattern();

    public static string RoleText(LicenseRole role) => role == LicenseRole.Base ? "base" : "add-on";

    /// <summary>
    /// Writes UTF-8 JSON without whitespace; numbers as their wire text. The caller has checked
    /// every rule.
    /// </summary>
    public static byte[] Write(LicensePayload payload)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("signingKeyId", payload.SigningKeyId);
            writer.WriteString("product", payload.ProductId);
            writer.WriteString("role", RoleText(payload.Role));
            if (payload.VendorTag is not null)
            {
                writer.WriteString("vendorTag", payload.VendorTag);
            }

            writer.WriteString("issued", payload.Issued.UtcDateTime.ToString(IssuedFormat, CultureInfo.InvariantCulture));
            if (payload.Expires is { } expires)
            {
                writer.WriteString("expires", expires.ToString(ExpiresFormat, CultureInfo.InvariantCulture));
            }

            if (payload.Features.Count > 0)
            {
                writer.WriteStartObject("features");
                foreach (var (name, value) in payload.Features)
                {
                    writer.WritePropertyName(name);
                    switch (value.Kind)
                    {
                        case FeatureKind.Switch:
                            writer.WriteBooleanValue(true);
                            break;
                        case FeatureKind.Number:
                            if (NumberRule.Check(value.Number!.Value, out var wire) is { } problem)
                            {
                                throw new InvalidOperationException(problem);
                            }

                            writer.WriteRawValue(wire);
                            break;
                        default:
                            writer.WriteStringValue(value.Text);
                            break;
                    }
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Reads a payload strictly (ADR-0001, Read strictly; PDR-0021). Never throws.
    /// </summary>
    public static PayloadReadResult Read(ReadOnlySpan<byte> json)
    {
        try
        {
            return ReadCore(json);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException
                                       or ArgumentException or DecoderFallbackException)
        {
            return new PayloadReadResult(null, null, null);
        }
    }

    private static PayloadReadResult ReadCore(ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(json, new JsonReaderOptions { MaxDepth = 8 });
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            return new PayloadReadResult(null, null, null);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var strings = new Dictionary<string, string>(StringComparer.Ordinal);
        var features = new List<KeyValuePair<string, FeatureValue>>();
        var schemaValid = true;
        var routingValid = true;

        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            var field = reader.GetString()!;
            var repeated = !seen.Add(field);
            reader.Read();

            if (repeated)
            {
                schemaValid = false;
                if (field is "product" or "signingKeyId")
                {
                    routingValid = false;
                }
            }

            if (field == "features")
            {
                if (reader.TokenType != JsonTokenType.StartObject || !ReadFeatures(ref reader, features))
                {
                    schemaValid = false;
                    reader.Skip();
                }
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                strings[field] = reader.GetString()!;
            }
            else
            {
                // Wrong type, including null for vendorTag and expires.
                schemaValid = false;
                if (field is "product" or "signingKeyId")
                {
                    routingValid = false;
                }

                reader.Skip();
            }

            if (!KnownFields.Contains(field))
            {
                schemaValid = false;
            }
        }

        if (reader.TokenType != JsonTokenType.EndObject || reader.Read())
        {
            return new PayloadReadResult(null, null, null);
        }

        strings.TryGetValue("product", out var productId);
        strings.TryGetValue("signingKeyId", out var signingKeyId);
        if (!routingValid || productId is null || signingKeyId is null)
        {
            return new PayloadReadResult(null, null, null);
        }

        var payload = schemaValid ? CheckSchema(strings, features) : null;
        return new PayloadReadResult(productId, signingKeyId, payload);
    }

    private static bool ReadFeatures(ref Utf8JsonReader reader, List<KeyValuePair<string, FeatureValue>> features)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var valid = true;
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            var name = reader.GetString()!;
            reader.Read();

            // Names are lowercase by rule, so an exact comparison finds repeats.
            if (!names.Add(name) || !FeatureRules.IsValidName(name))
            {
                valid = false;
            }

            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    features.Add(new(name, FeatureValue.Switch));
                    break;
                case JsonTokenType.Number:
                    // Number tokens are never escaped, so ValueSpan is the raw token text.
                    var raw = Encoding.UTF8.GetString(reader.ValueSpan);
                    if (NumberRule.TryRead(raw, out var number))
                    {
                        features.Add(new(name, FeatureValue.FromNumber(number)));
                    }
                    else
                    {
                        valid = false;
                    }

                    break;
                case JsonTokenType.String:
                    var text = reader.GetString()!;
                    if (FeatureRules.TextProblem(text) is null)
                    {
                        features.Add(new(name, FeatureValue.FromText(text)));
                    }
                    else
                    {
                        valid = false;
                    }

                    break;
                default:
                    // false, null, object or array.
                    valid = false;
                    reader.Skip();
                    break;
            }
        }

        return valid;
    }

    private static LicensePayload? CheckSchema(
        Dictionary<string, string> strings,
        List<KeyValuePair<string, FeatureValue>> features)
    {
        if (!strings.TryGetValue("signingKeyId", out var signingKeyId) || !SigningKeyIdPattern().IsMatch(signingKeyId)
            || !strings.TryGetValue("product", out var productId) || !ProductIdRule.IsValid(productId)
            || !strings.TryGetValue("role", out var roleText)
            || !strings.TryGetValue("issued", out var issuedText))
        {
            return null;
        }

        LicenseRole role;
        switch (roleText)
        {
            case "base":
                role = LicenseRole.Base;
                break;
            case "add-on":
                role = LicenseRole.AddOn;
                break;
            default:
                return null;
        }

        strings.TryGetValue("vendorTag", out var vendorTag);
        if (vendorTag is not null && !VendorTagRule.IsValid(vendorTag))
        {
            return null;
        }

        if (!IssuedPattern().IsMatch(issuedText)
            || !DateTime.TryParseExact(issuedText, IssuedFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var issued))
        {
            return null;
        }

        DateOnly? expires = null;
        if (strings.TryGetValue("expires", out var expiresText))
        {
            if (!ExpiresPattern().IsMatch(expiresText)
                || !DateOnly.TryParseExact(expiresText, ExpiresFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var expiresDate))
            {
                return null;
            }

            expires = expiresDate;
        }

        return new LicensePayload(
            signingKeyId,
            productId,
            role,
            vendorTag,
            new DateTimeOffset(DateTime.SpecifyKind(issued, DateTimeKind.Utc)),
            expires,
            features);
    }
}

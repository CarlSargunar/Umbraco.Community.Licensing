using System.Globalization;
using System.Text.Json;

namespace Umbraco.Community.Licensing.Core;

/// <summary>
/// Writes the payload as compact UTF-8 JSON (ADR-0002): fixed field order, optional fields and empty
/// <c>features</c> omitted, numbers as raw normalised text (never <see cref="double"/>), dates as
/// <c>yyyy-MM-ddTHH:mm:ssZ</c>, default encoder (non-ASCII escaped). The writer trusts its input: the issuer
/// validates with <see cref="ContentRules"/> first. Input that cannot be written correctly (a number failing
/// <see cref="NumberRule.TryNormalise"/>, a date with a fraction of a second, a missing name or text) is a
/// programming error and throws <see cref="InvalidOperationException"/>.
/// </summary>
internal static class PayloadWriter
{
    private const string DateFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    public static byte[] Write(LicensePayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("signingKeyId", payload.SigningKeyId);
            writer.WriteString("product", payload.Product);
            writer.WriteString("issued", FormatDate(payload.Issued, "issued"));
            if (payload.Expires is { } expires)
            {
                writer.WriteString("expires", FormatDate(expires, "expires"));
            }

            if (payload.DisplayName is not null)
            {
                writer.WriteString("displayName", payload.DisplayName);
            }

            if (payload.VendorTag is not null)
            {
                writer.WriteString("vendorTag", payload.VendorTag);
            }

            if (payload.Features.Count > 0)
            {
                writer.WriteStartObject("features");
                foreach (var feature in payload.Features)
                {
                    WriteFeature(writer, feature);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    private static void WriteFeature(Utf8JsonWriter writer, FeatureInput feature)
    {
        if (feature.Name is null)
        {
            throw new InvalidOperationException("A feature has no name.");
        }

        writer.WritePropertyName(feature.Name);
        switch (feature.Kind)
        {
            case FeatureKind.Switch:
                if (!feature.IsOn)
                {
                    throw new InvalidOperationException($"The switch {feature.Name} is off; a switch is written only when on.");
                }

                writer.WriteBooleanValue(true);
                break;

            case FeatureKind.Number:
                if (!NumberRule.TryNormalise(feature.Number, out var text, out var reason))
                {
                    throw new InvalidOperationException($"The number {feature.Name} cannot be written: {reason}");
                }

                writer.WriteRawValue(text, skipInputValidation: false);
                break;

            default:
                writer.WriteStringValue(feature.Text
                    ?? throw new InvalidOperationException($"The text {feature.Name} has no value."));
                break;
        }
    }

    private static string FormatDate(DateTimeOffset value, string field)
    {
        var utc = value.UtcDateTime;
        if (utc.Ticks % TimeSpan.TicksPerSecond != 0)
        {
            throw new InvalidOperationException($"The {field} time has a fraction of a second; the payload carries whole seconds.");
        }

        return utc.ToString(DateFormat, CultureInfo.InvariantCulture);
    }
}

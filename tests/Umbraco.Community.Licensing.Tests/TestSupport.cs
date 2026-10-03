using System.Globalization;
using System.Text;
using Umbraco.Community.Licensing.Internal;

namespace Umbraco.Community.Licensing.Tests;

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public static FixedClock At(string utc) =>
        new(DateTimeOffset.Parse(utc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));

    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>One vendor: a signing key pair, an issuer and an evaluator trusting the key.</summary>
internal sealed class Vendor : IDisposable
{
    public SigningKeyPair Keys { get; } = SigningKeyPair.Create();

    public FixedClock Clock { get; } = FixedClock.At("2026-10-01T12:00:00Z");

    public TrustedSigningKeys Trusted { get; }

    public Vendor()
    {
        Trusted = new TrustedSigningKeys([Keys.PublicKey]);
    }

    public IssuedLicenseKey Issue(
        string product,
        LicenseRole role,
        string? at = null,
        string? reference = null,
        string? expires = null,
        string? vendorTag = null,
        params LicenseFeature[] features)
    {
        var issuer = new LicenseIssuer(at is null ? Clock : FixedClock.At(at));
        return issuer.Issue(
            new LicenseRequest
            {
                ProductId = product,
                Role = role,
                Reference = reference,
                VendorTag = vendorTag,
                Expires = expires is null ? null : DateOnly.Parse(expires, CultureInfo.InvariantCulture),
                Features = features,
            },
            Keys.PrivateKey);
    }

    public LicenseEvaluation Evaluate(string product, params string?[] keys) =>
        new LicenseEvaluator(Trusted, Clock).Evaluate(product, keys);

    public LicenseEvaluation EvaluateAt(string now, string product, params string?[] keys) =>
        new LicenseEvaluator(Trusted, FixedClock.At(now)).Evaluate(product, keys);

    /// <summary>Signs an arbitrary payload, as a faulty tool outside the generation API would.</summary>
    public string SignRaw(string payloadJson, string identifier = "LIC-8F3AK-M7RXB-7Q2D") =>
        KeyString.Sign(identifier, Encoding.UTF8.GetBytes(payloadJson), Keys.PrivateKey);

    public string Payload(string extra = "", string product = "acme.commerce", string role = "base") =>
        $"{{\"signingKeyId\":\"{Keys.SigningKeyId}\",\"product\":\"{product}\",\"role\":\"{role}\",\"issued\":\"2026-03-01T09:14:22Z\"{extra}}}";

    public void Dispose() => Keys.Dispose();
}

internal static class KeyText
{
    public static string PayloadJson(string keyString) =>
        Encoding.UTF8.GetString(System.Buffers.Text.Base64Url.DecodeFromChars(keyString.Split('.')[1]));

    /// <summary>Replaces the payload segment, keeping identifier and signature.</summary>
    public static string WithPayload(string keyString, string payloadJson)
    {
        var parts = keyString.Split('.');
        return parts[0] + "." + Base64UrlText.Encode(Encoding.UTF8.GetBytes(payloadJson)) + "." + parts[2];
    }
}

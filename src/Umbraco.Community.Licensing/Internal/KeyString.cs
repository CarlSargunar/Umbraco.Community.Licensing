using System.Text;

namespace Umbraco.Community.Licensing.Internal;

/// <summary>
/// A supplied string read up to the routing claims (ADR-0001, Reading a supplied string, steps
/// 1 to 4). <see cref="Identifier"/> is set whenever the start of the string is an identifier,
/// whether or not the rest is readable.
/// </summary>
internal sealed record KeyStringReadResult(
    string? Identifier,
    bool IsReadable,
    string SigningInput,
    byte[] Signature,
    PayloadReadResult? Payload)
{
    public static KeyStringReadResult Unreadable(string? identifier) => new(identifier, false, "", [], null);
}

internal static class KeyString
{
    private const char Separator = '.';

    /// <summary>Builds and signs a key string: <c>identifier.base64url(payload).base64url(signature)</c>.</summary>
    public static string Sign(string identifier, byte[] payload, SigningPrivateKey signingKey)
    {
        var signingInput = identifier + Separator + Base64UrlText.Encode(payload);
        var signature = signingKey.Sign(Encoding.ASCII.GetBytes(signingInput));
        return signingInput + Separator + Base64UrlText.Encode(signature);
    }

    /// <summary>Reads a supplied string. Never throws.</summary>
    public static KeyStringReadResult Read(string? supplied)
    {
        // Step 1: remove all whitespace (PDR-0021).
        var text = supplied is null ? "" : string.Concat(supplied.Where(c => !char.IsWhiteSpace(c)));

        // Step 2: the identifier, only on a full match of the first segment (PDR-0020).
        var firstDot = text.IndexOf(Separator);
        var first = firstDot < 0 ? text : text[..firstDot];
        var identifier = LicenseReferenceRule.IsKeyIdentifier(first) ? first : null;
        if (identifier is null)
        {
            return KeyStringReadResult.Unreadable(null);
        }

        // Step 3: three non-empty base64url segments; a 64-byte signature.
        var segments = text.Split(Separator);
        if (segments.Length != 3
            || !Base64UrlText.TryDecode(segments[1], out var payloadBytes)
            || !Base64UrlText.TryDecode(segments[2], out var signature)
            || signature.Length != P256.SignatureLength)
        {
            return KeyStringReadResult.Unreadable(identifier);
        }

        // Step 4: routing claims, product and signingKeyId as strings.
        var payload = PayloadFormat.Read(payloadBytes);
        if (!payload.HasRoutingClaims)
        {
            return KeyStringReadResult.Unreadable(identifier);
        }

        return new KeyStringReadResult(identifier, true, segments[0] + Separator + segments[1], signature, payload);
    }
}

using System.Security.Cryptography;

namespace Umbraco.Community.Licensing.Internal;

/// <summary>ECDSA P-256 with SHA-256, IEEE P1363 signatures (ADR-0001).</summary>
internal static class P256
{
    public const int SignatureLength = 64;
    private const string CurveOid = "1.2.840.10045.3.1.7";
    private const int SigningKeyIdBytes = 8;

    public static bool IsP256(ECDsa key)
    {
        var curve = key.ExportParameters(false).Curve;
        if (!curve.IsNamed)
        {
            return false;
        }

        return curve.Oid.Value == CurveOid
            || string.Equals(curve.Oid.FriendlyName, ECCurve.NamedCurves.nistP256.Oid.FriendlyName, StringComparison.Ordinal);
    }

    /// <summary>
    /// The signing key ID: first 8 bytes of SHA-256 over the SubjectPublicKeyInfo DER, base64url,
    /// 11 characters (ADR-0001).
    /// </summary>
    public static string SigningKeyId(ReadOnlySpan<byte> subjectPublicKeyInfo)
    {
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(subjectPublicKeyInfo, hash);
        return Base64UrlText.Encode(hash[..SigningKeyIdBytes]);
    }

    public static byte[] Sign(ECDsa key, ReadOnlySpan<byte> data) =>
        key.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

    public static bool Verify(ECDsa key, ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature) =>
        signature.Length == SignatureLength
        && key.VerifyData(data, signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
}

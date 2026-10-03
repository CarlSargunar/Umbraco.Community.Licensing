using System.Security.Cryptography;
using Umbraco.Community.Licensing.Internal;

namespace Umbraco.Community.Licensing;

/// <summary>
/// The public half of a signing key pair: shipped inside a product to verify license keys.
/// Its <see cref="SigningKeyId"/> is derived from the key itself, so the exported key carries it.
/// </summary>
public sealed class SigningPublicKey : IEquatable<SigningPublicKey>
{
    private readonly byte[] _subjectPublicKeyInfo;

    private SigningPublicKey(byte[] subjectPublicKeyInfo)
        : this(subjectPublicKeyInfo, P256.SigningKeyId(subjectPublicKeyInfo))
    {
    }

    /// <summary>With an explicit ID: only for testing the 64-bit collision path.</summary>
    internal SigningPublicKey(byte[] subjectPublicKeyInfo, string signingKeyId)
    {
        _subjectPublicKeyInfo = subjectPublicKeyInfo;
        SigningKeyId = signingKeyId;
    }

    /// <summary>The signing key ID (ADR-0001): 11 characters, derived from the public key.</summary>
    public string SigningKeyId { get; }

    /// <summary>Reads a PEM <c>PUBLIC KEY</c> (SubjectPublicKeyInfo) block for an ECDSA P-256 key.</summary>
    /// <exception cref="ArgumentException">The text is not a P-256 public key.</exception>
    public static SigningPublicKey FromPem(string pem)
    {
        ArgumentNullException.ThrowIfNull(pem);
        using var key = ECDsa.Create();
        try
        {
            key.ImportFromPem(pem);
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            throw new ArgumentException("Not a PEM-encoded ECDSA public key.", nameof(pem), ex);
        }

        return FromKey(key, nameof(pem));
    }

    /// <summary>Reads a DER-encoded SubjectPublicKeyInfo for an ECDSA P-256 key.</summary>
    /// <exception cref="ArgumentException">The bytes are not a P-256 public key.</exception>
    public static SigningPublicKey FromSubjectPublicKeyInfo(ReadOnlySpan<byte> subjectPublicKeyInfo)
    {
        using var key = ECDsa.Create();
        try
        {
            key.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out var read);
            if (read != subjectPublicKeyInfo.Length)
            {
                throw new ArgumentException("Trailing data after the public key.", nameof(subjectPublicKeyInfo));
            }
        }
        catch (CryptographicException ex)
        {
            throw new ArgumentException("Not a DER-encoded ECDSA public key.", nameof(subjectPublicKeyInfo), ex);
        }

        return FromKey(key, nameof(subjectPublicKeyInfo));
    }

    internal static SigningPublicKey FromKey(ECDsa key, string parameterName)
    {
        if (!P256.IsP256(key))
        {
            throw new ArgumentException("The key is not on the P-256 curve.", parameterName);
        }

        return new SigningPublicKey(key.ExportSubjectPublicKeyInfo());
    }

    /// <summary>Exports the key as a PEM <c>PUBLIC KEY</c> block. Contains no private key material.</summary>
    public string ExportPem() => PemEncoding.WriteString("PUBLIC KEY", _subjectPublicKeyInfo);

    /// <summary>Exports the key as DER-encoded SubjectPublicKeyInfo.</summary>
    public byte[] ExportSubjectPublicKeyInfo() => (byte[])_subjectPublicKeyInfo.Clone();

    internal bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
    {
        // A new instance per call: ECDsa instances are not documented as thread-safe.
        using var key = ECDsa.Create();
        key.ImportSubjectPublicKeyInfo(_subjectPublicKeyInfo, out _);
        try
        {
            return P256.Verify(key, data, signature);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool Equals(SigningPublicKey? other) =>
        other is not null && _subjectPublicKeyInfo.AsSpan().SequenceEqual(other._subjectPublicKeyInfo);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SigningPublicKey);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(SigningKeyId);

    /// <inheritdoc />
    public override string ToString() => SigningKeyId;
}

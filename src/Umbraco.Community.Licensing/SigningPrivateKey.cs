using System.Security.Cryptography;
using Umbraco.Community.Licensing.Internal;

namespace Umbraco.Community.Licensing;

/// <summary>
/// The private half of a signing key pair, used only by the issuer. Never ship it inside a
/// product. The caller owns it: the library does not keep it after a call returns.
/// <see cref="Dispose"/> clears the key material held by this instance.
/// </summary>
public sealed class SigningPrivateKey : IDisposable
{
    private readonly byte[] _pkcs8;
    private bool _disposed;

    private SigningPrivateKey(byte[] pkcs8, SigningPublicKey publicKey)
    {
        _pkcs8 = pkcs8;
        PublicKey = publicKey;
    }

    /// <summary>The signing key ID of this key pair, derived from its public key.</summary>
    public string SigningKeyId => PublicKey.SigningKeyId;

    /// <summary>The matching public key.</summary>
    public SigningPublicKey PublicKey { get; }

    /// <summary>Reads a PEM <c>PRIVATE KEY</c> (PKCS#8) or <c>EC PRIVATE KEY</c> block for an ECDSA P-256 key.</summary>
    /// <exception cref="ArgumentException">The text is not an unencrypted P-256 private key.</exception>
    public static SigningPrivateKey FromPem(string pem)
    {
        ArgumentNullException.ThrowIfNull(pem);
        using var key = ECDsa.Create();
        try
        {
            key.ImportFromPem(pem);
            _ = key.ExportParameters(true);
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            throw new ArgumentException("Not a PEM-encoded ECDSA private key.", nameof(pem), ex);
        }

        return FromKey(key, nameof(pem));
    }

    /// <summary>Reads a DER-encoded PKCS#8 private key for an ECDSA P-256 key.</summary>
    /// <exception cref="ArgumentException">The bytes are not a P-256 private key.</exception>
    public static SigningPrivateKey FromPkcs8(ReadOnlySpan<byte> pkcs8)
    {
        using var key = ECDsa.Create();
        try
        {
            key.ImportPkcs8PrivateKey(pkcs8, out _);
        }
        catch (CryptographicException ex)
        {
            throw new ArgumentException("Not a DER-encoded PKCS#8 ECDSA private key.", nameof(pkcs8), ex);
        }

        return FromKey(key, nameof(pkcs8));
    }

    internal static SigningPrivateKey FromKey(ECDsa key, string parameterName)
    {
        var publicKey = SigningPublicKey.FromKey(key, parameterName);
        return new SigningPrivateKey(key.ExportPkcs8PrivateKey(), publicKey);
    }

    /// <summary>Exports the key as an unencrypted PEM <c>PRIVATE KEY</c> (PKCS#8) block, for custody.</summary>
    public string ExportPkcs8Pem()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return PemEncoding.WriteString("PRIVATE KEY", _pkcs8);
    }

    /// <summary>Exports the key as DER-encoded PKCS#8, for custody.</summary>
    public byte[] ExportPkcs8()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return (byte[])_pkcs8.Clone();
    }

    internal byte[] Sign(ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        using var key = ECDsa.Create();
        key.ImportPkcs8PrivateKey(_pkcs8, out _);
        return P256.Sign(key, data);
    }

    /// <summary>Clears the key material held by this instance.</summary>
    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(_pkcs8);
        _disposed = true;
    }

    /// <inheritdoc />
    public override string ToString() => SigningKeyId;
}

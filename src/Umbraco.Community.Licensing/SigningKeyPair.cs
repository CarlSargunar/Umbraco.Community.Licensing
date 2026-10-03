using System.Security.Cryptography;

namespace Umbraco.Community.Licensing;

/// <summary>
/// A new signing key pair. Keep <see cref="PrivateKey"/> in custody for issuing; ship only
/// <see cref="PublicKey"/> inside the product.
/// </summary>
public sealed class SigningKeyPair : IDisposable
{
    private SigningKeyPair(SigningPrivateKey privateKey)
    {
        PrivateKey = privateKey;
    }

    /// <summary>The private key, for the issuer only.</summary>
    public SigningPrivateKey PrivateKey { get; }

    /// <summary>The public key, for the product.</summary>
    public SigningPublicKey PublicKey => PrivateKey.PublicKey;

    /// <summary>The signing key ID, derived from the public key.</summary>
    public string SigningKeyId => PublicKey.SigningKeyId;

    /// <summary>Creates a new ECDSA P-256 signing key pair.</summary>
    public static SigningKeyPair Create()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return new SigningKeyPair(SigningPrivateKey.FromKey(key, "key"));
    }

    /// <summary>Clears the private key material.</summary>
    public void Dispose() => PrivateKey.Dispose();
}

using System.Security.Cryptography;

namespace Umbraco.Community.Licensing.Tests;

/// <summary>tasks.md section 4: signing-key management.</summary>
public sealed class SigningKeyTests
{
    // 4.1

    [Fact]
    public void KeyPair_PartsExportableSeparately()
    {
        using var pair = SigningKeyPair.Create();

        var publicPem = pair.PublicKey.ExportPem();
        var privatePem = pair.PrivateKey.ExportPkcs8Pem();

        Assert.StartsWith("-----BEGIN PUBLIC KEY-----", publicPem);
        Assert.StartsWith("-----BEGIN PRIVATE KEY-----", privatePem);
        Assert.Equal(pair.SigningKeyId, SigningPublicKey.FromPem(publicPem).SigningKeyId);
        using var privateKey = SigningPrivateKey.FromPem(privatePem);
        Assert.Equal(pair.SigningKeyId, privateKey.SigningKeyId);
    }

    // Private key read back from custody as DER (signing-key-management spec "Create a signing key pair").
    [Fact]
    public void PrivateKey_Pkcs8DerRoundTrips()
    {
        using var vendor = new Vendor();
        using var reimported = SigningPrivateKey.FromPkcs8(vendor.Keys.PrivateKey.ExportPkcs8());

        var key = new LicenseIssuer(vendor.Clock).Issue(
            new LicenseRequest { ProductId = "acme.commerce", Role = LicenseRole.Base }, reimported).KeyString;

        Assert.Equal(vendor.Keys.SigningKeyId, reimported.SigningKeyId);
        Assert.Equal(LicenseKeyState.Valid, vendor.Evaluate("acme.commerce", key).Rows[0].State);
    }

    [Fact]
    public void PublicExport_CarriesSigningKeyIdAndNoPrivateKey()
    {
        using var pair = SigningKeyPair.Create();
        var publicPem = pair.PublicKey.ExportPem();
        var publicDer = pair.PublicKey.ExportSubjectPublicKeyInfo();

        Assert.Equal(pair.SigningKeyId, SigningPublicKey.FromSubjectPublicKeyInfo(publicDer).SigningKeyId);
        Assert.Throws<ArgumentException>(() => SigningPrivateKey.FromPem(publicPem));

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(publicPem);
        Assert.Throws<CryptographicException>(() => ecdsa.ExportParameters(includePrivateParameters: true));

        var privateScalar = ECDsaFrom(pair.PrivateKey).ExportParameters(true).D!;
        Assert.False(publicDer.AsSpan().IndexOf(privateScalar) >= 0);
    }

    [Fact]
    public void Import_RejectsOtherCurves()
    {
        using var p384 = ECDsa.Create(ECCurve.NamedCurves.nistP384);

        Assert.Throws<ArgumentException>(() => SigningPublicKey.FromPem(p384.ExportSubjectPublicKeyInfoPem()));
        Assert.Throws<ArgumentException>(() => SigningPrivateKey.FromPem(p384.ExportPkcs8PrivateKeyPem()));
        Assert.Throws<ArgumentException>(() => SigningPublicKey.FromPem("not a key"));
    }

    [Fact]
    public void PrivateKey_DisposeClearsMaterial()
    {
        var pair = SigningKeyPair.Create();
        pair.Dispose();

        Assert.Throws<ObjectDisposedException>(() => pair.PrivateKey.ExportPkcs8());
    }

    // 4.2

    [Fact]
    public void TrustedSet_AddResolveRemove()
    {
        using var a = SigningKeyPair.Create();
        using var b = SigningKeyPair.Create();
        var trusted = new TrustedSigningKeys();

        trusted.Add(a.PublicKey);
        trusted.Add(b.PublicKey);
        trusted.Add(SigningPublicKey.FromPem(a.PublicKey.ExportPem()));

        Assert.Equal(2, trusted.Count);
        Assert.True(trusted.TryGet(a.SigningKeyId, out var resolved));
        Assert.Equal(a.PublicKey, resolved);
        Assert.True(trusted.Remove(a.SigningKeyId));
        Assert.False(trusted.TryGet(a.SigningKeyId, out _));
        Assert.False(trusted.Remove(a.SigningKeyId));
        Assert.Single(trusted);
    }

    [Fact]
    public void TrustedSet_RejectsSameIdForDifferentKey()
    {
        using var a = SigningKeyPair.Create();
        using var b = SigningKeyPair.Create();
        var trusted = new TrustedSigningKeys([a.PublicKey]);
        var collision = new SigningPublicKey(b.PublicKey.ExportSubjectPublicKeyInfo(), a.SigningKeyId);

        Assert.Throws<InvalidOperationException>(() => trusted.Add(collision));
        Assert.Equal(1, trusted.Count);
        Assert.True(trusted.TryGet(a.SigningKeyId, out var held));
        Assert.Equal(a.PublicKey, held);
    }

    [Fact]
    public void TrustedSet_SeveralKeysVerify()
    {
        using var a = new Vendor();
        using var b = new Vendor();
        var trusted = new TrustedSigningKeys([a.Keys.PublicKey, b.Keys.PublicKey]);
        var evaluator = new LicenseEvaluator(trusted, a.Clock);

        var rows = evaluator.Evaluate("acme.commerce",
        [
            a.Issue("acme.commerce", LicenseRole.Base).KeyString,
            b.Issue("acme.commerce", LicenseRole.Base).KeyString,
        ]).Rows;

        Assert.All(rows, r => Assert.Equal(LicenseKeyState.Valid, r.State));
    }

    private static ECDsa ECDsaFrom(SigningPrivateKey key)
    {
        var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(key.ExportPkcs8(), out _);
        return ecdsa;
    }
}

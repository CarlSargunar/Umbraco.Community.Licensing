namespace Umbraco.Community.Licensing.Tests;

/// <summary>tasks.md 8.2: rotating signing keys (signing-key-management spec).</summary>
public sealed class RotationTests
{
    [Fact]
    public void AddNewKeyThenWithdrawOld()
    {
        using var a = SigningKeyPair.Create();
        using var b = SigningKeyPair.Create();
        var clock = FixedClock.At("2026-10-01T12:00:00Z");
        var key = new LicenseIssuer(clock).Issue(
            new LicenseRequest { ProductId = "acme.commerce", Role = LicenseRole.Base }, a.PrivateKey).KeyString;
        var trusted = new TrustedSigningKeys([a.PublicKey]);
        var evaluator = new LicenseEvaluator(trusted, clock);

        trusted.Add(b.PublicKey);
        var withBoth = evaluator.Evaluate("acme.commerce", [key]);

        Assert.Equal(LicenseKeyState.Valid, withBoth.Rows[0].State);
        Assert.Equal(a.SigningKeyId, withBoth.Rows[0].License!.SigningKeyId);
        Assert.True(withBoth.Product.IsLicensed);

        trusted.Remove(a.SigningKeyId);
        var withdrawn = evaluator.Evaluate("acme.commerce", [key]);

        Assert.Equal(LicenseKeyState.SigningKeyNotRecognised, withdrawn.Rows[0].State);
        Assert.False(withdrawn.Product.IsLicensed);

        var reissued = new LicenseIssuer(clock).Issue(
            new LicenseRequest { ProductId = "acme.commerce", Role = LicenseRole.Base }, b.PrivateKey).KeyString;
        Assert.Equal(LicenseKeyState.Valid, evaluator.Evaluate("acme.commerce", [reissued]).Rows[0].State);
    }
}

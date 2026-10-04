using System.Reflection;

namespace Umbraco.Community.Licensing.Tests;

/// <summary>tasks.md section 5: license generation.</summary>
public sealed class IssuerTests : IDisposable
{
    private readonly Vendor _vendor = new();

    public void Dispose() => _vendor.Dispose();

    // 5.1

    [Fact]
    public void MinimalBaseLicense()
    {
        var issued = _vendor.Issue("acme.seo-toolkit", LicenseRole.Base);

        var license = _vendor.Evaluate("acme.seo-toolkit", issued.KeyString).Rows[0].License!;

        Assert.Matches(@"^LIC-[2-9A-HJKMNP-Z]{5}-[2-9A-HJKMNP-Z]{5}\z", issued.Reference);
        Assert.Equal(issued.Reference, license.Reference);
        Assert.Equal(issued.KeyIdentifier, license.KeyIdentifier);
        Assert.Null(license.Expires);
        Assert.Null(license.VendorTag);
        Assert.Empty(license.Features);
        Assert.Equal(LicenseRole.Base, license.Role);
    }

    [Fact]
    public void FullContentsReportedExactly()
    {
        var issued = _vendor.Issue("acme.commerce", LicenseRole.AddOn, expires: "2027-03-01", vendorTag: "SHOP-2026-000123",
            features: [LicenseFeature.Switch("ecommerce"), LicenseFeature.Number("max-orders", 500m), LicenseFeature.Text("licensed-domain", "example.com")]);
        var baseKey = _vendor.Issue("acme.commerce", LicenseRole.Base);

        var row = _vendor.Evaluate("acme.commerce", issued.KeyString, baseKey.KeyString).Rows[0];

        Assert.True(row.IsVerified);
        Assert.Equal(LicenseKeyState.Valid, row.State);
        var license = row.License!;
        Assert.Equal("acme.commerce", license.ProductId);
        Assert.Equal(LicenseRole.AddOn, license.Role);
        Assert.Equal(new DateOnly(2027, 3, 1), license.Expires);
        Assert.Equal("SHOP-2026-000123", license.VendorTag);
        Assert.Equal(issued.Issued, license.Issued);
        Assert.Equal(_vendor.Keys.SigningKeyId, license.SigningKeyId);
        Assert.Equal(3, license.Features.Count);
        Assert.Equal(FeatureValue.Switch, license.Features["ecommerce"]);
        Assert.Equal(500m, license.Features["max-orders"].Number);
        Assert.Equal("example.com", license.Features["licensed-domain"].Text);
    }

    [Fact]
    public void FirstIssueGeneratesReference_ReissueKeepsIt()
    {
        var first = _vendor.Issue("acme.commerce", LicenseRole.Base, at: "2026-03-01T09:14:00Z");
        var reissue = _vendor.Issue("acme.commerce", LicenseRole.Base, at: "2026-03-01T10:21:00Z",
            reference: first.Reference.ToLowerInvariant().Replace('-', ' '));

        Assert.Equal(first.Reference, reissue.Reference);
        Assert.StartsWith(reissue.Reference + "-", reissue.KeyIdentifier);
        Assert.NotEqual(first.Issued, reissue.Issued);
        Assert.NotEqual(_vendor.Issue("acme.commerce", LicenseRole.Base).Reference, first.Reference);
    }

    [Fact]
    public void ReissueGetsOwnKeyPart()
    {
        var parts = Enumerable.Range(0, 20)
            .Select(_ => _vendor.Issue("acme.commerce", LicenseRole.Base, reference: "LIC-8F3AK-M7RXB").KeyIdentifier)
            .ToList();

        Assert.All(parts, p => Assert.StartsWith("LIC-8F3AK-M7RXB-", p));
        Assert.True(parts.Distinct().Count() > 1);
    }

    [Fact]
    public void Reissue_SpecReference()
    {
        var issued = _vendor.Issue("acme.commerce", LicenseRole.Base, reference: "lic-8f3ak m7rxb");

        Assert.Equal("LIC-8F3AK-M7RXB", issued.Reference);
        Assert.StartsWith("LIC-8F3AK-M7RXB-", issued.KeyString);
    }

    [Fact]
    public void IssueTimeTruncatedToSecond()
    {
        var issued = _vendor.Issue("acme.commerce", LicenseRole.Base, at: "2026-09-28T14:30:22.734Z");

        Assert.Equal(new DateTimeOffset(2026, 9, 28, 14, 30, 22, TimeSpan.Zero), issued.Issued);
        Assert.Contains("\"issued\":\"2026-09-28T14:30:22Z\"", KeyText.PayloadJson(issued.KeyString));
    }

    [Fact]
    public void IssueTimeIsUtcFromNonUtcClock()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 9, 28, 16, 30, 22, TimeSpan.FromHours(2)));
        var issued = new LicenseIssuer(clock).Issue(
            new LicenseRequest { ProductId = "acme.commerce", Role = LicenseRole.Base }, _vendor.Keys.PrivateKey);

        Assert.Equal(TimeSpan.Zero, issued.Issued.Offset);
        Assert.Contains("\"issued\":\"2026-09-28T14:30:22Z\"", KeyText.PayloadJson(issued.KeyString));
    }

    [Fact]
    public void NoWayToSupplyIssueTimeOrKeyPart()
    {
        var names = typeof(LicenseRequest).GetProperties().Select(p => p.Name).ToList();

        Assert.Equal(["ProductId", "Role", "Reference", "VendorTag", "Expires", "Features"], names);
    }

    [Fact]
    public void VendorTag_SignedUnchanged()
    {
        var issued = _vendor.Issue("acme.commerce", LicenseRole.Base, vendorTag: "SHOP-2026-000123");

        Assert.Equal("SHOP-2026-000123", _vendor.Evaluate("acme.commerce", issued.KeyString).Rows[0].License!.VendorTag);
    }

    [Fact]
    public void VendorTag_SameTagOnTwoKeys()
    {
        var a = _vendor.Issue("acme.commerce", LicenseRole.Base, vendorTag: "#1001");
        var b = _vendor.Issue("acme.commerce", LicenseRole.AddOn, vendorTag: "#1001");

        Assert.NotEqual(a.KeyString, b.KeyString);
    }

    [Fact]
    public void VendorTag_NoneAndReissueWithoutTagCarriesNone()
    {
        var first = _vendor.Issue("acme.commerce", LicenseRole.Base, at: "2026-03-01T09:14:00Z", vendorTag: "SHOP-2025-000087");
        var reissue = _vendor.Issue("acme.commerce", LicenseRole.Base, at: "2026-03-02T09:14:00Z", reference: first.Reference);

        var rows = _vendor.Evaluate("acme.commerce", first.KeyString, reissue.KeyString).Rows;

        Assert.DoesNotContain("vendorTag", KeyText.PayloadJson(reissue.KeyString));
        Assert.Null(rows[1].License!.VendorTag);
        Assert.Equal("SHOP-2025-000087", rows[0].License!.VendorTag);
    }

    // 5.2

    public static TheoryData<LicenseRequest, string, string> InvalidRequests => new()
    {
        { new LicenseRequest { ProductId = "acme.commerce" }, "Role", "required" },
        { new LicenseRequest { ProductId = "acme.commerce", Role = (LicenseRole)7 }, "Role", "base or add-on" },
        { new LicenseRequest { ProductId = "commerce", Role = LicenseRole.Base }, "ProductId", "vendor.product" },
        { new LicenseRequest { ProductId = "Acme.Commerce", Role = LicenseRole.Base }, "ProductId", "vendor.product" },
        { new LicenseRequest { Role = LicenseRole.Base }, "ProductId", "vendor.product" },
        { new LicenseRequest { ProductId = "acme.commerce", Role = LicenseRole.Base, Reference = "LIC-8F3AK-M7RXO" }, "Reference", "LIC-XXXXX-XXXXX" },
        { new LicenseRequest { ProductId = "acme.commerce", Role = LicenseRole.Base, Reference = "LIC-8F3AK" }, "Reference", "LIC-XXXXX-XXXXX" },
        { new LicenseRequest { ProductId = "acme.commerce", Role = LicenseRole.Base, VendorTag = "jane@acme.com" }, "VendorTag", "1 to 64" },
        { new LicenseRequest { ProductId = "acme.commerce", Role = LicenseRole.Base, VendorTag = "" }, "VendorTag", "1 to 64" },
        { new LicenseRequest { ProductId = "acme.commerce", Role = LicenseRole.Base, Expires = new DateOnly(2026, 9, 30) }, "Expires", "UTC date" },
        { new LicenseRequest { ProductId = "acme.commerce", Role = LicenseRole.Base, Features = [LicenseFeature.Number("max-orders", -200m)] }, "Features", "positive" },
        { new LicenseRequest { ProductId = "acme.commerce", Role = LicenseRole.Base, Features = [null!] }, "Features", "must not contain null" },
    };

    // license-generation spec "Rejection reports every problem": the message names the rule, not only the field.
    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void EachRuleRejected(LicenseRequest request, string field, string rule)
    {
        var issuer = new LicenseIssuer(FixedClock.At("2026-10-01T12:00:00Z"));

        var error = Assert.Throws<LicenseRequestException>(() => issuer.Issue(request, _vendor.Keys.PrivateKey));

        var problem = Assert.Single(error.Problems);
        Assert.Equal(field, problem.Field);
        Assert.Contains(rule, problem.Message);
        Assert.Contains(field, error.Message);
    }

    [Theory]
    [InlineData("2026-10-01T00:00:00Z")]
    [InlineData("2026-10-01T23:59:59Z")]
    public void ExpiryTodayAccepted(string now)
    {
        var issued = _vendor.Issue("acme.commerce", LicenseRole.Base, at: now, expires: "2026-10-01");

        Assert.NotNull(issued.KeyString);
    }

    [Fact]
    public void ExpiryCheckedAgainstUtcDate()
    {
        // 2026-10-01T01:00+02:00 is 2026-09-30T23:00Z: 2026-09-30 is today in UTC.
        var clock = new FixedClock(new DateTimeOffset(2026, 10, 1, 1, 0, 0, TimeSpan.FromHours(2)));

        var issued = new LicenseIssuer(clock).Issue(
            new LicenseRequest { ProductId = "acme.commerce", Role = LicenseRole.Base, Expires = new DateOnly(2026, 9, 30) },
            _vendor.Keys.PrivateKey);

        Assert.NotNull(issued.KeyString);
    }

    [Fact]
    public void SeveralProblemsAllReported()
    {
        var error = Assert.Throws<LicenseRequestException>(() => new LicenseIssuer(_vendor.Clock).Issue(
            new LicenseRequest
            {
                ProductId = "acme.commerce",
                Features = [LicenseFeature.Number("max-orders", -200m)],
            },
            _vendor.Keys.PrivateKey));

        Assert.Equal(["Role", "Features"], error.Problems.Select(p => p.Field));
    }

    [Fact]
    public void EveryProblemReported()
    {
        var error = Assert.Throws<LicenseRequestException>(() => new LicenseIssuer(_vendor.Clock).Issue(
            new LicenseRequest
            {
                ProductId = "Acme",
                Reference = "nope",
                VendorTag = "a b",
                Expires = new DateOnly(2020, 1, 1),
                Features = [LicenseFeature.Switch("pro", false), LicenseFeature.Text("x", " y")],
            },
            _vendor.Keys.PrivateKey));

        Assert.Equal(["ProductId", "Role", "Reference", "VendorTag", "Expires", "Features", "Features"],
            error.Problems.Select(p => p.Field));
    }

    // 5.3

    [Fact]
    public void IssuerHoldsOnlyItsClock()
    {
        var fields = typeof(LicenseIssuer).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.All(fields, f => Assert.Equal(typeof(TimeProvider), f.FieldType));
    }

    [Fact]
    public void ResultHoldsNoKeyMaterial()
    {
        var issuer = new LicenseIssuer(_vendor.Clock);
        var issued = issuer.Issue(new LicenseRequest { ProductId = "acme.commerce", Role = LicenseRole.Base }, _vendor.Keys.PrivateKey);

        var propertyTypes = typeof(IssuedLicenseKey).GetProperties().Select(p => p.PropertyType).Distinct();
        Assert.All(propertyTypes, t => Assert.True(t == typeof(string) || t == typeof(DateTimeOffset)));
        var der = Convert.ToBase64String(_vendor.Keys.PrivateKey.ExportPkcs8());
        Assert.DoesNotContain(der[^40..], issued.ToString());
    }

    [Fact]
    public void IssuingAfterTheCallerDisposesTheKeyFails()
    {
        var pair = SigningKeyPair.Create();
        var issuer = new LicenseIssuer(_vendor.Clock);
        issuer.Issue(new LicenseRequest { ProductId = "acme.commerce", Role = LicenseRole.Base }, pair.PrivateKey);
        pair.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            issuer.Issue(new LicenseRequest { ProductId = "acme.commerce", Role = LicenseRole.Base }, pair.PrivateKey));
    }
}

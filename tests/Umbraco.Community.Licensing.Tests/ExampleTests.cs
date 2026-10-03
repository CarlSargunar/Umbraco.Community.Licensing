namespace Umbraco.Community.Licensing.Tests;

/// <summary>
/// tasks.md 8.1: every site evaluation in docs/license-examples.md, end to end. Now is
/// 2026-10-01 unless the example states otherwise. Key parts are random, so rows are matched
/// by the identifiers the issuer returned.
/// </summary>
public sealed class ExampleTests : IDisposable
{
    private const string Commerce = "acme.commerce";
    private const string Ref8F3AK = "LIC-8F3AK-M7RXB";
    private readonly Vendor _acme = new();

    public void Dispose() => _acme.Dispose();

    private IssuedLicenseKey Issue(LicenseRole role, string at, string? reference = null, string? expires = null,
        string? vendorTag = null, string product = Commerce, params LicenseFeature[] features) =>
        _acme.Issue(product, role, at, reference, expires, vendorTag, features);

    private static LicenseFeature Orders(decimal n) => LicenseFeature.Number("max-orders", n);

    private static void AssertStates(LicenseEvaluation result, params LicenseKeyState[] states) =>
        Assert.Equal(states, result.Rows.Select(r => r.State));

    private static void AssertFeatures(ProductLicense product, params string[] expected) =>
        Assert.Equal(expected.Order(StringComparer.Ordinal),
            product.Features.Select(f => f.Value.Kind == FeatureKind.Switch ? f.Key : $"{f.Key} {f.Value}"));

    [Fact]
    public void Example01_MinimalLicense()
    {
        var key = Issue(LicenseRole.Base, "2026-01-10T14:02:00Z", reference: "LIC-4HN7T-QW2ZC", product: "acme.seo-toolkit");

        var result = _acme.Evaluate("acme.seo-toolkit", key.KeyString);

        AssertStates(result, LicenseKeyState.Valid);
        Assert.True(result.Product.IsLicensed);
        Assert.False(result.Product.IsGranted("x"));
        Assert.Null(result.Rows[0].License!.Expires);
    }

    [Fact]
    public void Example02_TypicalBaseLicense()
    {
        var key = Issue(LicenseRole.Base, "2026-03-01T09:14:00Z", Ref8F3AK, "2027-03-01", features: [LicenseFeature.Switch("ecommerce"), Orders(500)]);

        var result = _acme.Evaluate(Commerce, key.KeyString);

        AssertStates(result, LicenseKeyState.Valid);
        Assert.Equal(new DateOnly(2027, 3, 1), result.Rows[0].License!.Expires);
        AssertFeatures(result.Product, "ecommerce", "max-orders 500");
        AssertStates(_acme.EvaluateAt("2027-03-01T23:59:59Z", Commerce, key.KeyString), LicenseKeyState.Valid);
    }

    [Fact]
    public void Example03_RenewalSupersedes()
    {
        var old = Issue(LicenseRole.Base, "2025-03-01T10:02:00Z", Ref8F3AK, "2026-03-01", features: Orders(500));
        var renewed = Issue(LicenseRole.Base, "2026-03-01T09:14:00Z", Ref8F3AK, "2027-03-01", features: Orders(500));

        var result = _acme.Evaluate(Commerce, old.KeyString, renewed.KeyString);

        AssertStates(result, LicenseKeyState.Superseded, LicenseKeyState.Valid);
        AssertFeatures(result.Product, "max-orders 500");
    }

    [Fact]
    public void Example04_SameDayCorrection()
    {
        var typo = Issue(LicenseRole.Base, "2026-03-01T09:14:00Z", Ref8F3AK, features: Orders(50));
        var correction = Issue(LicenseRole.Base, "2026-03-01T10:21:00Z", Ref8F3AK, features: Orders(500));

        var result = _acme.Evaluate(Commerce, typo.KeyString, correction.KeyString);

        AssertStates(result, LicenseKeyState.Superseded, LicenseKeyState.Valid);
        AssertFeatures(result.Product, "max-orders 500");
    }

    [Fact]
    public void Example05_ReissueChangesRole()
    {
        LicenseFeature[] features = [LicenseFeature.Switch("ecommerce"), Orders(500)];
        var addOn = Issue(LicenseRole.AddOn, "2026-03-01T09:14:00Z", Ref8F3AK, features: features);
        var baseKey = Issue(LicenseRole.Base, "2026-03-01T10:21:00Z", Ref8F3AK, features: features);

        var fixedResult = _acme.Evaluate(Commerce, addOn.KeyString, baseKey.KeyString);

        AssertStates(fixedResult, LicenseKeyState.Superseded, LicenseKeyState.Valid);
        Assert.Equal(LicenseRole.AddOn, fixedResult.Rows[0].SupersededRoleChange);
        Assert.True(fixedResult.Product.IsLicensed);
        AssertFeatures(fixedResult.Product, "ecommerce", "max-orders 500");

        var original = Issue(LicenseRole.Base, "2026-03-01T09:14:00Z", Ref8F3AK, features: features);
        var mistake = Issue(LicenseRole.AddOn, "2026-09-12T15:30:00Z", Ref8F3AK, features: features);

        var broken = _acme.Evaluate(Commerce, original.KeyString, mistake.KeyString);

        AssertStates(broken, LicenseKeyState.Superseded, LicenseKeyState.Inactive);
        Assert.Equal(LicenseRole.Base, broken.Rows[0].SupersededRoleChange);
        Assert.False(broken.Product.IsLicensed);
        Assert.Empty(broken.Product.Features);
    }

    [Fact]
    public void Example06_WhichKeysMaySupersede()
    {
        // A key that fails verification never supersedes.
        var working = Issue(LicenseRole.Base, "2026-03-01T09:14:00Z", Ref8F3AK, features: Orders(500));
        var upgrade = Issue(LicenseRole.Base, "2026-09-28T09:00:00Z", Ref8F3AK, features: Orders(2000));
        var mangled = Mangle(upgrade.KeyString);

        var failed = _acme.Evaluate(Commerce, working.KeyString, mangled);

        AssertStates(failed, LicenseKeyState.Valid, LicenseKeyState.NotVerified);
        Assert.Equal(upgrade.KeyIdentifier, failed.Rows[1].KeyIdentifier);
        AssertFeatures(failed.Product, "max-orders 500");

        // A key for another product takes no part.
        using var zenith = new Vendor();
        var forms = zenith.Issue("zenith.forms", LicenseRole.Base, "2026-09-30T00:00:00Z", Ref8F3AK);
        var acme = Issue(LicenseRole.Base, "2026-03-01T09:14:00Z", Ref8F3AK);

        var other = _acme.Evaluate(Commerce, acme.KeyString, forms.KeyString);

        AssertStates(other, LicenseKeyState.Valid, LicenseKeyState.WrongProduct);
        Assert.Equal("zenith.forms", other.Rows[1].ClaimedProductId);
        Assert.True(other.Product.IsLicensed);

        // An expired key still supersedes.
        var longTerm = Issue(LicenseRole.Base, "2026-03-01T09:14:00Z", Ref8F3AK, "2099-03-01");
        var sold = Issue(LicenseRole.Base, "2026-03-01T10:21:00Z", Ref8F3AK, "2026-09-01");

        var expired = _acme.Evaluate(Commerce, longTerm.KeyString, sold.KeyString);

        AssertStates(expired, LicenseKeyState.Superseded, LicenseKeyState.Expired);
        Assert.False(expired.Product.IsLicensed);
        Assert.Empty(expired.Product.Features);
    }

    [Fact]
    public void Example07_SameKeyTwiceAndTiedKeys()
    {
        var original = Issue(LicenseRole.Base, "2026-03-01T09:14:07Z", Ref8F3AK, features: Orders(500));

        // The same key supplied twice.
        var twice = _acme.Evaluate(Commerce, original.KeyString, original.KeyString);
        AssertStates(twice, LicenseKeyState.Valid, LicenseKeyState.Duplicate);
        Assert.Equal(0, twice.Rows[1].DuplicateOf);
        Assert.False(twice.Rows[1].HasVendorError);
        AssertFeatures(twice.Product, "max-orders 500");

        // Different keys tied for latest: both count.
        var d4wk = Issue(LicenseRole.Base, "2026-09-28T14:30:22Z", Ref8F3AK, features: Orders(2000));
        var k9px = Issue(LicenseRole.Base, "2026-09-28T14:30:22Z", Ref8F3AK, features: Orders(2000));

        var tied = _acme.Evaluate(Commerce, original.KeyString, d4wk.KeyString, k9px.KeyString);

        AssertStates(tied, LicenseKeyState.Superseded, LicenseKeyState.Valid, LicenseKeyState.Valid);
        Assert.False(tied.Rows[0].HasVendorError);
        Assert.Equal([k9px.KeyIdentifier], tied.Rows[1].TiedWith);
        Assert.Equal([d4wk.KeyIdentifier], tied.Rows[2].TiedWith);
        AssertFeatures(tied.Product, "max-orders 4000");

        // Tied keys that disagree.
        var t6wn = Issue(LicenseRole.Base, "2026-09-28T14:30:22Z", Ref8F3AK, features: Orders(2000));
        var x3bn = Issue(LicenseRole.Base, "2026-09-28T14:30:22Z", Ref8F3AK, features: Orders(500));

        var disagree = _acme.Evaluate(Commerce, original.KeyString, t6wn.KeyString, x3bn.KeyString);

        AssertStates(disagree, LicenseKeyState.Superseded, LicenseKeyState.Valid, LicenseKeyState.Valid);
        Assert.True(disagree.Rows[1].HasVendorError);
        Assert.True(disagree.Rows[2].HasVendorError);
        AssertFeatures(disagree.Product, "max-orders 2500");

        // The fix: a later reissue supersedes both, and the flag is gone.
        var h8rc = Issue(LicenseRole.Base, "2026-09-29T08:02:11Z", Ref8F3AK, features: Orders(2000));

        var fixedResult = _acme.Evaluate(Commerce, original.KeyString, t6wn.KeyString, x3bn.KeyString, h8rc.KeyString);

        AssertStates(fixedResult, LicenseKeyState.Superseded, LicenseKeyState.Superseded, LicenseKeyState.Superseded, LicenseKeyState.Valid);
        Assert.All(fixedResult.Rows, r => Assert.False(r.HasVendorError));
        Assert.All(fixedResult.Rows.Take(3), r => Assert.Equal(3, r.SupersededBy));
        AssertFeatures(fixedResult.Product, "max-orders 2000");
    }

    [Fact]
    public void Example08_AddOnsAloneDoNotLicense()
    {
        var pack = Issue(LicenseRole.AddOn, "2026-06-15T16:40:00Z", "LIC-4Z9BE-T6WNH", "2027-03-01", features: Orders(1000));
        var ai = Issue(LicenseRole.AddOn, "2026-09-01T08:05:00Z", "LIC-77DQS-9YJ4M", "2026-11-01", features: LicenseFeature.Switch("ai-assist"));

        var result = _acme.Evaluate(Commerce, pack.KeyString, ai.KeyString);

        AssertStates(result, LicenseKeyState.Inactive, LicenseKeyState.Inactive);
        Assert.False(result.Product.IsLicensed);
    }

    private string[] Example09Site(string packExpiry = "2027-03-01") =>
    [
        Issue(LicenseRole.Base, "2026-03-01T09:14:00Z", Ref8F3AK, "2027-03-01", features: [LicenseFeature.Switch("ecommerce"), Orders(500)]).KeyString,
        Issue(LicenseRole.AddOn, "2026-06-15T16:40:00Z", "LIC-4Z9BE-T6WNH", packExpiry, features: Orders(1000)).KeyString,
        Issue(LicenseRole.AddOn, "2026-07-01T10:00:00Z", "LIC-2V5C9-HKD3P", packExpiry, features: Orders(500)).KeyString,
        Issue(LicenseRole.AddOn, "2026-09-01T08:05:00Z", "LIC-77DQS-9YJ4M", "2026-11-01", features: LicenseFeature.Switch("ai-assist")).KeyString,
    ];

    [Fact]
    public void Example09_BasePlusAddOns()
    {
        var result = _acme.Evaluate(Commerce, Example09Site());

        AssertStates(result, LicenseKeyState.Valid, LicenseKeyState.Valid, LicenseKeyState.Valid, LicenseKeyState.Valid);
        AssertFeatures(result.Product, "ai-assist", "ecommerce", "max-orders 2000");
        Assert.Equal(1, result.Product.ValidBaseCount);
    }

    [Fact]
    public void Example10_DecimalQuantities()
    {
        const string vault = "acme.media-vault";
        var result = _acme.Evaluate(vault,
            Issue(LicenseRole.Base, "2026-01-01T00:00:00Z", "LIC-9QW3E-RT5YU", product: vault, features: LicenseFeature.Number("storage-gb", 10m)).KeyString,
            Issue(LicenseRole.AddOn, "2026-01-01T00:00:00Z", "LIC-M4K8N-B6VC2", product: vault, features: LicenseFeature.Number("storage-gb", 2.5m)).KeyString,
            Issue(LicenseRole.AddOn, "2026-01-01T00:00:00Z", "LIC-X7Z3H-J9FD4", product: vault, features: LicenseFeature.Number("storage-gb", 2.5m)).KeyString);

        AssertStates(result, LicenseKeyState.Valid, LicenseKeyState.Valid, LicenseKeyState.Valid);
        Assert.Equal(15m, result.Product.GetFeature("storage-gb")!.Number);
        Assert.Equal("15", result.Product.GetFeature("storage-gb")!.ToString());
    }

    [Fact]
    public void Example11_AddOnExpiresBeforeBase()
    {
        var result = _acme.EvaluateAt("2026-11-15T00:00:00Z", Commerce, Example09Site());

        AssertStates(result, LicenseKeyState.Valid, LicenseKeyState.Valid, LicenseKeyState.Valid, LicenseKeyState.Expired);
        AssertFeatures(result.Product, "ecommerce", "max-orders 2000");
    }

    [Fact]
    public void Example12_BaseExpiresBeforeAddOns()
    {
        var site = Example09Site(packExpiry: "2027-06-15")[..3];

        var result = _acme.EvaluateAt("2027-03-02T00:00:00Z", Commerce, site);

        AssertStates(result, LicenseKeyState.Expired, LicenseKeyState.Inactive, LicenseKeyState.Inactive);
        Assert.False(result.Product.IsLicensed);
        Assert.Empty(result.Product.Features);

        // Renewing the base under its reference brings the packs back with no reissue.
        var renewal = Issue(LicenseRole.Base, "2027-03-02T00:00:00Z", Ref8F3AK, "2028-03-01", features: [LicenseFeature.Switch("ecommerce"), Orders(500)]);
        var renewed = _acme.EvaluateAt("2027-03-02T00:00:00Z", Commerce, [.. site, renewal.KeyString]);

        AssertStates(renewed, LicenseKeyState.Superseded, LicenseKeyState.Valid, LicenseKeyState.Valid, LicenseKeyState.Valid);
    }

    [Fact]
    public void Example13_TwoBaseLicenses()
    {
        LicenseFeature[] features = [LicenseFeature.Switch("ecommerce"), Orders(500)];
        var result = _acme.Evaluate(Commerce,
            Issue(LicenseRole.Base, "2026-03-01T09:14:00Z", Ref8F3AK, features: features).KeyString,
            Issue(LicenseRole.Base, "2026-04-01T09:14:00Z", "LIC-C2D8R-XE6GU", features: features).KeyString);

        AssertStates(result, LicenseKeyState.Valid, LicenseKeyState.Valid);
        Assert.Equal(2, result.Product.ValidBaseCount);
        AssertFeatures(result.Product, "ecommerce", "max-orders 1000");
    }

    [Fact]
    public void Example14_Trial()
    {
        var trial = Issue(LicenseRole.Base, "2026-09-20T11:30:00Z", "LIC-7B2SW-NF5HA", "2026-10-20",
            features: [LicenseFeature.Switch("ecommerce"), LicenseFeature.Switch("trial")]);

        AssertStates(_acme.Evaluate(Commerce, trial.KeyString), LicenseKeyState.Valid);
        Assert.True(_acme.Evaluate(Commerce, trial.KeyString).Product.IsGranted("trial"));
        AssertStates(_acme.EvaluateAt("2026-10-21T00:00:00Z", Commerce, trial.KeyString), LicenseKeyState.Expired);
    }

    [Fact]
    public void Example15_AddOnProductIsSeparate()
    {
        using var zenith = new Vendor();
        var shipping = zenith.Issue("zenith.commerce-shipping", LicenseRole.Base, "2026-05-01T12:00:00Z", "LIC-5E3FT-BPZ7Y", "2027-05-01",
            features: LicenseFeature.Switch("shipping-rates"));

        var forCommerce = _acme.Evaluate(Commerce, shipping.KeyString);
        var forShipping = zenith.Evaluate("zenith.commerce-shipping", shipping.KeyString);

        AssertStates(forCommerce, LicenseKeyState.WrongProduct);
        Assert.False(forCommerce.Product.IsLicensed);
        AssertStates(forShipping, LicenseKeyState.Valid);
        Assert.True(forShipping.Product.IsGranted("shipping-rates"));
    }

    [Fact]
    public void Example16_TextFeatures()
    {
        var key = Issue(LicenseRole.Base, "2026-04-12T10:30:00Z", "LIC-3TQ7A-YE9DR", "2027-04-12", product: "acme.search-cloud",
            features: [LicenseFeature.Text("licensed-domain", "example.com"), LicenseFeature.Text("tenant", "t-8c21f"), LicenseFeature.Number("max-documents", 50000m)]);

        var result = _acme.Evaluate("acme.search-cloud", key.KeyString);

        AssertStates(result, LicenseKeyState.Valid);
        Assert.Equal("example.com", result.Product.GetFeature("licensed-domain")!.Text);
        AssertFeatures(result.Product, "licensed-domain example.com", "max-documents 50000", "tenant t-8c21f");
    }

    [Fact]
    public void Example17_TextAcrossLicenses()
    {
        const string cloud = "acme.search-cloud";
        var baseKey = Issue(LicenseRole.Base, "2026-04-12T10:30:00Z", "LIC-3TQ7A-YE9DR", product: cloud,
            features: [LicenseFeature.Text("licensed-domain", "example.com"), LicenseFeature.Text("tenant", "t-8c21f"), LicenseFeature.Number("max-documents", 50000m)]);
        string AddOn(string domain) => Issue(LicenseRole.AddOn, "2026-05-01T10:30:00Z", "LIC-K6W2P-D4ZHN", product: cloud,
            features: [LicenseFeature.Text("licensed-domain", domain), LicenseFeature.Number("max-documents", 25000m)]).KeyString;

        var equal = _acme.Evaluate(cloud, baseKey.KeyString, AddOn("example.com"));

        AssertStates(equal, LicenseKeyState.Valid, LicenseKeyState.Valid);
        AssertFeatures(equal.Product, "licensed-domain example.com", "max-documents 75000", "tenant t-8c21f");
        Assert.Empty(equal.Product.ConflictingFeatures);

        foreach (var conflicting in new[] { "exmaple.com", "Example.com" })
        {
            var typo = _acme.Evaluate(cloud, baseKey.KeyString, AddOn(conflicting));

            AssertStates(typo, LicenseKeyState.Valid, LicenseKeyState.Valid);
            AssertFeatures(typo.Product, "max-documents 75000", "tenant t-8c21f");
            Assert.Equal(["licensed-domain"], typo.Product.ConflictingFeatures);
            Assert.All(typo.Rows, r => Assert.Equal(["licensed-domain"], r.ConflictingFeatures));
            Assert.Null(typo.Product.GetFeature("licensed-domain"));
            Assert.True(typo.Product.IsLicensed);
        }

        var textVsNumber = _acme.Evaluate(cloud, baseKey.KeyString,
            Issue(LicenseRole.AddOn, "2026-05-01T10:30:00Z", product: cloud, features: LicenseFeature.Text("max-documents", "500")).KeyString);
        Assert.Equal(["max-documents"], textVsNumber.Product.ConflictingFeatures);
    }

    [Fact]
    public void Example18_KeysThatFailVerification()
    {
        using var newSigningKey = new Vendor();
        using var zenith = new Vendor();
        var key1 = Issue(LicenseRole.Base, "2026-03-01T09:14:00Z", Ref8F3AK, "2027-03-01", features: [LicenseFeature.Switch("ecommerce"), Orders(500)]);
        var key2 = Issue(LicenseRole.AddOn, "2026-06-15T16:40:00Z", "LIC-4Z9BE-T6WNH", "2027-03-01", features: Orders(1000));
        var key3 = Issue(LicenseRole.AddOn, "2026-09-28T10:00:00Z", "LIC-4Z9BE-T6WNH", "2027-03-01", features: Orders(2000));
        var key4 = zenith.Issue("zenith.commerce-shipping", LicenseRole.Base, "2026-05-01T12:00:00Z", "LIC-5E3FT-BPZ7Y", "2027-05-01");
        var key5 = Issue(LicenseRole.AddOn, "2026-07-01T10:00:00Z", "LIC-2V5C9-HKD3P", "2027-03-01", features: Orders(500));
        var key6 = newSigningKey.Issue(Commerce, LicenseRole.AddOn, "2026-09-01T08:05:00Z", "LIC-77DQS-9YJ4M", features: LicenseFeature.Switch("ai-assist"));
        var key7 = Issue(LicenseRole.Base, "2026-09-30T00:00:00Z", Ref8F3AK, "2027-03-01", features: Orders(500));
        var edited = KeyText.PayloadJson(key7.KeyString)
            .Replace("2027-03-01", "2099-12-31", StringComparison.Ordinal)
            .Replace(":500", ":999999", StringComparison.Ordinal);

        var result = _acme.Evaluate(Commerce,
            key1.KeyString,
            key2.KeyString,
            Mangle(key3.KeyString),
            key4.KeyString,
            key5.KeyString[..(key5.KeyString.Length - 30)],
            key6.KeyString,
            KeyText.WithPayload(key7.KeyString, edited));

        AssertStates(result,
            LicenseKeyState.Valid, LicenseKeyState.Valid, LicenseKeyState.NotVerified, LicenseKeyState.WrongProduct,
            LicenseKeyState.Unreadable, LicenseKeyState.SigningKeyNotRecognised, LicenseKeyState.NotVerified);
        Assert.Equal(
            [key1.KeyIdentifier, key2.KeyIdentifier, key3.KeyIdentifier, key4.KeyIdentifier, key5.KeyIdentifier, key6.KeyIdentifier, key7.KeyIdentifier],
            result.Rows.Select(r => r.KeyIdentifier));
        Assert.Equal([null, null, Commerce, "zenith.commerce-shipping", null, Commerce, Commerce], result.Rows.Select(r => r.ClaimedProductId));
        Assert.All(result.Rows.Skip(2), r => Assert.Null(r.License));
        AssertFeatures(result.Product, "ecommerce", "max-orders 1500");
        var text = ResultText.AllStrings(result).ToList();
        Assert.DoesNotContain(text, s => s.Contains("2099", StringComparison.Ordinal) || s.Contains("999999", StringComparison.Ordinal) || s.Contains("ai-assist", StringComparison.Ordinal));

        // A string with no readable identifier is identified by position only.
        Assert.Null(_acme.Evaluate(Commerce, "Hunter2!").Rows[0].KeyIdentifier);

        // Whitespace is not a failure.
        var wrapped = key1.KeyString[..70] + "\r\n   " + key1.KeyString[70..] + "\n";
        AssertStates(_acme.Evaluate(Commerce, wrapped), LicenseKeyState.Valid);

        // A key that verifies but breaks the schema is unreadable and grants nothing.
        var faulty = _acme.SignRaw(_acme.Payload(",\"features\":{\"max-orders\":-200}"));
        var schema = _acme.Evaluate(Commerce, faulty);
        AssertStates(schema, LicenseKeyState.Unreadable);
        Assert.Equal("LIC-8F3AK-M7RXB-7Q2D", schema.Rows[0].KeyIdentifier);
        Assert.False(schema.Product.IsLicensed);
    }

    [Fact]
    public void Example19_RejectedWhenIssued()
    {
        LicenseRequestProblem Rejected(LicenseRequest request) => Assert.Single(Assert.Throws<LicenseRequestException>(
            () => new LicenseIssuer(_acme.Clock).Issue(request, _acme.Keys.PrivateKey)).Problems);
        LicenseRequest Base(string? vendorTag = null, DateOnly? expires = null, params LicenseFeature[] features) =>
            new() { ProductId = Commerce, Role = LicenseRole.Base, VendorTag = vendorTag, Expires = expires, Features = features };

        Assert.Equal("Role", Rejected(new LicenseRequest { ProductId = Commerce }).Field);
        Assert.Equal("ProductId", Rejected(new LicenseRequest { ProductId = "commerce", Role = LicenseRole.Base }).Field);
        Assert.Equal("ProductId", Rejected(new LicenseRequest { ProductId = "Acme.Commerce", Role = LicenseRole.Base }).Field);
        Assert.DoesNotContain(typeof(LicenseRequest).GetProperties(), p => p.Name is "Issued" or "KeyPart");
        Assert.Equal("Expires", Rejected(Base(expires: new DateOnly(2026, 9, 30))).Field);
        Assert.Equal("Features", Rejected(Base(features: LicenseFeature.Switch("pro", false))).Field);
        Assert.Equal("Features", Rejected(Base(features: LicenseFeature.Text("licensed-domain", ""))).Field);
        Assert.Equal("Features", Rejected(Base(features: LicenseFeature.Text("licensed-domain", " example.com"))).Field);
        Assert.Equal("Features", Rejected(Base(features: LicenseFeature.Text("notes", "line one\nline two"))).Field);
        Assert.Equal("Features", Rejected(Base(features: LicenseFeature.Text("notes", new string('x', 257)))).Field);
        Assert.Equal("Features", Rejected(Base(features: LicenseFeature.Number("max-orders", "5OO"))).Field);
        Assert.Equal("Features", Rejected(Base(features: Orders(-200))).Field);
        Assert.Equal("Features", Rejected(Base(features: LicenseFeature.Number("storage-gb", 2.12345m))).Field);
        Assert.Equal("Features", Rejected(Base(features: [Orders(500), Orders(1000)])).Field);
        Assert.Equal("Features", Rejected(Base(features: LicenseFeature.Number("Max Orders", 500m))).Field);
        Assert.Equal("VendorTag", Rejected(Base(vendorTag: "INV 2026 04")).Field);
        Assert.Equal("VendorTag", Rejected(Base(vendorTag: "jane@acme.com")).Field);
        Assert.Equal("VendorTag", Rejected(Base(vendorTag: "")).Field);
        Assert.Equal("VendorTag", Rejected(Base(vendorTag: new string('A', 65))).Field);

        // Personal data cannot be policed in general (PDR-0022); an email address in the vendor
        // tag is caught by its character set, above.

        // Expiry today is allowed.
        Assert.NotNull(new LicenseIssuer(_acme.Clock).Issue(Base(expires: new DateOnly(2026, 10, 1)), _acme.Keys.PrivateKey));
    }

    [Fact]
    public void Example20_VendorTag()
    {
        var old = Issue(LicenseRole.Base, "2025-03-01T09:14:00Z", Ref8F3AK, "2026-03-01", "SHOP-2025-000087", features: [LicenseFeature.Switch("ecommerce"), Orders(500)]);
        var renewal = Issue(LicenseRole.Base, "2026-03-01T09:14:00Z", Ref8F3AK, "2027-03-01", "SHOP-2026-000123", features: [LicenseFeature.Switch("ecommerce"), Orders(500)]);
        var pack = Issue(LicenseRole.AddOn, "2026-03-01T09:14:00Z", "LIC-4Z9BE-T6WNH", "2027-03-01", "SHOP-2026-000123", features: Orders(1000));
        var failing = Issue(LicenseRole.AddOn, "2026-03-01T09:14:00Z", "LIC-2PW9H-KD4NZ", vendorTag: "SHOP-2026-000999");

        var result = _acme.Evaluate(Commerce, renewal.KeyString, old.KeyString, pack.KeyString, Mangle(failing.KeyString));

        AssertStates(result, LicenseKeyState.Valid, LicenseKeyState.Superseded, LicenseKeyState.Valid, LicenseKeyState.NotVerified);
        Assert.Equal(["SHOP-2026-000123", "SHOP-2025-000087", "SHOP-2026-000123", null], result.Rows.Select(r => r.License?.VendorTag));
        Assert.DoesNotContain(ResultText.AllStrings(result), s => s.Contains("000999", StringComparison.Ordinal));
        AssertFeatures(result.Product, "ecommerce", "max-orders 1500");
    }

    /// <summary>Changes one signature character, as a bad paste might: still readable, never verifies.</summary>
    private static string Mangle(string keyString)
    {
        var i = keyString.Length - 10;
        return keyString[..i] + (keyString[i] == 'A' ? 'B' : 'A') + keyString[(i + 1)..];
    }
}

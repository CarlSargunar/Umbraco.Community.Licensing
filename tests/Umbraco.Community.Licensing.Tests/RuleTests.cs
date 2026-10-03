using Umbraco.Community.Licensing.Internal;

namespace Umbraco.Community.Licensing.Tests;

/// <summary>tasks.md section 3: identifiers and value rules.</summary>
public sealed class RuleTests : IDisposable
{
    private readonly Vendor _vendor = new();

    public void Dispose() => _vendor.Dispose();

    private LicenseRequestException Reject(params LicenseFeature[] features) =>
        Assert.Throws<LicenseRequestException>(() =>
            _vendor.Issue("acme.commerce", LicenseRole.Base, features: features));

    // 3.1

    [Theory]
    [InlineData("acme.commerce", true)]
    [InlineData("acme.seo-toolkit", true)]
    [InlineData("zenith.commerce-shipping", true)]
    [InlineData("a1.b2", true)]
    [InlineData("commerce", false)]
    [InlineData("Acme.Commerce", false)]
    [InlineData("acme.commerce.extra", false)]
    [InlineData("1acme.commerce", false)]
    [InlineData("acme.-commerce", false)]
    [InlineData("acme.commerce\n", false)]
    [InlineData("acme_x.commerce", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ProductId(string? productId, bool valid)
    {
        Assert.Equal(valid, ProductIdRule.IsValid(productId));
    }

    // 3.2

    [Fact]
    public void Reference_GeneratedInFormatFromAlphabet()
    {
        for (var i = 0; i < 200; i++)
        {
            var reference = LicenseReferenceRule.NewReference();
            var keyPart = LicenseReferenceRule.NewKeyPart();

            Assert.Matches(@"^LIC-[2-9A-HJKMNP-Z]{5}-[2-9A-HJKMNP-Z]{5}\z", reference);
            Assert.Matches(@"^[2-9A-HJKMNP-Z]{4}\z", keyPart);
            Assert.True(LicenseReferenceRule.IsKeyIdentifier(LicenseReferenceRule.KeyIdentifier(reference, keyPart)));
        }

        Assert.Equal(31, LicenseReferenceRule.Alphabet.Length);
        Assert.Equal(31, LicenseReferenceRule.Alphabet.Distinct().Count());
        Assert.DoesNotContain(LicenseReferenceRule.Alphabet, c => c is '0' or 'O' or '1' or 'I' or 'L');
    }

    [Theory]
    [InlineData("lic-8f3ak m7rxb")]
    [InlineData("LIC-8F3AK-M7RXB")]
    [InlineData("8F3AKM7RXB")]
    [InlineData("8f3ak-m7rxb")]
    [InlineData(" LIC 8F3AK M7RXB ")]
    public void Reference_ParsedIgnoringCaseHyphensSpaces(string input)
    {
        Assert.True(LicenseReferenceRule.TryNormalize(input, out var reference));
        Assert.Equal("LIC-8F3AK-M7RXB", reference);
    }

    [Theory]
    [InlineData("LIC-8F3AK-M7RXO")]
    [InlineData("LIC-8F3AK-M7RX0")]
    [InlineData("LIC-8F3AK-M7RX1")]
    [InlineData("LIC-8F3AK-M7RXI")]
    [InlineData("LIC-8F3AK-M7RXL")]
    [InlineData("LIC-8F3AK-M7RX")]
    [InlineData("LIC-8F3AK-M7RXBB")]
    [InlineData("LIC-8F3AK-M7RXB-7Q2D")]
    [InlineData("")]
    [InlineData(null)]
    public void Reference_Rejected(string? input)
    {
        Assert.False(LicenseReferenceRule.TryNormalize(input, out _));
    }

    // 3.3: every feature row of docs/license-examples.md example 19.

    public static TheoryData<LicenseFeature[]> Example19FeatureRows => new()
    {
        new[] { LicenseFeature.Switch("pro", false) },
        new[] { LicenseFeature.Text("licensed-domain", "") },
        new[] { LicenseFeature.Text("licensed-domain", " example.com") },
        new[] { LicenseFeature.Text("licensed-domain", "example.com ") },
        new[] { LicenseFeature.Text("notes", "line one\nline two") },
        new[] { LicenseFeature.Text("notes", "tab\there") },
        new[] { LicenseFeature.Text("notes", new string('a', 257)) },
        new[] { LicenseFeature.Number("max-orders", "5OO") },
        new[] { LicenseFeature.Number("max-orders", -200m) },
        new[] { LicenseFeature.Number("max-orders", "-200") },
        new[] { LicenseFeature.Number("storage-gb", 2.12345m) },
        new[] { LicenseFeature.Number("max-orders", 1_000_000_000_000_000m) },
        new[] { LicenseFeature.Number("max-orders", 500m), LicenseFeature.Number("max-orders", 1000m) },
        new[] { LicenseFeature.Number("max-orders", 500m), LicenseFeature.Switch("max-orders") },
        new[] { LicenseFeature.Number("Max Orders", 500m) },
        new[] { LicenseFeature.Switch("Pro") },
        new[] { LicenseFeature.Switch("2fa") },
        new[] { LicenseFeature.Switch("") },
        new[] { LicenseFeature.Switch(null!) },
    };

    [Theory]
    [MemberData(nameof(Example19FeatureRows))]
    public void Features_Example19RowsRejected(LicenseFeature[] features)
    {
        var error = Reject(features);

        Assert.All(error.Problems, p => Assert.Equal("Features", p.Field));
        Assert.Single(error.Problems);
    }

    [Fact]
    public void Features_ValidTypesKeptExactly()
    {
        var issued = _vendor.Issue("acme.commerce", LicenseRole.Base, features:
        [
            LicenseFeature.Switch("ecommerce"),
            LicenseFeature.Number("storage-gb", 2.5m),
            LicenseFeature.Number("max-documents", "50000"),
            LicenseFeature.Number("zero", 0m),
            LicenseFeature.Text("licensed-domain", "example.com"),
            LicenseFeature.Text("max-orders", "500"),
            LicenseFeature.Text("long", new string('a', 256)),
            LicenseFeature.Text("emoji", "café \U0001F600"),
        ]);

        var features = _vendor.Evaluate("acme.commerce", issued.KeyString).Rows[0].License!.Features;

        Assert.Equal(FeatureValue.Switch, features["ecommerce"]);
        Assert.Equal(2.5m, features["storage-gb"].Number);
        Assert.Equal(50000m, features["max-documents"].Number);
        Assert.Equal(0m, features["zero"].Number);
        Assert.Equal("example.com", features["licensed-domain"].Text);
        Assert.Equal(FeatureKind.Text, features["max-orders"].Kind);
        Assert.Equal("500", features["max-orders"].Text);
        Assert.Equal(256, features["long"].Text!.Length);
        Assert.Equal("café \U0001F600", features["emoji"].Text);
    }

    [Fact]
    public void Features_TextLengthCountsCharactersNotUtf16Units()
    {
        var text = string.Concat(Enumerable.Repeat("\U0001F600", 256));

        Assert.Null(FeatureRules.TextProblem(text));
        Assert.NotNull(FeatureRules.TextProblem(text + "a"));
        Assert.NotNull(FeatureRules.TextProblem("bad\uD800"));
        Assert.NotNull(FeatureRules.TextProblem("a" + (char)0x2028 + "b"));
    }

    // 3.4: PDR-0022 inputs and the vendor tag rows of example 19.

    [Theory]
    [InlineData("#1001")]
    [InlineData("SHOP-2026-000123")]
    [InlineData("pi_3NkX9a2eZvKYlo2C1")]
    [InlineData("INV/2026/04")]
    [InlineData("a.b")]
    public void VendorTag_Accepted(string tag)
    {
        Assert.True(VendorTagRule.IsValid(tag));
        var issued = _vendor.Issue("acme.commerce", LicenseRole.Base, vendorTag: tag);
        Assert.Equal(tag, _vendor.Evaluate("acme.commerce", issued.KeyString).Rows[0].License!.VendorTag);
    }

    [Theory]
    [InlineData("INV 2026 04")]
    [InlineData("jane@acme.com")]
    [InlineData("")]
    [InlineData("ABC\n")]
    [InlineData(" ABC")]
    [InlineData("ABC+1")]
    [InlineData("café")]
    [InlineData("Kelvin")]
    public void VendorTag_Rejected(string tag)
    {
        Assert.False(VendorTagRule.IsValid(tag));
        var error = Assert.Throws<LicenseRequestException>(() =>
            _vendor.Issue("acme.commerce", LicenseRole.Base, vendorTag: tag));
        Assert.Equal("VendorTag", Assert.Single(error.Problems).Field);
    }

    [Fact]
    public void VendorTag_LengthLimit()
    {
        Assert.True(VendorTagRule.IsValid(new string('A', 64)));
        Assert.False(VendorTagRule.IsValid(new string('A', 65)));
    }

    [Fact]
    public void VendorTag_WrittenUnescaped()
    {
        var issued = _vendor.Issue("acme.commerce", LicenseRole.Base, vendorTag: "#1001");

        Assert.Contains("\"vendorTag\":\"#1001\"", KeyText.PayloadJson(issued.KeyString));
    }

    [Fact]
    public void VendorTag_EveryAllowedCharacterWrittenUnescaped()
    {
        var allowed = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_.#/";
        foreach (var chunk in allowed.Chunk(64).Select(c => new string(c)))
        {
            var issued = _vendor.Issue("acme.commerce", LicenseRole.Base, vendorTag: chunk);
            Assert.Contains($"\"vendorTag\":\"{chunk}\"", KeyText.PayloadJson(issued.KeyString));
        }
    }
}

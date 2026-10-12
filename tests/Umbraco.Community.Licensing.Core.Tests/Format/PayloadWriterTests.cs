using System.Text;
using System.Text.Json;
using Umbraco.Community.Licensing.Core.Tests.Support;

namespace Umbraco.Community.Licensing.Core.Tests.Format;

public class PayloadWriterTests
{
    private static readonly DateTimeOffset Issued = new(2026, 9, 1, 8, 5, 12, TimeSpan.Zero);

    private static LicensePayload Minimal(params FeatureInput[] features) =>
        new("0F8NSYaNR_Q", "acme.commerce", Issued, null, null, null, features);

    private static string Json(LicensePayload payload) => Encoding.UTF8.GetString(PayloadWriter.Write(payload));

    [Fact]
    public void Write_Adr0002Sample_ProducesExactJson()
    {
        var payload = new LicensePayload(
            "0F8NSYaNR_Q",
            "acme.commerce",
            Issued,
            new DateTimeOffset(2027, 3, 1, 23, 59, 59, TimeSpan.Zero),
            "Commerce Pro",
            "CUST-0042",
            [
                FeatureInput.Switch("ecommerce", true),
                FeatureInput.OfNumber("max-orders", 2500m),
                FeatureInput.OfText("licensed-domains", "example.com,shop.example.com"),
            ]);

        Assert.Equal(
            "{\"signingKeyId\":\"0F8NSYaNR_Q\",\"product\":\"acme.commerce\",\"issued\":\"2026-09-01T08:05:12Z\","
            + "\"expires\":\"2027-03-01T23:59:59Z\",\"displayName\":\"Commerce Pro\",\"vendorTag\":\"CUST-0042\","
            + "\"features\":{\"ecommerce\":true,\"max-orders\":2500,\"licensed-domains\":\"example.com,shop.example.com\"}}",
            Json(payload));
    }

    [Fact]
    public void Write_Minimal_OmitsOptionalFieldsAndEmptyFeatures()
    {
        Assert.Equal(
            "{\"signingKeyId\":\"0F8NSYaNR_Q\",\"product\":\"acme.commerce\",\"issued\":\"2026-09-01T08:05:12Z\"}",
            Json(Minimal()));
    }

    [Fact]
    public void Write_EachOptionalFieldAlone_IsWrittenOthersOmitted()
    {
        var expires = Minimal() with { Expires = new DateTimeOffset(2027, 1, 2, 3, 4, 5, TimeSpan.Zero) };
        var name = Minimal() with { DisplayName = "Pro" };
        var tag = Minimal() with { VendorTag = "T-1" };
        var features = Minimal(FeatureInput.Switch("a", true));
        const string head = "{\"signingKeyId\":\"0F8NSYaNR_Q\",\"product\":\"acme.commerce\",\"issued\":\"2026-09-01T08:05:12Z\"";

        Assert.Equal(head + ",\"expires\":\"2027-01-02T03:04:05Z\"}", Json(expires));
        Assert.Equal(head + ",\"displayName\":\"Pro\"}", Json(name));
        Assert.Equal(head + ",\"vendorTag\":\"T-1\"}", Json(tag));
        Assert.Equal(head + ",\"features\":{\"a\":true}}", Json(features));
    }

    [Theory]
    [InlineData("tr-TR")]
    [InlineData("de-DE")]
    public void Write_Numbers_AreNormalisedAndCultureInvariant(string culture)
    {
        using var scope = new CultureScope(culture);

        var json = Json(Minimal(
            FeatureInput.OfNumber("a", 2.50m),
            FeatureInput.OfNumber("b", 2500m),
            FeatureInput.OfNumber("c", 0.1234m),
            FeatureInput.OfNumber("d", 0m)));

        Assert.EndsWith("\"features\":{\"a\":2.5,\"b\":2500,\"c\":0.1234,\"d\":0}}", json);
    }

    [Fact]
    public void Write_NonAscii_IsEscapedAndRoundTrips()
    {
        const string name = "Café 中文 \U0001F600";
        const string text = "über, ångström";
        var payload = Minimal(FeatureInput.OfText("note", text)) with { DisplayName = name };

        var bytes = PayloadWriter.Write(payload);

        Assert.All(bytes, b => Assert.True(b < 0x80));
        using var doc = JsonDocument.Parse(bytes);
        Assert.Equal(name, doc.RootElement.GetProperty("displayName").GetString());
        Assert.Equal(text, doc.RootElement.GetProperty("features").GetProperty("note").GetString());
    }

    [Fact]
    public void Write_NonUtcDate_IsWrittenAsUtcInstant()
    {
        var payload = Minimal() with
        {
            Issued = new DateTimeOffset(2026, 9, 1, 10, 5, 12, TimeSpan.FromHours(2)),
            Expires = new DateTimeOffset(2027, 1, 1, 1, 0, 0, TimeSpan.FromHours(5.5)),
        };

        var json = Json(payload);

        Assert.Contains("\"issued\":\"2026-09-01T08:05:12Z\"", json);
        Assert.Contains("\"expires\":\"2026-12-31T19:30:00Z\"", json);
    }

    [Fact]
    public void Write_FeaturesKeepListOrder()
    {
        var json = Json(Minimal(FeatureInput.Switch("zeta", true), FeatureInput.Switch("alpha", true)));

        Assert.EndsWith("\"features\":{\"zeta\":true,\"alpha\":true}}", json);
    }

    [Fact]
    public void Write_DateWithFraction_Throws()
    {
        var payload = Minimal() with { Issued = Issued.AddTicks(1) };

        Assert.Throws<InvalidOperationException>(() => PayloadWriter.Write(payload));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.23456)]
    public void Write_NumberFailingTheRule_Throws(double value)
    {
        var payload = Minimal(FeatureInput.OfNumber("a", (decimal)value));

        Assert.Throws<InvalidOperationException>(() => PayloadWriter.Write(payload));
    }

    [Fact]
    public void Write_OffSwitchOrMissingText_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => PayloadWriter.Write(Minimal(FeatureInput.Switch("a", false))));
        Assert.Throws<InvalidOperationException>(() => PayloadWriter.Write(Minimal(FeatureInput.OfText("a", null))));
    }
}

using System.Globalization;
using System.Text;
using Umbraco.Community.Licensing.Internal;

namespace Umbraco.Community.Licensing.Tests;

/// <summary>tasks.md section 2: key string format and cryptography (ADR-0001).</summary>
public sealed class KeyFormatTests : IDisposable
{
    // ADR-0001 worked example: public key with signing key ID 0F8NSYaNR_Q and example 1's key.
    private const string AdrPublicKeyPem = """
        -----BEGIN PUBLIC KEY-----
        MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEE9JU1oc/2dr+LcbtihhiLwjORp+7
        UNlgoSeR0g7RiHABsDCZPL7I1Y33osymOTjKsC8Z9h8W6M8lIscmca/tcg==
        -----END PUBLIC KEY-----
        """;

    private const string AdrMinimalKey = """
        LIC-4HN7T-QW2ZC-9KXM.eyJzaWduaW5nS2V5SWQiOiIwRjhOU1lhTlJfUSIsInByb2R1Y3QiOiJhY21lLnNlby10b
        29sa2l0Iiwicm9sZSI6ImJhc2UiLCJpc3N1ZWQiOiIyMDI2LTAxLTEwVDE0OjAyOjM3WiJ9.hMkCI5CH9V64HedGKZ
        2MinQ2a1QdGUSkh4TS1CPhVMAN8EauLHm-5pHnseh9Tq1x5PAluIWuld-edZJCvX91MQ
        """;

    private const string AdrTaggedKey = """
        LIC-8F3AK-M7RXB-7Q2D.eyJzaWduaW5nS2V5SWQiOiIwRjhOU1lhTlJfUSIsInByb2R1Y3QiOiJhY21lLmNvbW1lc
        mNlIiwicm9sZSI6ImJhc2UiLCJ2ZW5kb3JUYWciOiJTSE9QLTIwMjYtMDAwMTIzIiwiaXNzdWVkIjoiMjAyNi0wMy0
        wMVQwOToxNDoyMloiLCJleHBpcmVzIjoiMjAyNy0wMy0wMSIsImZlYXR1cmVzIjp7ImVjb21tZXJjZSI6dHJ1ZSwib
        WF4LW9yZGVycyI6NTAwfX0.EP9HAvpbFfQj6rMkeUuwqo99VwrZu2tsC_BAX5NZAF9rqjVWll6DohP57B3h8RHRTya
        UIkQDqGdmOCmY0jIiCg
        """;

    private readonly Vendor _vendor = new();

    public void Dispose() => _vendor.Dispose();

    // 2.1

    [Fact]
    public void SigningKeyId_IsTheSameFromPrivateAndPublicKey()
    {
        using var pair = SigningKeyPair.Create();

        Assert.Equal(pair.PrivateKey.SigningKeyId, pair.PublicKey.SigningKeyId);
        Assert.Equal(11, pair.SigningKeyId.Length);
        using var reimported = SigningPrivateKey.FromPem(pair.PrivateKey.ExportPkcs8Pem());
        Assert.Equal(pair.SigningKeyId, reimported.SigningKeyId);
        Assert.Equal(pair.SigningKeyId, SigningPublicKey.FromPem(pair.PublicKey.ExportPem()).SigningKeyId);
    }

    [Fact]
    public void SigningKeyId_DiffersBetweenKeyPairs()
    {
        using var a = SigningKeyPair.Create();
        using var b = SigningKeyPair.Create();

        Assert.NotEqual(a.SigningKeyId, b.SigningKeyId);
    }

    [Fact]
    public void SigningKeyId_MatchesAdrWorkedExample()
    {
        Assert.Equal("0F8NSYaNR_Q", SigningPublicKey.FromPem(AdrPublicKeyPem).SigningKeyId);
    }

    [Fact]
    public void AdrWorkedExamples_VerifyAgainstAdrPublicKey()
    {
        var trusted = new TrustedSigningKeys([SigningPublicKey.FromPem(AdrPublicKeyPem)]);
        var evaluator = new LicenseEvaluator(trusted, FixedClock.At("2026-10-01T00:00:00Z"));

        var minimal = evaluator.Evaluate("acme.seo-toolkit", [AdrMinimalKey]).Rows[0];
        var tagged = evaluator.Evaluate("acme.commerce", [AdrTaggedKey]).Rows[0];

        Assert.Equal(LicenseKeyState.Valid, minimal.State);
        Assert.Equal("LIC-4HN7T-QW2ZC-9KXM", minimal.KeyIdentifier);
        Assert.Equal(LicenseKeyState.Valid, tagged.State);
        Assert.Equal("SHOP-2026-000123", tagged.License!.VendorTag);
    }

    // 2.2

    [Fact]
    public void Payload_RoundTripsEveryFieldAndFeatureType()
    {
        var payload = new LicensePayload(
            "0F8NSYaNR_Q",
            "acme.commerce",
            LicenseRole.AddOn,
            "SHOP-2026-000123",
            new DateTimeOffset(2026, 3, 1, 9, 14, 22, TimeSpan.Zero),
            new DateOnly(2027, 3, 1),
            [
                new("ecommerce", FeatureValue.Switch),
                new("max-orders", FeatureValue.FromNumber(500)),
                new("storage-gb", FeatureValue.FromNumber(2.5m)),
                new("licensed-domain", FeatureValue.FromText("example.com")),
                new("code", FeatureValue.FromText("500")),
            ]);

        var bytes = PayloadFormat.Write(payload);
        var json = Encoding.UTF8.GetString(bytes);
        var read = PayloadFormat.Read(bytes).Payload;

        Assert.Equal(
            """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"add-on","vendorTag":"SHOP-2026-000123","issued":"2026-03-01T09:14:22Z","expires":"2027-03-01","features":{"ecommerce":true,"max-orders":500,"storage-gb":2.5,"licensed-domain":"example.com","code":"500"}}""",
            json);
        Assert.NotNull(read);
        Assert.Equal(payload.SigningKeyId, read.SigningKeyId);
        Assert.Equal(payload.ProductId, read.ProductId);
        Assert.Equal(payload.Role, read.Role);
        Assert.Equal(payload.VendorTag, read.VendorTag);
        Assert.Equal(payload.Issued, read.Issued);
        Assert.Equal(payload.Expires, read.Expires);
        Assert.Equal(payload.Features, read.Features);
        Assert.Equal(FeatureKind.Text, read.Features.Single(f => f.Key == "code").Value.Kind);
        Assert.Equal(FeatureKind.Number, read.Features.Single(f => f.Key == "max-orders").Value.Kind);
    }

    [Fact]
    public void Payload_OmitsAbsentOptionalFields()
    {
        var payload = new LicensePayload("0F8NSYaNR_Q", "acme.seo-toolkit", LicenseRole.Base, null,
            new DateTimeOffset(2026, 1, 10, 14, 2, 37, TimeSpan.Zero), null, []);

        Assert.Equal(
            """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.seo-toolkit","role":"base","issued":"2026-01-10T14:02:37Z"}""",
            Encoding.UTF8.GetString(PayloadFormat.Write(payload)));
    }

    // 2.3

    [Theory]
    [InlineData("2.50", "2.5")]
    [InlineData("3.0", "3")]
    [InlineData("2.12340", "2.1234")]
    [InlineData("0", "0")]
    [InlineData("999999999999999", "999999999999999")]
    [InlineData("99999999999.9999", "99999999999.9999")]
    public void Number_IssuerWritesCanonicalForm(string input, string wire)
    {
        Assert.Null(NumberRule.Check(decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture), out var written));
        Assert.Equal(wire, written);
    }

    [Theory]
    [InlineData("1000000000000000")]
    [InlineData("1.23456")]
    [InlineData("-1")]
    public void Number_IssuerRejectsOutOfRange(string input)
    {
        Assert.NotNull(NumberRule.Check(decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture), out _));
    }

    [Fact]
    public void Number_WriterUsesInvariantCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var issued = _vendor.Issue("acme.commerce", LicenseRole.Base, features: LicenseFeature.Number("storage-gb", 2.50m));
            Assert.Contains("\"storage-gb\":2.5}", KeyText.PayloadJson(issued.KeyString));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("5e2")]
    [InlineData("-0")]
    [InlineData("007")]
    [InlineData("1.23456")]
    [InlineData("2.12340")]
    [InlineData("1.")]
    [InlineData("1000000000000000")]
    public void Number_ReaderRejectsTokensBreakingTheGrammar(string token)
    {
        var json = _vendor.Payload($",\"features\":{{\"n\":{token}}}");

        Assert.Null(PayloadFormat.Read(Encoding.UTF8.GetBytes(json)).Payload);
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("2.5", "2.5")]
    [InlineData("2.50", "2.5")]
    [InlineData("0.1234", "0.1234")]
    public void Number_ReaderReadsDecimal(string token, string expected)
    {
        var json = _vendor.Payload($",\"features\":{{\"n\":{token}}}");

        var value = PayloadFormat.Read(Encoding.UTF8.GetBytes(json)).Payload!.Features.Single().Value;
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), value.Number);
        Assert.Equal(expected, value.ToString());
    }

    // 2.4

    public static TheoryData<string> StrictReaderViolations => new()
    {
        // Missing required fields.
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","issued":"2026-03-01T09:14:22Z"}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base"}""",
        // Unknown top-level field.
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","issued":"2026-03-01T09:14:22Z","version":1}""",
        // expires: null, and inexact dates.
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","issued":"2026-03-01T09:14:22Z","expires":null}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","issued":"2026-03-01T09:14:22Z","expires":"2027-3-1"}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","issued":"2026-03-01T09:14:22Z","expires":"2027-03-01T00:00:00Z"}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","issued":"2026-03-01T09:14:22.000Z"}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","issued":"2026-03-01T09:14:22+00:00"}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","issued":"2026-02-30T09:14:22Z"}""",
        // Feature values false, null, object, array; repeated name.
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","issued":"2026-03-01T09:14:22Z","features":{"pro":false}}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","issued":"2026-03-01T09:14:22Z","features":{"pro":null}}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","issued":"2026-03-01T09:14:22Z","features":{"pro":{}}}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","issued":"2026-03-01T09:14:22Z","features":{"pro":[]}}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","issued":"2026-03-01T09:14:22Z","features":{"max-orders":500,"max-orders":1000}}""",
        // vendorTag null, empty, 65 characters, @, space, decoded trailing newline.
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","vendorTag":null,"issued":"2026-03-01T09:14:22Z"}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","vendorTag":"","issued":"2026-03-01T09:14:22Z"}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","vendorTag":"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA","issued":"2026-03-01T09:14:22Z"}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","vendorTag":"jane@acme.com","issued":"2026-03-01T09:14:22Z"}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","vendorTag":"INV 2026","issued":"2026-03-01T09:14:22Z"}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","vendorTag":"ABC\n","issued":"2026-03-01T09:14:22Z"}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","vendorTag":123,"issued":"2026-03-01T09:14:22Z"}""",
        // Other rules checked at issue.
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"trial","issued":"2026-03-01T09:14:22Z"}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","issued":"2026-03-01T09:14:22Z","features":{"Max-Orders":500}}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","issued":"2026-03-01T09:14:22Z","features":{"notes":"line one\nline two"}}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","issued":"2026-03-01T09:14:22Z","features":{"d":" example.com"}}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","issued":"2026-03-01T09:14:22Z","features":[]}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","role":"base","issued":"2026-03-01T09:14:22Z"}""",
        // Object or array on a top-level field other than features: routing claims survive (ADR-0001 step 4).
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","issued":"2026-03-01T09:14:22Z","expires":{"date":"2027-03-01"}}""",
        """{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":["base"],"issued":"2026-03-01T09:14:22Z"}""",
    };

    [Theory]
    [MemberData(nameof(StrictReaderViolations))]
    public void StrictReader_RejectsSchemaViolationsButKeepsRoutingClaims(string json)
    {
        var read = PayloadFormat.Read(Encoding.UTF8.GetBytes(json));

        Assert.True(read.HasRoutingClaims);
        Assert.Null(read.Payload);
    }

    [Theory]
    [InlineData("""{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","vendorTag":"INV\/2026","issued":"2026-03-01T09:14:22Z"}""", "INV/2026")]
    [InlineData("""{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","role":"base","vendorTag":"#1001","issued":"2026-03-01T09:14:22Z"}""", "#1001")]
    public void StrictReader_AcceptsEscapedVendorTag(string json, string expected)
    {
        Assert.Equal(expected, PayloadFormat.Read(Encoding.UTF8.GetBytes(json)).Payload!.VendorTag);
    }

    [Fact]
    public void StrictReader_EmptyFeaturesObjectMeansNone()
    {
        var read = PayloadFormat.Read(Encoding.UTF8.GetBytes(_vendor.Payload(",\"features\":{}")));

        Assert.Empty(read.Payload!.Features);
    }

    [Theory]
    [InlineData("""[]""")]
    [InlineData("""{"product":"acme.commerce","role":"base"}""")]
    [InlineData("""{"signingKeyId":"0F8NSYaNR_Q","product":5}""")]
    [InlineData("""{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce","product":"acme.other"}""")]
    [InlineData("""{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce"} x""")]
    [InlineData("""{"signingKeyId":"0F8NSYaNR_Q","product":"acme.commerce\""")]
    public void Reader_WithoutRoutingClaimsIsUnreadable(string json)
    {
        Assert.False(PayloadFormat.Read(Encoding.UTF8.GetBytes(json)).HasRoutingClaims);
    }

    // 2.5

    [Fact]
    public void Signing_ProducesSingleLineThreeSegmentKey()
    {
        var issued = _vendor.Issue("acme.commerce", LicenseRole.Base, expires: "2027-03-01",
            features: [LicenseFeature.Switch("ecommerce"), LicenseFeature.Text("licensed-domain", "example.com")]);

        var key = issued.KeyString;
        var segments = key.Split('.');
        Assert.DoesNotContain(key, char.IsWhiteSpace);
        Assert.All(key, c => Assert.True(c is > ' ' and < (char)127));
        Assert.Equal(3, segments.Length);
        Assert.Matches(@"^LIC-[2-9A-HJKMNP-Z]{5}-[2-9A-HJKMNP-Z]{5}-[2-9A-HJKMNP-Z]{4}\z", segments[0]);
        Assert.StartsWith(issued.KeyIdentifier + ".", key);
        Assert.True(Base64UrlText.TryDecode(segments[2], out var signature));
        Assert.Equal(64, signature.Length);
        Assert.DoesNotContain('=', key);
    }

    // 2.6

    [Fact]
    public void Verification_AcceptsValidAndRejectsAlteredOrWrongKey()
    {
        var key = _vendor.Issue("acme.commerce", LicenseRole.Base).KeyString;
        var parts = key.Split('.');
        var signature = System.Buffers.Text.Base64Url.DecodeFromChars(parts[2]);
        bool Verify(SigningPublicKey publicKey, string signingInput) =>
            publicKey.Verify(Encoding.ASCII.GetBytes(signingInput), signature);

        var signingInput = parts[0] + "." + parts[1];
        var alteredPayload = parts[0] + "." + Base64UrlText.Encode(Encoding.UTF8.GetBytes(
            KeyText.PayloadJson(key).Replace("\"base\"", "\"add-on\"", StringComparison.Ordinal)));
        var alteredIdentifier = parts[0][..^1] + (parts[0][^1] == 'A' ? 'B' : 'A') + "." + parts[1];
        var lowercased = parts[0].ToLowerInvariant() + "." + parts[1];
        using var other = SigningKeyPair.Create();

        Assert.True(Verify(_vendor.Keys.PublicKey, signingInput));
        Assert.False(Verify(_vendor.Keys.PublicKey, alteredPayload));
        Assert.False(Verify(_vendor.Keys.PublicKey, alteredIdentifier));
        Assert.False(Verify(_vendor.Keys.PublicKey, lowercased));
        Assert.False(Verify(other.PublicKey, signingInput));
    }

    // P-256 group order n.
    private const string P256Order = "FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551";

    public static TheoryData<string> DegenerateSignatures => new()
    {
        new string('0', 128), // r = s = 0
        new string('F', 128), // r, s > n
        P256Order + P256Order, // r = s = n
    };

    // license-validation spec "Not verified": a trusted, correct-product key whose signature is junk.
    [Theory]
    [MemberData(nameof(DegenerateSignatures))]
    public void Verification_RejectsDegenerateSignature(string signatureHex)
    {
        var parts = _vendor.Issue("acme.commerce", LicenseRole.Base).KeyString.Split('.');
        var forged = parts[0] + "." + parts[1] + "." + Base64UrlText.Encode(Convert.FromHexString(signatureHex));

        Assert.Equal(LicenseKeyState.NotVerified, _vendor.Evaluate("acme.commerce", forged).Rows[0].State);
    }

    // 2.7: ADR-0001 reading table.

    [Fact]
    public void Reading_FullKeyContinuesToRoutingClaims()
    {
        var key = _vendor.Issue("acme.commerce", LicenseRole.Base).KeyString;

        var read = KeyString.Read(key);

        Assert.True(read.IsReadable);
        Assert.Equal(key.Split('.')[0], read.Identifier);
        Assert.Equal("acme.commerce", read.Payload!.ProductId);
        Assert.Equal(_vendor.Keys.SigningKeyId, read.Payload.SigningKeyId);
    }

    [Theory]
    [InlineData(29)] // cut inside the payload
    [InlineData(20)] // cut at the dot
    [InlineData(21)] // cut after the dot
    public void Reading_KeyCutAfterIdentifierIsUnreadableWithIdentifier(int length)
    {
        var key = _vendor.Issue("acme.commerce", LicenseRole.Base).KeyString;

        var read = KeyString.Read(key[..length]);

        Assert.False(read.IsReadable);
        Assert.Equal(key[..20], read.Identifier);
    }

    [Fact]
    public void Reading_KeyCutBeforeSignatureEndIsUnreadable()
    {
        var key = _vendor.Issue("acme.commerce", LicenseRole.Base).KeyString;

        var read = KeyString.Read(key[..^3]);

        Assert.False(read.IsReadable);
        Assert.Equal(key[..20], read.Identifier);
    }

    [Theory]
    [InlineData("LIC-8F3AK-M7R")]
    [InlineData("lic-8f3ak-m7rxb-7q2d.eyJzaWdu.abc")]
    [InlineData("Hunter2!")]
    [InlineData("")]
    [InlineData("LIC-8F3AK-M7RXB-7Q2DX.eyJ.abc")]
    [InlineData("LIC-8F3AK-M7RXB-7Q2O.eyJ.abc")]
    public void Reading_StringWithoutIdentifierIsUnreadableByPosition(string supplied)
    {
        var read = KeyString.Read(supplied);

        Assert.False(read.IsReadable);
        Assert.Null(read.Identifier);
    }

    [Fact]
    public void Reading_RejectsExtraSegmentsPaddingAndShortSignature()
    {
        var key = _vendor.Issue("acme.commerce", LicenseRole.Base).KeyString;
        var parts = key.Split('.');

        Assert.False(KeyString.Read(key + ".x").IsReadable);
        Assert.False(KeyString.Read(parts[0] + "." + parts[1] + "==." + parts[2]).IsReadable);
        Assert.False(KeyString.Read(parts[0] + "." + parts[1] + "." + parts[2][..40]).IsReadable);
        Assert.False(KeyString.Read(parts[0] + ".." + parts[2]).IsReadable);
        Assert.False(KeyString.Read(parts[0] + "." + parts[1] + "+." + parts[2]).IsReadable);
    }

    [Fact]
    public void Reading_WrappedKeyWithTrailingNewlineReadsAsOriginal()
    {
        var key = _vendor.Issue("acme.commerce", LicenseRole.Base).KeyString;
        var wrapped = " " + key[..60] + "\r\n    " + key[60..120] + "\n\t" + key[120..] + "\n";

        var evaluation = _vendor.Evaluate("acme.commerce", wrapped);

        Assert.Equal(KeyString.Read(key).SigningInput, KeyString.Read(wrapped).SigningInput);
        Assert.Equal(LicenseKeyState.Valid, evaluation.Rows[0].State);
    }

    // 2.8

    [Fact]
    public void Schema_VerifiedKeyWithNegativeNumberIsUnreadable()
    {
        var key = _vendor.SignRaw(_vendor.Payload(",\"features\":{\"max-orders\":-200}"));

        var row = _vendor.Evaluate("acme.commerce", key).Rows[0];

        Assert.Equal(LicenseKeyState.Unreadable, row.State);
        Assert.Equal("LIC-8F3AK-M7RXB-7Q2D", row.KeyIdentifier);
        Assert.Null(row.License);
    }

    [Fact]
    public void Schema_VerifiedKeyWithValidContentsSignedOutsideApiIsValid()
    {
        var key = _vendor.SignRaw(_vendor.Payload(",\"features\":{\"max-orders\":200}"));

        Assert.Equal(LicenseKeyState.Valid, _vendor.Evaluate("acme.commerce", key).Rows[0].State);
    }

    // 2.9

    [Fact]
    public void Duplicates_SameSigningInputWithDifferentSignatures()
    {
        var payload = _vendor.Payload();
        var first = _vendor.SignRaw(payload);
        var second = _vendor.SignRaw(payload);

        var rows = _vendor.Evaluate("acme.commerce", first, second).Rows;

        Assert.NotEqual(first, second);
        Assert.Equal(LicenseKeyState.Valid, rows[0].State);
        Assert.Equal(LicenseKeyState.Duplicate, rows[1].State);
        Assert.Equal(0, rows[1].DuplicateOf);
    }
}

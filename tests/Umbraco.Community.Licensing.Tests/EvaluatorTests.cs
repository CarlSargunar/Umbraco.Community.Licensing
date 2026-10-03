using System.Globalization;
using System.Security.Cryptography;

namespace Umbraco.Community.Licensing.Tests;

/// <summary>tasks.md sections 6 and 7: per-key evaluation and the product result.</summary>
public sealed class EvaluatorTests : IDisposable
{
    private const string Product = "acme.commerce";
    private readonly Vendor _vendor = new();

    public void Dispose() => _vendor.Dispose();

    private string Base(params LicenseFeature[] features) => _vendor.Issue(Product, LicenseRole.Base, features: features).KeyString;

    private string AddOn(params LicenseFeature[] features) => _vendor.Issue(Product, LicenseRole.AddOn, features: features).KeyString;

    // 6.1

    [Fact]
    public void EmptyList_NoRowsNotLicensed()
    {
        var result = _vendor.Evaluate(Product);

        Assert.Empty(result.Rows);
        Assert.False(result.Product.IsLicensed);
        Assert.Equal(0, result.Product.ValidBaseCount);
        Assert.Empty(result.Product.Features);
        Assert.Equal(Product, result.Product.ProductId);
    }

    [Fact]
    public void OneRowPerStringInOrder()
    {
        var a = _vendor.Issue(Product, LicenseRole.Base);
        var b = _vendor.Issue(Product, LicenseRole.AddOn);

        var rows = _vendor.Evaluate(Product, b.KeyString, "Hunter2!", a.KeyString).Rows;

        Assert.Equal([0, 1, 2], rows.Select(r => r.Position));
        Assert.Equal(b.KeyIdentifier, rows[0].KeyIdentifier);
        Assert.Null(rows[1].KeyIdentifier);
        Assert.Equal(a.KeyIdentifier, rows[2].KeyIdentifier);
    }

    [Fact]
    public void InvalidProductIdIsCallerError()
    {
        Assert.Throws<ArgumentException>(() => _vendor.Evaluate("Acme.Commerce"));
    }

    // 6.2

    [Fact]
    public void FailureChecksInOrder()
    {
        using var stranger = new Vendor();
        var unreadable = "LIC-8F3AK-M7RXB-7Q2D.eyJzaWdu";
        var wrongProduct = _vendor.Issue("zenith.commerce-shipping", LicenseRole.Base).KeyString;
        var notRecognised = stranger.Issue(Product, LicenseRole.Base).KeyString;
        var key = Base();
        var notVerified = key[..^4] + (key[^4] == 'A' ? "B" : "A") + key[^3..];

        var states = _vendor.Evaluate(Product, unreadable, wrongProduct, notRecognised, notVerified).Rows.Select(r => r.State);

        Assert.Equal(
            [LicenseKeyState.Unreadable, LicenseKeyState.WrongProduct, LicenseKeyState.SigningKeyNotRecognised, LicenseKeyState.NotVerified],
            states);
    }

    [Fact]
    public void WrongProductBeforeSigningKey()
    {
        using var stranger = new Vendor();
        var key = stranger.Issue("zenith.commerce-shipping", LicenseRole.Base);

        var row = _vendor.Evaluate(Product, key.KeyString).Rows[0];

        Assert.Equal(LicenseKeyState.WrongProduct, row.State);
        Assert.Equal("zenith.commerce-shipping", row.ClaimedProductId);
        Assert.Equal(key.KeyIdentifier, row.KeyIdentifier);
    }

    [Fact]
    public void EditedIdentifierIsNotVerified()
    {
        var key = _vendor.Issue(Product, LicenseRole.Base);
        var edited = "LIC-8F3AK-M7RXB-7Q2D" + key.KeyString[20..];

        var row = _vendor.Evaluate(Product, edited).Rows[0];

        Assert.Equal(LicenseKeyState.NotVerified, row.State);
        Assert.Equal("LIC-8F3AK-M7RXB-7Q2D", row.KeyIdentifier);
    }

    // 6.3

    [Fact]
    public void EditedKeyReportsClaimedIdentifiersOnly()
    {
        var key = _vendor.Issue(Product, LicenseRole.Base, expires: "2027-03-01", features: LicenseFeature.Number("max-orders", 500m));
        var json = KeyText.PayloadJson(key.KeyString)
            .Replace("2027-03-01", "2099-12-31", StringComparison.Ordinal)
            .Replace("500", "999999", StringComparison.Ordinal);

        var result = _vendor.Evaluate(Product, KeyText.WithPayload(key.KeyString, json));
        var row = result.Rows[0];

        Assert.Equal(LicenseKeyState.NotVerified, row.State);
        Assert.False(row.IsVerified);
        Assert.Equal(Product, row.ClaimedProductId);
        Assert.Equal(key.KeyIdentifier, row.KeyIdentifier);
        Assert.Null(row.License);
        Assert.False(result.Product.IsLicensed);
        Assert.DoesNotContain(ResultText.AllStrings(result), s => s.Contains("2099", StringComparison.Ordinal) || s.Contains("999999", StringComparison.Ordinal));
    }

    [Fact]
    public void FailedKeyReportsNoVendorTag()
    {
        using var stranger = new Vendor();
        var untrusted = stranger.Issue(Product, LicenseRole.Base, vendorTag: "SHOP-2026-000123").KeyString;
        var trusted = _vendor.Issue(Product, LicenseRole.Base, vendorTag: "SHOP-2026-000123").KeyString;
        var altered = KeyText.WithPayload(trusted, KeyText.PayloadJson(trusted).Replace("base", "add-on", StringComparison.Ordinal));

        var result = _vendor.Evaluate(Product, untrusted, altered);

        Assert.Equal(LicenseKeyState.SigningKeyNotRecognised, result.Rows[0].State);
        Assert.Equal(LicenseKeyState.NotVerified, result.Rows[1].State);
        Assert.DoesNotContain(ResultText.AllStrings(result), s => s.Contains("SHOP", StringComparison.Ordinal));
    }

    [Fact]
    public void FailedKeyTakesNoPartInSupersedingOrCombining()
    {
        var valid = _vendor.Issue(Product, LicenseRole.Base, at: "2026-03-01T09:14:00Z", features: LicenseFeature.Number("max-orders", 500m));
        var later = _vendor.Issue(Product, LicenseRole.Base, at: "2026-09-28T09:14:00Z", reference: valid.Reference,
            features: LicenseFeature.Number("max-orders", 2000m));
        var mangled = later.KeyString.Remove(100, 1);

        var result = _vendor.Evaluate(Product, valid.KeyString, mangled);

        Assert.Equal(LicenseKeyState.Valid, result.Rows[0].State);
        Assert.Equal(500m, result.Product.GetFeature("max-orders")!.Number);
    }

    // 6.4

    [Fact]
    public void DuplicateCountsOnce()
    {
        var key = Base(LicenseFeature.Number("max-orders", 500m));

        var result = _vendor.Evaluate(Product, key, key);

        Assert.Equal(LicenseKeyState.Valid, result.Rows[0].State);
        Assert.Equal(LicenseKeyState.Duplicate, result.Rows[1].State);
        Assert.Equal(0, result.Rows[1].DuplicateOf);
        Assert.True(result.Rows[1].IsVerified);
        Assert.NotNull(result.Rows[1].License);
        Assert.Equal(500m, result.Product.GetFeature("max-orders")!.Number);
    }

    [Fact]
    public void DuplicateDiffersOnlyInWhitespace()
    {
        var key = Base();

        var rows = _vendor.Evaluate(Product, key, key + "\n").Rows;

        Assert.Equal(LicenseKeyState.Duplicate, rows[1].State);
        Assert.Equal(0, rows[1].DuplicateOf);
    }

    [Fact]
    public void DuplicateOfSupersededKeyIsDuplicate()
    {
        var old = _vendor.Issue(Product, LicenseRole.Base, at: "2025-03-01T10:02:00Z");
        var renewed = _vendor.Issue(Product, LicenseRole.Base, at: "2026-03-01T09:14:00Z", reference: old.Reference);

        var rows = _vendor.Evaluate(Product, old.KeyString, renewed.KeyString, old.KeyString).Rows;

        Assert.Equal([LicenseKeyState.Superseded, LicenseKeyState.Valid, LicenseKeyState.Duplicate], rows.Select(r => r.State));
    }

    // 6.5 / 6.6: rules beyond docs/license-examples.md examples 3 to 7 (in ExampleTests).

    [Fact]
    public void SupersedingIsScopedToReference()
    {
        var a = _vendor.Issue(Product, LicenseRole.Base, at: "2025-03-01T10:02:00Z");
        var b = _vendor.Issue(Product, LicenseRole.Base, at: "2026-03-01T09:14:00Z");

        var rows = _vendor.Evaluate(Product, a.KeyString, b.KeyString).Rows;

        Assert.All(rows, r => Assert.Equal(LicenseKeyState.Valid, r.State));
    }

    [Fact]
    public void SupersededRowNamesLatestKeyAndNoRoleNoteWhenSameRole()
    {
        var old = _vendor.Issue(Product, LicenseRole.Base, at: "2025-03-01T10:02:00Z");
        var renewed = _vendor.Issue(Product, LicenseRole.Base, at: "2026-03-01T09:14:00Z", reference: old.Reference);

        var rows = _vendor.Evaluate(Product, renewed.KeyString, old.KeyString).Rows;

        Assert.Equal(0, rows[1].SupersededBy);
        Assert.Null(rows[1].SupersededRoleChange);
        Assert.False(rows[1].HasVendorError);
    }

    // 6.7

    [Theory]
    [InlineData("2027-03-01T23:59:59Z", LicenseKeyState.Valid)]
    [InlineData("2027-03-01T23:59:59.999Z", LicenseKeyState.Valid)]
    [InlineData("2027-03-02T00:00:00Z", LicenseKeyState.Expired)]
    public void ExpiryEndOfDayUtc(string now, LicenseKeyState state)
    {
        var key = _vendor.Issue(Product, LicenseRole.Base, expires: "2027-03-01").KeyString;

        Assert.Equal(state, _vendor.EvaluateAt(now, Product, key).Rows[0].State);
    }

    [Fact]
    public void ExpiryUsesUtcNotClockOffset()
    {
        var key = _vendor.Issue(Product, LicenseRole.Base, expires: "2027-03-01").KeyString;
        var clock = new FixedClock(new DateTimeOffset(2027, 3, 1, 20, 0, 0, TimeSpan.FromHours(-8)));

        var row = new LicenseEvaluator(_vendor.Trusted, clock).Evaluate(Product, [key]).Rows[0];

        Assert.Equal(LicenseKeyState.Expired, row.State);
    }

    [Fact]
    public void NoExpiryNeverExpires()
    {
        var key = Base();

        Assert.Equal(LicenseKeyState.Valid, _vendor.EvaluateAt("9999-12-31T23:59:59Z", Product, key).Rows[0].State);
    }

    // 6.9

    [Fact]
    public void VerifiedRowReportsVendorTagOrNone()
    {
        var tagged = _vendor.Issue(Product, LicenseRole.Base, vendorTag: "SHOP-2026-000123");
        var untagged = _vendor.Issue(Product, LicenseRole.AddOn);

        var rows = _vendor.Evaluate(Product, tagged.KeyString, untagged.KeyString, tagged.KeyString).Rows;

        Assert.Equal("SHOP-2026-000123", rows[0].License!.VendorTag);
        Assert.Null(rows[1].License!.VendorTag);
        Assert.Equal("SHOP-2026-000123", rows[2].License!.VendorTag);
        Assert.Equal(LicenseKeyState.Duplicate, rows[2].State);
    }

    // 7.1

    [Fact]
    public void MixedSwitchAndNumberConflict()
    {
        var result = _vendor.Evaluate(Product, Base(LicenseFeature.Switch("seats")), AddOn(LicenseFeature.Number("seats", 5m)));

        Assert.Equal(["seats"], result.Product.ConflictingFeatures);
        Assert.Null(result.Product.GetFeature("seats"));
        Assert.All(result.Rows, r => Assert.Equal(["seats"], r.ConflictingFeatures));
    }

    [Fact]
    public void ConflictOnlyAmongCountedKeys()
    {
        var expired = _vendor.Issue(Product, LicenseRole.AddOn, expires: "2026-10-01", features: LicenseFeature.Text("region", "us")).KeyString;
        var result = _vendor.EvaluateAt("2026-10-05T00:00:00Z", Product, Base(LicenseFeature.Text("region", "eu")), expired);

        Assert.Empty(result.Product.ConflictingFeatures);
        Assert.Equal("eu", result.Product.GetFeature("region")!.Text);
        Assert.Empty(result.Rows[1].ConflictingFeatures);
    }

    [Fact]
    public void LargeSumsStayExact()
    {
        var keys = Enumerable.Range(0, 10)
            .Select(_ => AddOn(LicenseFeature.Number("units", 99_999_999_999.9999m)))
            .Prepend(Base(LicenseFeature.Number("units", 0.0001m)))
            .ToArray();

        var result = _vendor.Evaluate(Product, keys);

        Assert.Equal(999_999_999_999.9991m, result.Product.GetFeature("units")!.Number);
    }

    // 7.2

    [Fact]
    public void LookupIgnoresAsciiCase()
    {
        var result = _vendor.Evaluate(Product, Base(LicenseFeature.Number("max-orders", 500m), LicenseFeature.Switch("pid")));

        Assert.Equal(500m, result.Product.GetFeature("Max-Orders")!.Number);
        Assert.True(result.Product.IsGranted("MAX-ORDERS"));
        Assert.True(result.Product.IsGranted("PID"));
        Assert.False(result.Product.IsGranted("unknown"));
        Assert.False(result.Product.IsGranted("max-orders "));
    }

    [Fact]
    public void LookupIsCultureIndependent()
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
            var result = _vendor.Evaluate(Product, Base(LicenseFeature.Switch("pid"), LicenseFeature.Switch("ai-assist"), LicenseFeature.Switch("key")));

            Assert.True(result.Product.IsGranted("PID"));
            Assert.True(result.Product.IsGranted("AI-ASSIST"));
            Assert.False(result.Product.IsGranted("PİD"));
            Assert.False(result.Product.IsGranted("pıd"));
            Assert.False(result.Product.IsGranted("Key"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Fact]
    public void NotLicensedGrantsNothing()
    {
        var result = _vendor.Evaluate(Product, AddOn(LicenseFeature.Switch("ai-assist")));

        Assert.False(result.Product.IsLicensed);
        Assert.False(result.Product.IsGranted("ai-assist"));
        Assert.Empty(result.Product.Features);
    }

    // 7.3

    [Fact]
    public void ProductResultSameForEveryOrder()
    {
        var reference = _vendor.Issue(Product, LicenseRole.Base, at: "2026-03-01T09:14:00Z").Reference;
        string[] keys =
        [
            _vendor.Issue(Product, LicenseRole.Base, at: "2026-03-01T09:14:00Z", reference: reference,
                features: [LicenseFeature.Number("max-orders", 500m), LicenseFeature.Text("licensed-domain", "example.com")]).KeyString,
            _vendor.Issue(Product, LicenseRole.Base, at: "2026-09-28T14:30:22Z", reference: reference,
                features: [LicenseFeature.Number("max-orders", 2000m), LicenseFeature.Text("licensed-domain", "example.com")]).KeyString,
            _vendor.Issue(Product, LicenseRole.Base, at: "2026-09-28T14:30:22Z", reference: reference,
                features: LicenseFeature.Number("max-orders", 500m)).KeyString,
            AddOn(LicenseFeature.Number("max-orders", 1000m), LicenseFeature.Switch("ai-assist")),
            AddOn(LicenseFeature.Text("licensed-domain", "exmaple.com"), LicenseFeature.Number("storage-gb", 2.5m)),
            _vendor.Issue(Product, LicenseRole.AddOn, expires: "2026-10-01", features: LicenseFeature.Text("tenant", "t-1")).KeyString,
            "Hunter2!",
        ];
        keys = [.. keys, keys[3]];

        var expected = ResultText.Describe(_vendor.Evaluate(Product, keys).Product);
        foreach (var permutation in Permutations(keys.Length).Take(2000))
        {
            var shuffled = permutation.Select(i => keys[i]).ToArray();
            Assert.Equal(expected, ResultText.Describe(_vendor.Evaluate(Product, shuffled).Product));
        }
    }

    [Fact]
    public void NoKeyMaterialInResult()
    {
        using var stranger = new Vendor();
        var reference = _vendor.Issue(Product, LicenseRole.Base).Reference;
        string[] keys =
        [
            Base(LicenseFeature.Number("max-orders", 500m), LicenseFeature.Text("licensed-domain", "example.com")),
            AddOn(LicenseFeature.Switch("ai-assist")),
            _vendor.Issue(Product, LicenseRole.Base, reference: reference, vendorTag: "SHOP-1").KeyString,
            stranger.Issue(Product, LicenseRole.Base).KeyString,
            stranger.Issue("zenith.forms", LicenseRole.Base).KeyString,
            Base()[..60],
            "Hunter2!",
        ];
        keys = [.. keys, keys[0]];

        var strings = ResultText.AllStrings(_vendor.Evaluate(Product, keys)).ToList();

        foreach (var key in keys)
        {
            foreach (var segment in key.Split('.').Skip(1).Where(s => s.Length >= 8))
            {
                for (var i = 0; i + 8 <= segment.Length; i++)
                {
                    var window = segment.Substring(i, 8);
                    Assert.DoesNotContain(strings, s => s.Contains(window, StringComparison.Ordinal));
                }
            }
        }
    }

    // 7.4

    [Fact]
    public void GarbageNeverThrows()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(300);
        var key = Base();
        string?[] inputs =
        [
            "",
            null,
            key[..(key.Length / 2)],
            "Hunter2!",
            System.Text.Encoding.Latin1.GetString(randomBytes),
            Convert.ToBase64String(randomBytes),
            "LIC-8F3AK-M7RXB-7Q2D." + Convert.ToBase64String(randomBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=') + "." + new string('A', 86),
            "LIC-8F3AK-M7RXB-7Q2D.e30." + new string('A', 86),
            "LIC-8F3AK-M7RXB-7Q2D.W10." + new string('A', 86),
            "LIC-8F3AK-M7RXB-7Q2D.." ,
            "...",
            "LIC-8F3AK-M7RXB-7Q2D._w." + new string('A', 86),
            new string('.', 10_000),
            new string('[', 100_000),
        ];

        var rows = _vendor.Evaluate(Product, inputs).Rows;

        Assert.Equal(inputs.Length, rows.Count);
        Assert.All(rows, r => Assert.Equal(LicenseKeyState.Unreadable, r.State));
    }

    [Fact]
    public void DeeplyNestedOrInvalidUtf8PayloadNeverThrows()
    {
        string Raw(string json) => _vendor.SignRaw(json);
        string[] inputs =
        [
            Raw(_vendor.Payload(",\"features\":{\"a\":" + new string('[', 500) + new string(']', 500) + "}")),
            Base()[..21] + Umbraco.Community.Licensing.Internal.Base64UrlText.Encode([(byte)0x7B, (byte)0x22, (byte)0xFF, (byte)0x22, (byte)0x3A, (byte)0x31, (byte)0x7D]) + "." + new string('A', 86),
            _vendor.Keys.SigningKeyId,
        ];

        var rows = _vendor.Evaluate(Product, inputs).Rows;

        Assert.All(rows, r => Assert.Equal(LicenseKeyState.Unreadable, r.State));
    }

    private static IEnumerable<int[]> Permutations(int n)
    {
        var items = Enumerable.Range(0, n).ToArray();
        return Permute(items, 0);

        static IEnumerable<int[]> Permute(int[] items, int k)
        {
            if (k == items.Length)
            {
                yield return (int[])items.Clone();
                yield break;
            }

            for (var i = k; i < items.Length; i++)
            {
                (items[k], items[i]) = (items[i], items[k]);
                foreach (var p in Permute(items, k + 1))
                {
                    yield return p;
                }

                (items[k], items[i]) = (items[i], items[k]);
            }
        }
    }
}

internal static class ResultText
{
    public static string Describe(ProductLicense product) =>
        $"{product.ProductId} licensed={product.IsLicensed} bases={product.ValidBaseCount} "
        + string.Join(",", product.Features.Select(f => $"{f.Key}:{f.Value.Kind}:{f.Value}"))
        + " conflicts=" + string.Join(",", product.ConflictingFeatures);

    public static IEnumerable<string> AllStrings(LicenseEvaluation result)
    {
        yield return Describe(result.Product);
        foreach (var row in result.Rows)
        {
            yield return $"{row.Position} {row.State} {row.KeyIdentifier} {row.ClaimedProductId} {row.DuplicateOf} {row.SupersededBy} {row.SupersededRoleChange}";
            foreach (var s in row.TiedWith.Concat(row.ConflictingFeatures))
            {
                yield return s;
            }

            if (row.License is { } license)
            {
                yield return license.ToString();
                foreach (var feature in license.Features)
                {
                    yield return $"{feature.Key}:{feature.Value}";
                }
            }
        }
    }
}

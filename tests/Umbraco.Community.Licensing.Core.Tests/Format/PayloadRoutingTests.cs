using System.Text;

namespace Umbraco.Community.Licensing.Core.Tests.Format;

public class PayloadRoutingTests
{
    private static bool TryRead(string json, out string product, out string keyId) =>
        PayloadRouting.TryRead(Encoding.UTF8.GetBytes(json), out product, out keyId);

    [Fact]
    public void TryRead_Adr0002Sample_ReturnsProductAndKeyId()
    {
        var json = "{\"signingKeyId\":\"0F8NSYaNR_Q\",\"product\":\"acme.commerce\",\"issued\":\"2026-09-01T08:05:12Z\","
            + "\"features\":{\"ecommerce\":true,\"max-orders\":2500}}";

        Assert.True(TryRead(json, out var product, out var keyId));
        Assert.Equal("acme.commerce", product);
        Assert.Equal("0F8NSYaNR_Q", keyId);
    }

    [Fact]
    public void TryRead_UnknownFieldsAndBadOtherFields_AreIgnored()
    {
        // Lenient: nothing but product and signingKeyId is looked at, and content rules do not apply.
        Assert.True(TryRead(
            "{\"x\":[1,{\"y\":null}],\"product\":\"NOT A PRODUCT\",\"issued\":42,\"signingKeyId\":\"k\",\"features\":{\"a\":false}}",
            out var product,
            out var keyId));
        Assert.Equal("NOT A PRODUCT", product);
        Assert.Equal("k", keyId);
    }

    [Fact]
    public void TryRead_WhitespaceAndEscapedForms_AreRead()
    {
        Assert.True(TryRead(" { \"signingKeyId\" : \"0F8NSYaNR_Q\" , \"\\u0070roduct\" : \"acme.\\u0063ommerce\" } ", out var product, out var keyId));
        Assert.Equal("acme.commerce", product);
        Assert.Equal("0F8NSYaNR_Q", keyId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("[]")]
    [InlineData("\"product\"")]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("true")]
    public void TryRead_NonObjectJson_IsUnreadable(string json) => Assert.False(TryRead(json, out _, out _));

    [Theory]
    [InlineData("{\"signingKeyId\":\"0F8NSYaNR_Q\"}")]
    [InlineData("{\"product\":\"acme.commerce\"}")]
    [InlineData("{}")]
    public void TryRead_MissingField_IsUnreadable(string json) => Assert.False(TryRead(json, out _, out _));

    [Theory]
    [InlineData("{\"signingKeyId\":\"k\",\"product\":1}")]
    [InlineData("{\"signingKeyId\":\"k\",\"product\":null}")]
    [InlineData("{\"signingKeyId\":\"k\",\"product\":true}")]
    [InlineData("{\"signingKeyId\":\"k\",\"product\":[\"a.b\"]}")]
    [InlineData("{\"signingKeyId\":7,\"product\":\"a.b\"}")]
    [InlineData("{\"signingKeyId\":{},\"product\":\"a.b\"}")]
    public void TryRead_NonStringField_IsUnreadable(string json) => Assert.False(TryRead(json, out _, out _));

    [Theory]
    [InlineData("{\"signingKeyId\":\"k\",\"product\":\"a.b\",\"product\":\"a.b\"}")]
    [InlineData("{\"signingKeyId\":\"k\",\"signingKeyId\":\"j\",\"product\":\"a.b\"}")]
    public void TryRead_RepeatedField_IsUnreadable(string json) => Assert.False(TryRead(json, out _, out _));

    [Theory]
    [InlineData("{\"signingKeyId\":\"k\",\"product\":\"a.b\"")]
    [InlineData("{\"signingKeyId\":\"k\",\"product\":\"a.")]
    [InlineData("{\"signingKeyId\":\"k\",\"prod")]
    [InlineData("{\"signingKeyId\":\"k\",")]
    [InlineData("{")]
    [InlineData("{\"signingKeyId\":\"k\",\"product\":\"a.b\",}")]
    [InlineData("{\"signingKeyId\":\"k\" \"product\":\"a.b\"}")]
    [InlineData("{\"signingKeyId\":\"k\",\"product\":\"a.b\"} x")]
    [InlineData("{\"signingKeyId\":\"k\",\"product\":\"a.b\"}{}")]
    [InlineData("{\"signingKeyId\":\"k\",\"product\":\"a.b\",\"x\":[1,2}")]
    public void TryRead_TruncatedOrMalformedJson_IsUnreadable(string json) => Assert.False(TryRead(json, out _, out _));

    [Fact]
    public void TryRead_InvalidUtf8_IsUnreadable()
    {
        byte[] bytes = [.. "{\"signingKeyId\":\"k\",\"product\":\"a"u8, 0xC3, 0x28, .. "b\"}"u8];

        Assert.False(PayloadRouting.TryRead(bytes, out _, out _));
    }

    [Fact]
    public void TryRead_InvalidUtf8InIgnoredField_IsUnreadable()
    {
        byte[] bytes = [.. "{\"x\":\""u8, 0xFF, .. "\",\"signingKeyId\":\"k\",\"product\":\"a.b\"}"u8];

        Assert.False(PayloadRouting.TryRead(bytes, out _, out _));
    }

    [Theory]
    [InlineData("{\"signingKeyId\":\"k\",\"product\":\"a\\uD800b\"}")]
    [InlineData("{\"signingKeyId\":\"k\",\"product\":\"a\\uDC00\"}")]
    [InlineData("{\"signingKeyId\":\"\\uD800\",\"product\":\"a.b\"}")]
    [InlineData("{\"signingKeyId\":\"k\",\"product\":\"\\uD800\\u0041\"}")]
    public void TryRead_EscapedLoneSurrogate_IsUnreadable(string json) => Assert.False(TryRead(json, out _, out _));

    [Fact]
    public void TryRead_EscapedSurrogatePair_IsRead()
    {
        Assert.True(TryRead("{\"signingKeyId\":\"k\",\"product\":\"a\\uD83D\\uDE00\"}", out var product, out _));
        Assert.Equal("a\U0001F600", product);
    }

    [Theory]
    [InlineData(10_000)]
    [InlineData(100_000)]
    public void TryRead_DeeplyNestedArrays_IsUnreadableWithoutThrowing(int depth)
    {
        var json = "{\"x\":" + new string('[', depth) + new string(']', depth) + ",\"signingKeyId\":\"k\",\"product\":\"a.b\"}";

        Assert.False(TryRead(json, out _, out _));
        Assert.False(TryRead(new string('[', depth), out _, out _));
        Assert.False(TryRead("{\"signingKeyId\":\"k\",\"product\":\"a.b\",\"x\":" + new string('{', depth), out _, out _));
    }

    [Fact]
    public void TryRead_Failure_LeavesOutputsEmpty()
    {
        Assert.False(TryRead("[]", out var product, out var keyId));
        Assert.Equal(string.Empty, product);
        Assert.Equal(string.Empty, keyId);
    }
}

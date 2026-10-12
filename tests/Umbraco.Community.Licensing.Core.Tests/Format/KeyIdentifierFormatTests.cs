namespace Umbraco.Community.Licensing.Core.Tests.Format;

public class KeyIdentifierReferenceOfTests
{
    [Theory]
    [InlineData("LIC-8F3AK-M7RXB-7Q2D", "8F3AKM7RXB")]
    [InlineData("8F3AK-M7RXB-7Q2D", "8F3AKM7RXB")]
    [InlineData("A-8F3AK-M7RXB-7Q2D", "8F3AKM7RXB")]
    [InlineData("ABCDEFGHIJ123456-8F3AK-M7RXB-7Q2D", "8F3AKM7RXB")]
    public void ReferenceOf_ReturnsTenCharactersWithoutHyphensOrPrefix(string identifier, string reference)
    {
        Assert.Matches(KeyIdentifierFormat.IdentifierPattern, identifier);
        Assert.Equal(reference, KeyIdentifierFormat.ReferenceOf(identifier));
    }
}

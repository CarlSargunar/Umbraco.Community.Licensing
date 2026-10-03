using System.Buffers.Text;

namespace Umbraco.Community.Licensing.Internal;

/// <summary>RFC 4648 section 5 base64url without padding (ADR-0001).</summary>
internal static class Base64UrlText
{
    public static string Encode(ReadOnlySpan<byte> bytes) => Base64Url.EncodeToString(bytes);

    /// <summary>Decodes strictly: only <c>A-Z a-z 0-9 - _</c>, no padding, no whitespace.</summary>
    public static bool TryDecode(string text, out byte[] bytes)
    {
        bytes = [];
        if (text.Length == 0 || text.Length % 4 == 1)
        {
            return false;
        }

        foreach (var c in text)
        {
            if (!(char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
            {
                return false;
            }
        }

        try
        {
            bytes = Base64Url.DecodeFromChars(text);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

namespace QsNet.Constants;

/// <summary>
///     Hex table of all 256 characters
/// </summary>
public static class HexTable
{
    /// <summary>
    ///     Hex table containing percent-encoded strings for all 256 byte values
    /// </summary>
    internal static readonly string[] Table = Create();

    private static string[] Create()
    {
        var arr = new string[256];
        for (var i = 0; i < 256; i++)
        {
            var chars = new char[3];
            chars[0] = '%';
            chars[1] = GetHexChar((i >> 4) & 0xF);
            chars[2] = GetHexChar(i & 0xF);
            arr[i] = new string(chars);
        }
        return arr;
    }

    private static char GetHexChar(int n)
    {
        return (char)(n < 10 ? ('0' + n) : ('A' + (n - 10)));
    }
}
using System.Text;

namespace QsNet.Internal;

/// <summary>
///     Centralized charset validation helpers used by encode/decode option validation.
/// </summary>
internal static class CharsetHelper
{
    /// <summary>
    ///     Determines whether the provided encoding is one of the supported query-string charsets.
    /// </summary>
    /// <param name="encoding">The encoding to validate.</param>
    /// <returns><see langword="true" /> when the charset is UTF-8 or ISO-8859-1; otherwise <see langword="false" />.</returns>
    internal static bool IsSupportedCharset(Encoding? encoding)
    {
        return encoding is { CodePage: 65001 or 28591 };
    }
}
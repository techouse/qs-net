namespace QsNet.Enums;

/// <summary>
///     Indicates the decoding context for a scalar token.
/// </summary>
/// <remarks>
///     Use <see cref="Key" /> when decoding a key or key segment so the decoder can apply
///     key-specific rules (for example, preserving percent-encoded dots <c>%2E</c>/<c>%2e</c>
///     until after key splitting). Use <see cref="Value" /> for normal value decoding.
/// </remarks>
public enum DecodeKind
{
    /// <summary>
    ///     The token is a <b>key</b> (or a key segment).
    /// </summary>
    /// <remarks>
    ///     Implementations typically avoid turning <c>%2E</c>/<c>%2e</c> into a literal dot
    ///     before key splitting when this kind is used, to match the semantics of the
    ///     reference <c>qs</c> library.
    /// </remarks>
    Key,

    /// <summary>
    ///     The token is a <b>value</b>.
    /// </summary>
    /// <remarks>
    ///     Values are decoded normally (e.g., percent-decoding and charset handling)
    ///     without any key-specific protections.
    /// </remarks>
    Value
}
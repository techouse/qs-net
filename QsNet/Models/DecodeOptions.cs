using System;
using System.Text;
using QsNet.Enums;
using QsNet.Internal;

namespace QsNet.Models;

/// <summary>
///     A function that decodes a value from a query string or form data. It takes a value and an
///     optional encoding, returning the decoded value.
/// </summary>
/// <param name="value">The encoded value to decode.</param>
/// <param name="encoding">The character encoding to use for decoding, if any.</param>
/// <returns>The decoded value, or null if the value is not present.</returns>
/// <remarks>
///     When this delegate is used to decode keys (e.g., via <see cref="DecodeOptions.DecodeKey"/>),
///     it must return either a <see cref="string"/> or <see langword="null"/>; returning any other
///     type will cause <see cref="InvalidOperationException"/> at the call site.
/// </remarks>
public delegate object? Decoder(string? value, Encoding? encoding);

/// <summary>
///     A function that decodes a value from a query string or form data with key/value context.
///     The <see cref="DecodeKind" /> indicates whether the token is a key (or key segment) or a value.
/// </summary>
/// <param name="value">The encoded value to decode.</param>
/// <param name="encoding">The character encoding to use for decoding, if any.</param>
/// <param name="kind">Whether this token is a <see cref="DecodeKind.Key" /> or <see cref="DecodeKind.Value" />.</param>
/// <returns>The decoded value, or null if the value is not present.</returns>
/// <remarks>
///     When <paramref name="kind"/> is <see cref="DecodeKind.Key"/>, the decoder must return a
///     <see cref="string"/> or <see langword="null"/>. Returning any other type will cause
///     <see cref="InvalidOperationException"/> at the call site (see <see cref="DecodeOptions.DecodeKey"/>).
/// </remarks>
public delegate object? KindAwareDecoder(string? value, Encoding? encoding, DecodeKind kind);

/// <summary>
///     Options that configure the output of Qs.Decode.
/// </summary>
public sealed class DecodeOptions
{
    private readonly bool? _allowDots;

    private readonly bool? _decodeDotInKeys;

    /// <summary>
    ///     Initializes a new instance of the DecodeOptions class.
    /// </summary>
    public DecodeOptions()
    {
        if (
            !Equals(Charset, Encoding.UTF8) &&
#if NETSTANDARD2_0
            Charset.CodePage != 28591
#else
            !Equals(Charset, Encoding.Latin1)
#endif
        )
            throw new ArgumentException("Invalid charset");

        if (ParameterLimit <= 0)
            throw new ArgumentException("Parameter limit must be positive");
    }

    /// <summary>
    ///     Set to true to allow empty list values inside dictionaries in the encoded input.
    /// </summary>
    public bool AllowEmptyLists { get; init; }

    /// <summary>
    ///     Set to true to allow sparse lists in the encoded input.
    ///     Note: If set to true, the lists will contain null values for missing values.
    /// </summary>
    public bool AllowSparseLists { get; init; }

    /// <summary>
    ///     QS will limit specifying indices in a list to a maximum index of 20. Any list members with
    ///     an index of greater than 20 will instead be converted to a dictionary with the index as the key.
    ///     This is needed to handle cases when someone sent, for example, a[999999999] and it will
    ///     take significant time to iterate over this huge list. This limit can be overridden by passing
    ///     a ListLimit option.
    /// </summary>
    public int ListLimit { get; init; } = 20;

    /// <summary>
    ///     The character encoding to use when decoding the input.
    /// </summary>
    public Encoding Charset { get; init; } = Encoding.UTF8;

    /// <summary>
    ///     Some services add an initial utf8=✓ value to forms so that old InternetExplorer versions
    ///     are more likely to submit the form as UTF-8. Additionally, the server can check the value
    ///     against wrong encodings of the checkmark character and detect that a query string or
    ///     application/x-www-form-urlencoded body was *not* sent as UTF-8, e.g. if the form had an
    ///     accept-charset parameter or the containing page had a different character set.
    ///     QS supports this mechanism via the CharsetSentinel option. If specified, the UTF-8 parameter
    ///     will be omitted from the returned dictionary. It will be used to switch to ISO-8859-1/UTF-8 mode
    ///     depending on how the checkmark is encoded.
    ///     Important: When you specify both the Charset option and the CharsetSentinel option, the
    ///     charset will be overridden when the request contains a UTF-8 parameter from which the actual
    ///     charset can be deduced. In that sense the charset will behave as the default charset rather
    ///     than the authoritative charset.
    /// </summary>
    public bool CharsetSentinel { get; init; }

    /// <summary>
    ///     Set to true to parse the input as a comma-separated value.
    ///     Note: nested dictionaries, such as 'a={b:1},{c:d}' are not supported.
    /// </summary>
    public bool Comma { get; init; }

    /// <summary>
    ///     The delimiter to use when splitting key-value pairs in the encoded input.
    /// </summary>
    public IDelimiter Delimiter { get; init; } = new StringDelimiter("&");

    /// <summary>
    ///     By default, when nesting dictionaries QS will only decode up to 5 children deep. This depth can be
    ///     overridden by setting the Depth. The depth limit helps mitigate abuse when qs is used to
    ///     parse user input, and it is recommended to keep it a reasonably small number.
    /// </summary>
    public int Depth { get; init; } = 5;

    /// <summary>
    ///     For similar reasons, by default QS will only parse up to 1000 parameters. This can be
    ///     overridden by passing a ParameterLimit option.
    /// </summary>
    public int ParameterLimit { get; init; } = 1000;

    /// <summary>
    ///     Change the duplicate key handling strategy.
    /// </summary>
    public Duplicates Duplicates { get; init; } = Duplicates.Combine;

    /// <summary>
    ///     Set to true to ignore the leading question mark query prefix in the encoded input.
    /// </summary>
    public bool IgnoreQueryPrefix { get; init; }

    /// <summary>
    ///     Set to true to interpret HTML numeric entities (&amp;#...;) in the encoded input.
    /// </summary>
    public bool InterpretNumericEntities { get; init; }

    /// <summary>
    ///     To disable list parsing entirely, set ParseLists to false.
    /// </summary>
    public bool ParseLists { get; init; } = true;

    /// <summary>
    ///     Set to true to add a layer of protection by throwing an error when the limit is exceeded,
    ///     allowing you to catch and handle such cases.
    /// </summary>
    public bool StrictDepth { get; init; }

    /// <summary>
    ///     Set to true to decode values without = to null.
    /// </summary>
    public bool StrictNullHandling { get; init; }

    /// <summary>
    ///     Set to true to throw an error when the limit is exceeded.
    /// </summary>
    public bool ThrowOnLimitExceeded { get; init; }

    /// <summary>
    ///     Set to true to parse dot dictionary notation in the encoded input.
    ///     Note: when not explicitly set, this property implicitly evaluates to true
    ///     if <see cref="DecodeDotInKeys" /> is true, to keep option combinations coherent.
    /// </summary>
    public bool AllowDots
    {
        init => _allowDots = value;
        get => _allowDots ?? _decodeDotInKeys == true; // implied true when DecodeDotInKeys is true
    }

    /// <summary>
    ///     Set a Decoder to affect the decoding of the input.
    /// </summary>
    public Decoder? Decoder { private get; init; }

    /// <summary>
    ///     Optional decoder that receives key/value <see cref="DecodeKind" /> context.
    ///     When provided, this takes precedence over <see cref="Decoder" />.
    /// </summary>
    public KindAwareDecoder? DecoderWithKind { private get; init; }

    /// <summary>
    ///     Gets whether to decode dots in keys.
    /// </summary>
    public bool DecodeDotInKeys
    {
        init => _decodeDotInKeys = value;
        get => _decodeDotInKeys ?? false;
    }

    /// <summary>
    ///     Decode a single scalar token using the most specific decoder available.
    ///     If <see cref="DecoderWithKind" /> is provided, it is always used (even when it returns null).
    ///     Otherwise, the legacy two-argument <see cref="Decoder" /> is used; if neither is set,
    ///     a library default is used.
    /// </summary>
    public object? Decode(string? value, Encoding? encoding = null, DecodeKind kind = DecodeKind.Value)
    {
        if (kind == DecodeKind.Key && _decodeDotInKeys == true && _allowDots == false)
            throw new InvalidOperationException(
                "Invalid DecodeOptions: DecodeDotInKeys=true requires AllowDots=true when decoding keys."
            );
        var d3 = DecoderWithKind;
        if (d3 is not null) return d3.Invoke(value, encoding, kind);

        var d = Decoder;
        return d is not null ? d.Invoke(value, encoding) : DefaultDecode(value, encoding);
    }

    /// <summary>
    ///     Decode a key (or key segment). Returns a string or null.
    /// </summary>
    public string? DecodeKey(string? value, Encoding? encoding = null)
    {
        var decoded = Decode(value, encoding, DecodeKind.Key);
        return decoded switch
        {
            null => null,
            string s => s,
            _ => throw new InvalidOperationException(
                $"Key decoder must return a string or null; got {decoded.GetType().FullName}. " +
                "If using a custom decoder, ensure it returns string for keys.")
        };
    }

    /// <summary>
    ///     Decode a value token. Returns any scalar (string/number/etc.) or null.
    /// </summary>
    public object? DecodeValue(string? value, Encoding? encoding = null)
    {
        return Decode(value, encoding);
    }

    /// <summary>
    ///     Default decoder when no custom decoder is supplied. Keys are decoded identically
    ///     to values using <see cref="Utils.Decode" /> with the provided encoding.
    /// </summary>
    private static string? DefaultDecode(string? value, Encoding? encoding)
    {
        return value is null ? null : Utils.Decode(value, encoding);
    }


    /// <summary>
    ///     Creates a new instance of DecodeOptions with the specified properties changed.
    /// </summary>
    /// <param name="allowDots">Set to override AllowDots</param>
    /// <param name="decoder">Set to override the decoder function</param>
    /// <param name="decoderWithKind">Set to override the kind-aware decoder function</param>
    /// <param name="decodeDotInKeys">Set to override DecodeDotInKeys</param>
    /// <param name="allowEmptyLists">Set to override AllowEmptyLists</param>
    /// <param name="allowSparseLists">Set to override AllowSparseLists</param>
    /// <param name="listLimit">Set to override ListLimit</param>
    /// <param name="charset">Set to override Charset</param>
    /// <param name="charsetSentinel">Set to override CharsetSentinel</param>
    /// <param name="comma">Set to override Comma</param>
    /// <param name="delimiter">Set to override Delimiter</param>
    /// <param name="depth">Set to override Depth</param>
    /// <param name="parameterLimit">Set to override ParameterLimit</param>
    /// <param name="duplicates">Set to override Duplicates</param>
    /// <param name="ignoreQueryPrefix">Set to override IgnoreQueryPrefix</param>
    /// <param name="interpretNumericEntities">Set to override InterpretNumericEntities</param>
    /// <param name="parseLists">Set to override ParseLists</param>
    /// <param name="strictDepth">Set to override StrictDepth</param>
    /// <param name="strictNullHandling">Set to override StrictNullHandling</param>
    /// <param name="throwOnLimitExceeded">Set to override ThrowOnLimitExceeded</param>
    /// <returns>A new DecodeOptions instance with the specified changes</returns>
    public DecodeOptions CopyWith(
        bool? allowDots = null,
        Decoder? decoder = null,
        KindAwareDecoder? decoderWithKind = null,
        bool? decodeDotInKeys = null,
        bool? allowEmptyLists = null,
        bool? allowSparseLists = null,
        int? listLimit = null,
        Encoding? charset = null,
        bool? charsetSentinel = null,
        bool? comma = null,
        IDelimiter? delimiter = null,
        int? depth = null,
        int? parameterLimit = null,
        Duplicates? duplicates = null,
        bool? ignoreQueryPrefix = null,
        bool? interpretNumericEntities = null,
        bool? parseLists = null,
        bool? strictDepth = null,
        bool? strictNullHandling = null,
        bool? throwOnLimitExceeded = null
    )
    {
        return new DecodeOptions
        {
            AllowDots = allowDots ?? AllowDots,
            AllowEmptyLists = allowEmptyLists ?? AllowEmptyLists,
            AllowSparseLists = allowSparseLists ?? AllowSparseLists,
            ListLimit = listLimit ?? ListLimit,
            Charset = charset ?? Charset,
            CharsetSentinel = charsetSentinel ?? CharsetSentinel,
            Comma = comma ?? Comma,
            DecodeDotInKeys = decodeDotInKeys ?? DecodeDotInKeys,
            Decoder = decoder ?? Decoder,
            DecoderWithKind = decoderWithKind ?? DecoderWithKind,
            Delimiter = delimiter ?? Delimiter,
            Depth = depth ?? Depth,
            ParameterLimit = parameterLimit ?? ParameterLimit,
            Duplicates = duplicates ?? Duplicates,
            IgnoreQueryPrefix = ignoreQueryPrefix ?? IgnoreQueryPrefix,
            InterpretNumericEntities = interpretNumericEntities ?? InterpretNumericEntities,
            ParseLists = parseLists ?? ParseLists,
            StrictDepth = strictDepth ?? StrictDepth,
            StrictNullHandling = strictNullHandling ?? StrictNullHandling,
            ThrowOnLimitExceeded = throwOnLimitExceeded ?? ThrowOnLimitExceeded
        };
    }
}
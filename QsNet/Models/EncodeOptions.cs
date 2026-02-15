using System;
using System.Text;
using QsNet.Enums;
using QsNet.Internal;

namespace QsNet.Models;

/// <summary>
///     ValueEncoder function that takes a value, encoding, and format, and returns a String
///     representation of the value.
///     The encoder can be used to customize how values are encoded in the query string. If no encoder is
///     provided, Utils.Encode will be used by default.
/// </summary>
/// <param name="value">The value to encode</param>
/// <param name="encoding">The character encoding to use</param>
/// <param name="format">The format to use</param>
/// <returns>The encoded string representation</returns>
public delegate string ValueEncoder(object? value, Encoding? encoding, Format? format);

/// <summary>
///     DateSerializer function that takes a DateTime and returns a String representation
///     of the date.
///     This can be used to customize how DateTime objects are serialized in the encoded output. If
///     no serializer is provided, the default ISO format will be used.
/// </summary>
/// <param name="date">The date to serialize</param>
/// <returns>The serialized date string</returns>
public delegate string DateSerializer(DateTime date);

/// <summary>
///     Options that configure the output of Qs.Encode.
/// </summary>
public sealed class EncodeOptions
{
    private readonly bool? _allowDots;

    private readonly ListFormat? _listFormat;

    /// <summary>
    ///     Initializes a new instance of the EncodeOptions class.
    /// </summary>
    public EncodeOptions(ListFormat? listFormat = null)
    {
        ListFormat = listFormat;
    }

    /// <summary>
    ///     Set an Encoder to affect the encoding of values. Note: the encoder option does not apply if
    ///     Encode is false
    /// </summary>
    public ValueEncoder? Encoder { get; init; }

    /// <summary>
    ///     If you only want to override the serialization of DateTime objects, you can provide a custom
    ///     DateSerializer.
    /// </summary>
    public DateSerializer? DateSerializer { get; init; }

    /// <summary>
    ///     The list encoding format to use.
    /// </summary>
    public ListFormat? ListFormat
    {
        init => _listFormat = value;
#pragma warning disable CS0618 // Type or member is obsolete
        get =>
            _listFormat
            ?? (
                Indices.HasValue
                    ? Indices.Value
                        ? Enums.ListFormat.Indices
                        : Enums.ListFormat.Repeat
                    : Enums.ListFormat.Indices
            );
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    ///     Set to true to use dot dictionary notation in the encoded output.
    /// </summary>
    public bool AllowDots
    {
        init => _allowDots = value;
        get => _allowDots ?? EncodeDotInKeys;
    }

    /// <summary>
    ///     Convenience getter for accessing the format's formatter.
    /// </summary>
    public Formatter Formatter => Format.GetFormatter();

    /// <summary>
    ///     Set to true to add a question mark ? prefix to the encoded output.
    /// </summary>
    public bool AddQueryPrefix { get; init; }

    /// <summary>
    ///     Set to true to allow empty lists in the encoded output.
    /// </summary>
    public bool AllowEmptyLists { get; init; }

    /// <summary>
    ///     The character encoding to use.
    /// </summary>
    public Encoding Charset { get; init; } = Encoding.UTF8;

    /// <summary>
    ///     Set to true to announce the character by including an utf8=✓ parameter with the proper
    ///     encoding of the checkmark, similar to what Ruby on Rails and others do when submitting forms.
    /// </summary>
    public bool CharsetSentinel { get; init; }

    /// <summary>
    ///     The delimiter to use when joining key-value pairs in the encoded output.
    /// </summary>
    public string Delimiter { get; init; } = "&";

    /// <summary>
    ///     Set to false in order to disable encoding.
    /// </summary>
    public bool Encode { get; init; } = true;

    /// <summary>
    ///     Encode dictionary keys using dot notation by setting EncodeDotInKeys to true:
    ///     Caveat: When EncodeValuesOnly is true as well as EncodeDotInKeys, only dots in keys and
    ///     nothing else will be encoded.
    /// </summary>
    public bool EncodeDotInKeys { get; init; }

    /// <summary>
    ///     Encoding can be disabled for keys by setting the EncodeValuesOnly to true.
    /// </summary>
    public bool EncodeValuesOnly { get; init; }

    /// <summary>
    ///     The encoding format to use. The default format is Format.Rfc3986 which encodes ' ' to %20
    ///     which is backward compatible. You can also set format to Format.Rfc1738 which encodes ' '
    ///     to +.
    /// </summary>
    public Format Format { get; init; } = Format.Rfc3986;

    /// <summary>
    ///     Use the filter option to restrict which keys will be included in the encoded output. If you
    ///     pass a Function, it will be called for each key to obtain the replacement value. If you pass
    ///     a List, it will be used to select properties and list indices to be encoded.
    /// </summary>
    public IFilter? Filter { get; init; }

    /// <summary>
    ///     Set to true to completely skip encoding keys with null values.
    /// </summary>
    public bool SkipNulls { get; init; }

    /// <summary>
    ///     Set to true to distinguish between null values and empty strings. This way the encoded
    ///     string null values will have no = sign.
    /// </summary>
    public bool StrictNullHandling { get; init; }

    /// <summary>
    ///     When ListFormat is set to ListFormat.Comma, you can also set CommaRoundTrip option to true
    ///     or false, to append [] on single-item lists, so that they can round trip through a parse.
    /// </summary>
    public bool? CommaRoundTrip { get; init; }

    /// <summary>
    ///     When ListFormat is ListFormat.Comma, drop null items before joining instead of preserving empty slots.
    /// </summary>
    public bool CommaCompactNulls { get; init; }

    /// <summary>
    ///     Set a Sorter to affect the order of parameter keys.
    /// </summary>
    public Comparison<object?>? Sort { get; init; }

    /// <summary>
    ///     Deprecated: Use ListFormat instead.
    /// </summary>
    [Obsolete("Use ListFormat instead")]
    public bool? Indices { get; init; }

    /// <summary>
    ///     Encodes a value to a string.
    ///     Uses the provided encoder if available, otherwise uses Utils.Encode.
    /// </summary>
    /// <param name="value">The value to encode</param>
    /// <param name="encoding">The encoding to use</param>
    /// <param name="format">The format to use</param>
    /// <returns>The encoded string</returns>
    public string GetEncoder(object? value, Encoding? encoding = null, Format? format = null)
    {
        return Encoder?.Invoke(value, encoding ?? Charset, format ?? Format)
               ?? Utils.Encode(value, encoding ?? Charset, format ?? Format);
    }

    /// <summary>
    ///     Serializes a DateTime instance to a string.
    ///     Uses the provided DateSerializer function if available, otherwise uses DateTime ISO format.
    /// </summary>
    /// <param name="date">The date to serialize</param>
    /// <returns>The serialized date string</returns>
    public string GetDateSerializer(DateTime date)
    {
        return DateSerializer?.Invoke(date) ?? date.ToString("O");
    }

    /// <summary>
    ///     Validates option invariants before encoding.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <see cref="Charset" /> is not a supported encoding.</exception>
    internal void Validate()
    {
        if (CharsetHelper.IsSupportedCharset(Charset))
            return;

        throw new ArgumentException("Invalid charset");
    }

    /// <summary>
    ///     Creates a new instance of EncodeOptions with the specified properties changed.
    /// </summary>
    /// <param name="addQueryPrefix">Set to override AddQueryPrefix</param>
    /// <param name="allowDots">Set to override AllowDots</param>
    /// <param name="allowEmptyLists">Set to override AllowEmptyLists</param>
    /// <param name="charset">Set to override Charset</param>
    /// <param name="charsetSentinel">Set to override CharsetSentinel</param>
    /// <param name="delimiter">Set to override Delimiter</param>
    /// <param name="encode">Set to override Encode</param>
    /// <param name="encodeDotInKeys">Set to override EncodeDotInKeys</param>
    /// <param name="encodeValuesOnly">Set to override EncodeValuesOnly</param>
    /// <param name="filter">Set to override Filter</param>
    /// <param name="format">Set to override Format</param>
    /// <param name="listFormat">Set to override ListFormat</param>
    /// <param name="skipNulls">Set to override SkipNulls</param>
    /// <param name="strictNullHandling">Set to override StrictNullHandling</param>
    /// <param name="commaRoundTrip">Set to override CommaRoundTrip</param>
    /// <param name="commaCompactNulls">Set to override CommaCompactNulls</param>
    /// <param name="sort">Set to override Sort</param>
    /// <param name="indices">Set to override Indices (deprecated)</param>
    /// <param name="encoder">Set to override the encoder function</param>
    /// <param name="dateSerializer">Set to override the date serializer function</param>
    /// <returns>A new EncodeOptions instance with the specified changes</returns>
    public EncodeOptions CopyWith(
        bool? addQueryPrefix = null,
        bool? allowDots = null,
        bool? allowEmptyLists = null,
        Encoding? charset = null,
        bool? charsetSentinel = null,
        string? delimiter = null,
        bool? encode = null,
        bool? encodeDotInKeys = null,
        bool? encodeValuesOnly = null,
        IFilter? filter = null,
        Format? format = null,
        ListFormat? listFormat = null,
        bool? skipNulls = null,
        bool? strictNullHandling = null,
        bool? commaRoundTrip = null,
        bool? commaCompactNulls = null,
        Comparison<object?>? sort = null,
        bool? indices = null,
        ValueEncoder? encoder = null,
        DateSerializer? dateSerializer = null
    )
    {
        return new EncodeOptions
        {
            AddQueryPrefix = addQueryPrefix ?? AddQueryPrefix,
            AllowDots = allowDots ?? AllowDots,
            AllowEmptyLists = allowEmptyLists ?? AllowEmptyLists,
            Charset = charset ?? Charset,
            CharsetSentinel = charsetSentinel ?? CharsetSentinel,
            Delimiter = delimiter ?? Delimiter,
            Encode = encode ?? Encode,
            EncodeDotInKeys = encodeDotInKeys ?? EncodeDotInKeys,
            EncodeValuesOnly = encodeValuesOnly ?? EncodeValuesOnly,
            Filter = filter ?? Filter,
            Format = format ?? Format,
            ListFormat = listFormat ?? ListFormat,
            SkipNulls = skipNulls ?? SkipNulls,
            StrictNullHandling = strictNullHandling ?? StrictNullHandling,
            CommaRoundTrip = commaRoundTrip ?? CommaRoundTrip,
            CommaCompactNulls = commaCompactNulls ?? CommaCompactNulls,
            Sort = sort ?? Sort,
#pragma warning disable CS0618 // Type or member is obsolete
            Indices = indices ?? Indices,
#pragma warning restore CS0618 // Type or member is obsolete
            Encoder = encoder ?? Encoder,
            DateSerializer = dateSerializer ?? DateSerializer
        };
    }
}
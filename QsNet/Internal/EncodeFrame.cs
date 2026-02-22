using System;
using System.Collections.Generic;
using System.Text;
using QsNet.Enums;
using QsNet.Models;

namespace QsNet.Internal;

internal enum EncodePhase
{
    Start,
    Iterate,
    AwaitChild
}

internal sealed class EncodeFrame(
    object? data,
    bool undefined,
    SideChannelFrame sideChannel,
    KeyPathNode path,
    ListFormatGenerator generator,
    bool commaRoundTrip,
    bool compactNulls,
    bool allowEmptyLists,
    bool strictNullHandling,
    bool skipNulls,
    bool encodeDotInKeys,
    ValueEncoder? encoder,
    DateSerializer? serializeDate,
    Comparison<object?>? sort,
    IFilter? filter,
    bool allowDots,
    Format format,
    Formatter formatter,
    bool encodeValuesOnly,
    Encoding charset,
    bool addQueryPrefix)
{
    public object? Data { get; set; } = data;
    public bool Undefined { get; } = undefined;
    public SideChannelFrame SideChannel { get; } = sideChannel;
    public KeyPathNode Path { get; } = path;
    public ListFormatGenerator Generator { get; } = generator;
    public bool CommaRoundTrip { get; } = commaRoundTrip;
    public bool CompactNulls { get; } = compactNulls;
    public bool AllowEmptyLists { get; } = allowEmptyLists;
    public bool StrictNullHandling { get; } = strictNullHandling;
    public bool SkipNulls { get; } = skipNulls;
    public bool EncodeDotInKeys { get; } = encodeDotInKeys;
    public ValueEncoder? Encoder { get; } = encoder;
    public DateSerializer? SerializeDate { get; } = serializeDate;
    public Comparison<object?>? Sort { get; } = sort;
    public IFilter? Filter { get; } = filter;
    public bool AllowDots { get; } = allowDots;
    public Format Format { get; } = format;
    public Formatter Formatter { get; } = formatter;
    public bool EncodeValuesOnly { get; } = encodeValuesOnly;
    public Encoding Charset { get; } = charset;
    public bool AddQueryPrefix { get; } = addQueryPrefix;

    public EncodePhase Phase { get; set; } = EncodePhase.Start;
    public object? Obj { get; set; }
    public List<object?>? SeqList { get; set; }
    public bool IsSeq { get; set; }
    public bool IsCommaGenerator { get; set; }
    public bool IsCycleTracked { get; set; }
    public object? CycleKey { get; set; }
    public KeyPathNode? AdjustedPath { get; set; }
    public List<object?> ObjKeys { get; set; } = [];
    public List<object?> Values { get; } = [];
    public int Index { get; set; }
}
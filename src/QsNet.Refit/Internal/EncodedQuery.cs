namespace QsNet.Refit.Internal;

internal readonly struct EncodedQuery(string value, QueryPair[] pairs)
{
    public string Value { get; } = value;

    public QueryPair[] Pairs { get; } = pairs;
}

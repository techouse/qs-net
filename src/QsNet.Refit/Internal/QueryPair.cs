namespace QsNet.Refit.Internal;

internal readonly struct QueryPair(string key, string? value)
{
    public string Key { get; } = key;

    public string? Value { get; } = value;
}

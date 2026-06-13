namespace QsNet.Refit.Internal;

internal sealed class QueryPairKey(string key, int index)
{
    public string Key { get; } = key;

    public int Index { get; } = index;

    public override string ToString() => Key;
}

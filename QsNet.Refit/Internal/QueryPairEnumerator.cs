using System;
using System.Collections;

namespace QsNet.Refit.Internal;

internal sealed class QueryPairEnumerator(QueryPair[] pairs) : IDictionaryEnumerator
{
    private int _index = -1;

    public DictionaryEntry Entry => CreateEntry();

    public object Key => Entry.Key;

    public object? Value => Entry.Value;

    public object Current => Entry;

    public bool MoveNext()
    {
        if (_index >= pairs.Length)
            return false;

        _index++;
        return _index < pairs.Length;
    }

    public void Reset()
    {
        _index = -1;
    }

    private DictionaryEntry CreateEntry()
    {
        if (_index < 0 || _index >= pairs.Length)
            throw new InvalidOperationException("Enumeration has either not started or has already finished.");

        return new DictionaryEntry(new QueryPairKey(pairs[_index].Key, _index), pairs[_index].Value);
    }
}

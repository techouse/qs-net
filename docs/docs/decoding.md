## Decoding

### Nested dictionaries

```csharp
Qs.Decode("foo[bar]=baz");
// => { "foo": { "bar": "baz" } }

Qs.Decode("a%5Bb%5D=c");
// => { "a": { "b": "c" } }

Qs.Decode("foo[bar][baz]=foobarbaz");
// => { "foo": { "bar": { "baz": "foobarbaz" } } }
```

### Depth (default: 5)

Beyond the configured depth, remaining bracket content is kept as literal text:

```csharp
Qs.Decode("a[b][c][d][e][f][g][h][i]=j");
// => { "a": { "b": { "c": { "d": { "e": { "f": { "[g][h][i]": "j" } } } } } } }
```

Override depth:

```csharp
Qs.Decode("a[b][c][d][e][f][g][h][i]=j", new DecodeOptions { Depth = 1 });
// => { "a": { "b": { "[c][d][e][f][g][h][i]": "j" } } }
```

### Parameter limit

```csharp
Qs.Decode("a=b&c=d", new DecodeOptions { ParameterLimit = 1 });
// => { "a": "b" }
```

### Compatibility notes

- Deep decode/merge handling is iterative (stack-safe) to avoid recursion overflows on very deep inputs.
- Empty query segments and empty keys are ignored before `ParameterLimit` accounting.
- Comma-list limit behavior is deterministic:
  - `ThrowOnLimitExceeded = true` throws on overflow.
  - `ThrowOnLimitExceeded = false` truncates to the remaining list capacity.
- Some JavaScript `qs` edge-case limitations are intentionally fixed rather than mirrored.

### Ignore leading `?`

```csharp
Qs.Decode("?a=b&c=d", new DecodeOptions { IgnoreQueryPrefix = true });
// => { "a": "b", "c": "d" }
```

### Custom delimiter (string or regex)

```csharp
Qs.Decode("a=b;c=d", new DecodeOptions { Delimiter = new StringDelimiter(";") });
// => { "a": "b", "c": "d" }

Qs.Decode("a=b;c=d", new DecodeOptions { Delimiter = new RegexDelimiter("[;,]") });
// => { "a": "b", "c": "d" }
```

### Dot-notation and "decode dots in keys"

```csharp
Qs.Decode("a.b=c", new DecodeOptions { AllowDots = true });
// => { "a": { "b": "c" } }

Qs.Decode(
    "name%252Eobj.first=John&name%252Eobj.last=Doe",
    new DecodeOptions { DecodeDotInKeys = true }
);
// => { "name.obj": { "first": "John", "last": "Doe" } }
```

### Empty lists

```csharp
Qs.Decode("foo[]&bar=baz", new DecodeOptions { AllowEmptyLists = true });
// => { "foo": [], "bar": "baz" }
```

### Duplicates

```csharp
Qs.Decode("foo=bar&foo=baz");
// => { "foo": ["bar", "baz"] }

Qs.Decode("foo=bar&foo=baz", new DecodeOptions { Duplicates = Duplicates.Combine });
// => same as above

Qs.Decode("foo=bar&foo=baz", new DecodeOptions { Duplicates = Duplicates.First });
// => { "foo": "bar" }

Qs.Decode("foo=bar&foo=baz", new DecodeOptions { Duplicates = Duplicates.Last });
// => { "foo": "baz" }
```

### Charset and sentinel

```csharp
// Latin1
Qs.Decode("a=%A7", new DecodeOptions { Charset = Encoding.Latin1 });
// => { "a": "§" }

// Sentinels
Qs.Decode("utf8=%E2%9C%93&a=%C3%B8", new DecodeOptions { Charset = Encoding.Latin1, CharsetSentinel = true });
// => { "a": "ø" }

Qs.Decode("utf8=%26%2310003%3B&a=%F8", new DecodeOptions { Charset = Encoding.UTF8, CharsetSentinel = true });
// => { "a": "ø" }
```

### Interpret numeric entities (`&#1234;`)

```csharp
Qs.Decode(
    "a=%26%239786%3B",
    new DecodeOptions { Charset = Encoding.Latin1, InterpretNumericEntities = true }
);
// => { "a": "☺" }
```

### Lists

```csharp
Qs.Decode("a[]=b&a[]=c");
// => { "a": ["b", "c"] }

Qs.Decode("a[1]=c&a[0]=b");
// => { "a": ["b", "c"] }

Qs.Decode("a[1]=b&a[15]=c");
// => { "a": ["b", "c"] }

Qs.Decode("a[]=&a[]=b");
// => { "a": ["", "b"] }
```

`ListLimit` uses two related guards:
- Explicit numeric indices (`a[21]`) are limited by maximum allowed index (`index <= ListLimit`).
- Implicit/comma/combined list growth (`a[]`, comma lists, duplicate combine paths) is limited by maximum list size before overflow conversion or exception.

Large indices convert to a dictionary by default:

```csharp
Qs.Decode("a[100]=b");
// => { "a": { 100: "b" } }
```

Disable list parsing:

```csharp
Qs.Decode("a[]=b", new DecodeOptions { ParseLists = false });
// => { "a": { 0: "b" } }
```

Mixing notations merges into a dictionary:

```csharp
Qs.Decode("a[0]=b&a[b]=c");
// => { "a": { 0: "b", "b": "c" } }
```

Comma-separated values:

```csharp
Qs.Decode("a=b,c", new DecodeOptions { Comma = true });
// => { "a": ["b", "c"] }
```

### Primitive/scalar values

All values decode as strings by default:

```csharp
Qs.Decode("a=15&b=true&c=null");
// => { "a": "15", "b": "true", "c": "null" }
```

---

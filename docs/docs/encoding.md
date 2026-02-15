## Encoding

### Basics

```csharp
Qs.Encode(new Dictionary<string, object?> { ["a"] = "b" });
// => "a=b"

Qs.Encode(new Dictionary<string, object?> 
{ 
    ["a"] = new Dictionary<string, object?> { ["b"] = "c" } 
});
// => "a%5Bb%5D=c"
```

Disable URI encoding for readability:

```csharp
Qs.Encode(
    new Dictionary<string, object?> 
    { 
        ["a"] = new Dictionary<string, object?> { ["b"] = "c" } 
    },
    new EncodeOptions { Encode = false }
);
// => "a[b]=c"
```

Values-only encoding:

```csharp
Qs.Encode(
    new Dictionary<string, object?>
    {
        ["a"] = "b",
        ["c"] = new List<object?> { "d", "e=f" },
        ["f"] = new List<object?>
        {
            new List<object?> { "g" },
            new List<object?> { "h" },
        },
    },
    new EncodeOptions { EncodeValuesOnly = true }
);
// => "a=b&c[0]=d&c[1]=e%3Df&f[0][0]=g&f[1][0]=h"
```

Custom encoder:

```csharp
Qs.Encode(
    new Dictionary<string, object?>
    {
        ["a"] = new Dictionary<string, object?> { ["b"] = "č" },
    },
    new EncodeOptions
    {
        Encoder = (str, _, _) => str?.ToString() == "č" ? "c" : str?.ToString() ?? "",
    }
);
// => "a[b]=c"
```

### List formats

```csharp
var data = new Dictionary<string, object?> { ["a"] = new List<object?> { "b", "c" } };
var options = new EncodeOptions { Encode = false };

// default (indices)
Qs.Encode(data, options.CopyWith(listFormat: ListFormat.Indices));
// => "a[0]=b&a[1]=c"

// brackets
Qs.Encode(data, options.CopyWith(listFormat: ListFormat.Brackets));
// => "a[]=b&a[]=c"

// repeat
Qs.Encode(data, options.CopyWith(listFormat: ListFormat.Repeat));
// => "a=b&a=c"

// comma
Qs.Encode(data, options.CopyWith(listFormat: ListFormat.Comma));
// => "a=b,c"
```

**Note:** When `ListFormat.Comma` is used, you can set `EncodeOptions.CommaRoundTrip` to `true` or `false` so single-item lists append `[]` and round-trip through decoding. Set `EncodeOptions.CommaCompactNulls` to `true` with the comma format to drop `null` entries instead of keeping empty slots (for example, `["one", null, "two"]` becomes `one,two`).

### Compatibility notes

- Deep/nested object encoding is iterative (stack-safe), so very deep graphs do not recurse the call stack.
- With `EncodeOptions.Encode = false`, `byte[]` values are converted to strings using the selected charset (`UTF-8`/`Latin1`) instead of using runtime type names.
- `FunctionFilter` output still flows through `DateSerializer` and comma-list temporal normalization.
- A few legacy JavaScript `qs` edge-case limitations are intentionally not mirrored when they conflict with safety or deterministic behavior.

### Nested dictionaries

```csharp
Qs.Encode(
    new Dictionary<string, object?>
    {
        ["a"] = new Dictionary<string, object?>
        {
            ["b"] = new Dictionary<string, object?> { ["c"] = "d", ["e"] = "f" },
        },
    },
    new EncodeOptions { Encode = false }
);
// => "a[b][c]=d&a[b][e]=f"
```

Dot notation:

```csharp
Qs.Encode(
    new Dictionary<string, object?>
    {
        ["a"] = new Dictionary<string, object?>
        {
            ["b"] = new Dictionary<string, object?> { ["c"] = "d", ["e"] = "f" },
        },
    },
    new EncodeOptions { Encode = false, AllowDots = true }
);
// => "a.b.c=d&a.b.e=f"
```

Encode dots in keys:

```csharp
Qs.Encode(
    new Dictionary<string, object?>
    {
        ["name.obj"] = new Dictionary<string, object?>
        {
            ["first"] = "John",
            ["last"] = "Doe",
        },
    },
    new EncodeOptions { AllowDots = true, EncodeDotInKeys = true }
);
// => "name%252Eobj.first=John&name%252Eobj.last=Doe"
```

Allow empty lists:

```csharp
Qs.Encode(
    new Dictionary<string, object?> { ["foo"] = new List<object?>(), ["bar"] = "baz" },
    new EncodeOptions { Encode = false, AllowEmptyLists = true }
);
// => "foo[]&bar=baz"
```

Empty strings and nulls:

```csharp
Qs.Encode(new Dictionary<string, object?> { ["a"] = "" });
// => "a="
```

Return empty string for empty containers:

```csharp
Qs.Encode(new Dictionary<string, object?> { ["a"] = new List<object?>() });        // => ""
Qs.Encode(new Dictionary<string, object?> { ["a"] = new Dictionary<string, object?>() });    // => ""
Qs.Encode(new Dictionary<string, object?> { ["a"] = new List<object?> { new Dictionary<string, object?>() } }); // => ""
Qs.Encode(new Dictionary<string, object?> { ["a"] = new Dictionary<string, object?> { ["b"] = new List<object?>() } }); // => ""
Qs.Encode(new Dictionary<string, object?> { ["a"] = new Dictionary<string, object?> { ["b"] = new Dictionary<string, object?>() } }); // => ""
```

Omit `Undefined`:

```csharp
Qs.Encode(new Dictionary<string, object?> { ["a"] = null, ["b"] = Undefined.Create() });
// => "a="
```

Add query prefix:

```csharp
Qs.Encode(
    new Dictionary<string, object?> { ["a"] = "b", ["c"] = "d" },
    new EncodeOptions { AddQueryPrefix = true }
);
// => "?a=b&c=d"
```

Custom delimiter:

```csharp
Qs.Encode(
    new Dictionary<string, object?> { ["a"] = "b", ["c"] = "d" },
    new EncodeOptions { Delimiter = ";" }
);
// => "a=b;c=d"
```

### Dates

By default, `DateTime` is serialized using `ToString()` in ISO 8601 format.

```csharp
var date = new DateTime(1970, 1, 1, 0, 0, 0, 7, DateTimeKind.Utc);

Qs.Encode(
    new Dictionary<string, object?> { ["a"] = date },
    new EncodeOptions { Encode = false }
);
// => "a=1970-01-01T00:00:00.0070000Z"

Qs.Encode(
    new Dictionary<string, object?> { ["a"] = date },
    new EncodeOptions
    {
        Encode = false,
        DateSerializer = d => ((DateTimeOffset)d).ToUnixTimeMilliseconds().ToString(),
    }
);
// => "a=7"
```

### Sorting & filtering

```csharp
// Sort keys
Qs.Encode(
    new Dictionary<string, object?>
    {
        ["a"] = "c",
        ["z"] = "y",
        ["b"] = "f",
    },
    new EncodeOptions
    {
        Encode = false,
        Sort = (a, b) => string.Compare(a?.ToString(), b?.ToString(), StringComparison.Ordinal),
    }
);
// => "a=c&b=f&z=y"

// Filter by function (drop/transform values)
var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
var testDate = epochStart.AddMilliseconds(123);

Qs.Encode(
    new Dictionary<string, object?>
    {
        ["a"] = "b",
        ["c"] = "d",
        ["e"] = new Dictionary<string, object?>
        {
            ["f"] = testDate,
            ["g"] = new List<object?> { 2 },
        },
    },
    new EncodeOptions
    {
        Encode = false,
        Filter = new FunctionFilter(
            (prefix, value) =>
                prefix switch
                {
                    "b" => Undefined.Create(),
                    "e[f]" => (long)((DateTime)value! - epochStart).TotalMilliseconds,
                    "e[g][0]" => Convert.ToInt32(value) * 2,
                    _ => value,
                }
        ),
    }
);
// => "a=b&c=d&e[f]=123&e[g][0]=4"

// Filter by explicit list of keys/indices
Qs.Encode(
    new Dictionary<string, object?>
    {
        ["a"] = "b",
        ["c"] = "d",
        ["e"] = "f",
    },
    new EncodeOptions
    {
        Encode = false,
        Filter = new IterableFilter(new List<object> { "a", "e" }),
    }
);
// => "a=b&e=f"

Qs.Encode(
    new Dictionary<string, object?>
    {
        ["a"] = new List<object?> { "b", "c", "d" },
        ["e"] = "f",
    },
    new EncodeOptions
    {
        Encode = false,
        Filter = new IterableFilter(new List<object> { "a", 0, 2 }),
    }
);
// => "a[0]=b&a[2]=d"
```

### Null handling

```csharp
// Treat null values like empty strings by default
Qs.Encode(new Dictionary<string, object?> { ["a"] = null, ["b"] = "" });
// => "a=&b="

// Cannot distinguish between parameters with and without equal signs
Qs.Decode("a&b=");
// => { "a": "", "b": "" }

// Distinguish between null values and empty strings using strict null handling
Qs.Encode(
    new Dictionary<string, object?> { ["a"] = null, ["b"] = "" },
    new EncodeOptions { StrictNullHandling = true }
);
// => "a&b="

// Decode values without equals back to null using strict null handling
Qs.Decode("a&b=", new DecodeOptions { StrictNullHandling = true });
// => { "a": null, "b": "" }

// Completely skip rendering keys with null values using skip nulls
Qs.Encode(
    new Dictionary<string, object?> { ["a"] = "b", ["c"] = null },
    new EncodeOptions { SkipNulls = true }
);
// => "a=b"
```

### Charset handling

```csharp
// Encode using Latin1 charset
Qs.Encode(
    new Dictionary<string, object?> { ["æ"] = "æ" },
    new EncodeOptions { Charset = Encoding.Latin1 }
);
// => "%E6=%E6"

// Convert characters that don't exist in Latin1 to numeric entities
Qs.Encode(
    new Dictionary<string, object?> { ["a"] = "☺" },
    new EncodeOptions { Charset = Encoding.Latin1 }
);
// => "a=%26%239786%3B"

// Announce charset using charset sentinel option with UTF-8
Qs.Encode(
    new Dictionary<string, object?> { ["a"] = "☺" },
    new EncodeOptions { CharsetSentinel = true }
);
// => "utf8=%E2%9C%93&a=%E2%98%BA"

// Announce charset using charset sentinel option with Latin1
Qs.Encode(
    new Dictionary<string, object?> { ["a"] = "æ" },
    new EncodeOptions { Charset = Encoding.Latin1, CharsetSentinel = true }
);
// => "utf8=%26%2310003%3B&a=%E6"
```

### RFC 3986 vs RFC 1738 space encoding

```csharp
Qs.Encode(new Dictionary<string, object?> { ["a"] = "b c" });
// => "a=b%20c"   (RFC 3986 default)

Qs.Encode(new Dictionary<string, object?> { ["a"] = "b c" }, new EncodeOptions { Format = Format.Rfc3986 });
// => "a=b%20c"

Qs.Encode(new Dictionary<string, object?> { ["a"] = "b c" }, new EncodeOptions { Format = Format.Rfc1738 });
// => "a=b+c"
```

---

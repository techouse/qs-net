# QsNet

A query string encoding and decoding library for C#/.NET.

Ported from [qs](https://www.npmjs.com/package/qs) for JavaScript.

[![NuGet Version](https://img.shields.io/nuget/v/QsNet)](https://www.nuget.org/packages/QsNet)
[![NuGet Downloads](https://img.shields.io/nuget/dt/QsNet)](https://www.nuget.org/packages/QsNet)
[![Test](https://github.com/techouse/qs-net/actions/workflows/test.yml/badge.svg)](https://github.com/techouse/qs-net/actions/workflows/test.yml)
[![codecov](https://codecov.io/gh/techouse/qs-net/graph/badge.svg?token=ClCDNcsxqQ)](https://codecov.io/gh/techouse/qs-net)
[![GitHub](https://img.shields.io/github/license/techouse/qs-net)](LICENSE)
[![GitHub Repo stars](https://img.shields.io/github/stars/techouse/qs-net)](https://github.com/techouse/qs-net/stargazers)

---

## Highlights

- Nested dictionaries and lists: `foo[bar][baz]=qux` ⇄ `{ "foo": { "bar": { "baz": "qux" } } }`
- Multiple list formats (indices, brackets, repeat, comma)
- Dot-notation support (`a.b=c`) and `"."`-encoding toggles
- UTF-8 and Latin1 charsets, plus optional charset sentinel (`utf8=✓`)
- Custom encoders/decoders, key sorting, filtering, and strict null handling
- Supports `DateTime` serialization via a pluggable serializer
- Extensive tests (xUnit + FluentAssertions), performance-minded implementation

---

## Installation

### NuGet Package Manager

```
Install-Package QsNet
```

### .NET CLI

```bash
dotnet add package QsNet
```

### Package Reference

```xml
<PackageReference Include="QsNet" Version="<version>" />
```

---

## Requirements

- .NET **8.0+**

---

## Quick start

```csharp
using QsNet;

// Decode
Dictionary<string, object?> obj = Qs.Decode("foo[bar]=baz&foo[list][]=a&foo[list][]=b");
// -> { "foo": { "bar": "baz", "list": ["a", "b"] } }

// Encode
string qs = Qs.Encode(new Dictionary<string, object?> 
{ 
    ["foo"] = new Dictionary<string, object?> { ["bar"] = "baz" } 
});
// -> "foo%5Bbar%5D=baz"
```

---

## Usage

### Simple

```csharp
// Decode
Dictionary<string, object?> decoded = Qs.Decode("a=c");
// => { "a": "c" }

// Encode
string encoded = Qs.Encode(new Dictionary<string, object?> { ["a"] = "c" });
// => "a=c"
```

---

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

## Design notes

- **Performance:** The implementation mirrors qs semantics but is optimized for C#/.NET. Deep parsing, list compaction,
  and cycle-safe compaction are implemented iteratively where it matters.
- **Safety:** Defaults (depth, parameterLimit) help mitigate abuse in user-supplied inputs; you can loosen them when you
  fully trust the source.
- **Interop:** Exposes knobs similar to qs (filters, sorters, custom encoders/decoders) to make migrations
  straightforward.

---

Special thanks to the authors of [qs](https://www.npmjs.com/package/qs) for JavaScript:

- [Jordan Harband](https://github.com/ljharb)
- [TJ Holowaychuk](https://github.com/visionmedia/node-querystring)

---

## License

BSD 3-Clause © [techouse](https://github.com/techouse)

---
_layout: landing
---

# QsNet

A query string encoding and decoding library for C#/.NET.

Ported from [qs](https://www.npmjs.com/package/qs) for JavaScript.

[![NuGet Version](https://img.shields.io/nuget/v/QsNet)](https://www.nuget.org/packages/QsNet)
[![Test](https://github.com/techouse/qs-net/actions/workflows/test.yml/badge.svg)](https://github.com/techouse/QsNet/actions/workflows/test.yml)
[![codecov](https://codecov.io/gh/techouse/qs-net/graph/badge.svg?token=ClCDNcsxqQ)](https://codecov.io/gh/techouse/qs-net)
[![GitHub](https://img.shields.io/github/license/techouse/qs-net)](https://github.com/techouse/qs-net/blob/main/LICENSE)
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

## Usage

```csharp
using QsNet;

// Decode
var obj = Qs.Decode("foo[bar]=baz&foo[list][]=a&foo[list][]=b");
// -> { "foo": { "bar": "baz", "list": ["a", "b"] } }

// Encode
var qs = Qs.Encode(new Dictionary<string, object?> 
{ 
    ["foo"] = new Dictionary<string, object?> { ["bar"] = "baz" } 
});
// -> "foo%5Bbar%5D=baz"
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
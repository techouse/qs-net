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

## Design notes

- **Performance:** The implementation mirrors qs semantics but is optimized for C#/.NET. Deep parsing, list compaction,
  and cycle-safe compaction are implemented iteratively where it matters.
- **Safety:** Defaults (depth, parameterLimit) help mitigate abuse in user-supplied inputs; you can loosen them when you
  fully trust the source.
- **Interop:** Exposes knobs similar to qs (filters, sorters, custom encoders/decoders) to make migrations
  straightforward.

---

## Other ports


| Port                       | Repository                                                  | Package                                                                                                                                                                                       |
|----------------------------|-------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Dart                       | [techouse/qs](https://github.com/techouse/qs)               | [![pub.dev](https://img.shields.io/pub/v/qs_dart?logo=dart&label=pub.dev)](https://pub.dev/packages/qs_dart)                                                                                  |
| Python                     | [techouse/qs_codec](https://github.com/techouse/qs_codec)   | [![PyPI](https://img.shields.io/pypi/v/qs-codec?logo=python&label=PyPI)](https://pypi.org/project/qs-codec/)                                                                                  |
| Kotlin / JVM + Android AAR | [techouse/qs-kotlin](https://github.com/techouse/qs-kotlin) | [![Maven Central](https://img.shields.io/maven-central/v/io.github.techouse/qs-kotlin?logo=kotlin&label=Maven%20Central)](https://central.sonatype.com/artifact/io.github.techouse/qs-kotlin) |
| Swift / Objective-C        | [techouse/qs-swift](https://github.com/techouse/qs-swift)   | [![SPM](https://img.shields.io/github/v/release/techouse/qs-swift?logo=swift&label=SwiftPM)](https://swiftpackageindex.com/techouse/qs-swift)                                                 |
| Node.js (original)         | [ljharb/qs](https://github.com/ljharb/qs)                   | [![npm](https://img.shields.io/npm/v/qs?logo=javascript&label=npm)](https://www.npmjs.com/package/qs)                                                                                         |

---

Special thanks to the authors of [qs](https://www.npmjs.com/package/qs) for JavaScript:

- [Jordan Harband](https://github.com/ljharb)
- [TJ Holowaychuk](https://github.com/visionmedia/node-querystring)

---

## License

BSD 3-Clause © [techouse](https://github.com/techouse)
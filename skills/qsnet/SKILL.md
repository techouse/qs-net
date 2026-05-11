---
name: qsnet
description: Use this skill whenever a user wants to install, configure, troubleshoot, or write C#/.NET, ASP.NET Core, test, or library code for encoding and decoding nested query strings with the QsNet NuGet package. This skill helps produce practical Qs.Decode, Qs.Encode, ToQueryMap, and ToQueryString snippets, choose DecodeOptions and EncodeOptions, explain option tradeoffs, and avoid QsNet edge-case pitfalls around lists, dot notation, duplicates, null handling, charset sentinels, depth limits, ASP.NET query collections, and untrusted input.
---

# QsNet Usage Assistant

Help users parse and build query strings with the C#/.NET `QsNet` package.
Focus on user application code and interoperability outcomes, not repository
maintenance.

## Start With Inputs

Before producing a final snippet, collect only the missing details that change
the code:

- Runtime: C# application, ASP.NET Core request handling, tests, library code,
  .NET Framework/netstandard consumer, or generated example.
- Direction: decode an incoming query string, encode .NET data, or normalize
  query-string handling around an existing URL/request object.
- The actual query string or data structure when available.
- Target API convention for lists: indexed brackets, empty brackets, repeated
  keys, or comma-separated values.
- Whether the query may include a leading `?`, dot notation, literal dots in
  keys, duplicate keys, custom delimiters, comma-separated lists, `null` flags,
  ISO-8859-1/legacy charset behavior, ASP.NET query collection behavior, or
  untrusted user input.

Do not over-ask when the desired behavior is obvious. State assumptions in the
answer and give the user a concrete snippet they can paste.

## Installation

Use the NuGet package for normal .NET projects:

```bash
dotnet add package QsNet
```

Package Manager:

```powershell
Install-Package QsNet
```

Package reference:

```xml
<PackageReference Include="QsNet" Version="<version>" />
```

The package targets `net10.0` and `netstandard2.0`, so it can be consumed by
modern .NET and compatible .NET Standard projects. For older TFMs that need
Latin1/code-page encodings, remind users to register the code pages provider
before using `Encoding.GetEncoding("iso-8859-1")`.

## Public API

Prefer the `Qs` facade for application code:

```csharp
using QsNet;
using QsNet.Enums;
using QsNet.Models;
```

Core methods:

```csharp
Dictionary<string, object?> values = Qs.Decode("a[b]=c");
string query = Qs.Encode(new Dictionary<string, object?> { ["a"] = "c" });
```

Extension helpers are available when the shape is already a query string or a
`Dictionary<string, object?>`:

```csharp
Dictionary<string, object?> values = "a[b]=c".ToQueryMap();
string query = values.ToQueryString();
```

`Qs.Decode` accepts a raw string, an `IDictionary`, or an
`IEnumerable<KeyValuePair<string?, object?>>`. Prefer the raw query string when
nested syntax, duplicate keys, or exact delimiter behavior matters, because many
web framework query abstractions have already normalized or grouped the original
wire format.

## Base Patterns

Decode a nested query string:

```csharp
using QsNet;

var values = Qs.Decode("filter[status]=open&tag[]=dotnet&tag[]=qs");

// values["filter"] is a nested dictionary; values["tag"] is a list.
```

Encode nested .NET values:

```csharp
using QsNet;

var query = Qs.Encode(
    new Dictionary<string, object?>
    {
        ["filter"] = new Dictionary<string, object?>
        {
            ["status"] = "open",
        },
        ["tag"] = new List<object?> { "dotnet", "qs" },
    }
);

// filter%5Bstatus%5D=open&tag%5B0%5D=dotnet&tag%5B1%5D=qs
```

For readable examples, tests, or APIs that expect unescaped bracket syntax, set
`Encode = false` intentionally:

```csharp
using QsNet;
using QsNet.Models;

var query = Qs.Encode(
    new Dictionary<string, object?>
    {
        ["a"] = new Dictionary<string, object?> { ["b"] = "c" },
    },
    new EncodeOptions { Encode = false }
);

// a[b]=c
```

## ASP.NET Core

For request parsing, prefer the raw query string when qs-style nesting and
duplicates matter:

```csharp
using QsNet;
using QsNet.Models;

var values = Qs.Decode(
    httpContext.Request.QueryString.Value,
    new DecodeOptions { IgnoreQueryPrefix = true }
);
```

Use `Request.Query` only when the app is already comfortable with ASP.NET
Core's query normalization and grouping semantics. If a user starts from
`IQueryCollection`, mention that duplicate ordering and exact raw delimiters may
already be lost.

When encoding into a URL, use `AddQueryPrefix = true` only when the caller wants
the leading question mark:

```csharp
using QsNet;
using QsNet.Enums;
using QsNet.Models;

var query = Qs.Encode(
    new Dictionary<string, object?>
    {
        ["page"] = 2,
        ["tag"] = new List<object?> { "api", "docs" },
    },
    new EncodeOptions { AddQueryPrefix = true, ListFormat = ListFormat.Repeat }
);

// ?page=2&tag=api&tag=docs
```

## Decode Recipes

Use these options with `Qs.Decode(query, new DecodeOptions { ... })`:

- Leading question mark: `IgnoreQueryPrefix = true`.
- Dot notation such as `a.b=c`: `AllowDots = true`.
- Double-encoded literal dots in keys such as `name%252Eobj.first=John`:
  `DecodeDotInKeys = true`; this implies `AllowDots = true` unless explicitly
  contradicted.
- Duplicate keys: `Duplicates = Duplicates.Combine` keeps all values as a list;
  use `Duplicates.First` or `Duplicates.Last` to collapse.
- Bracket lists: enabled by default; set `ParseLists = false` to treat list
  syntax as dictionary keys.
- Empty list tokens such as `foo[]`: `AllowEmptyLists = true`.
- Sparse numeric indices: `AllowSparseLists = true` preserves holes as `null`
  entries; the default compacts lists.
- Large or sparse list indices: default `ListLimit` is `20`; indices above the
  limit become dictionary keys or are limited according to the merge path.
- Comma-separated values such as `a=b,c`: `Comma = true`.
- Tokens without `=` as `null`: `StrictNullHandling = true`.
- Custom delimiters: `Delimiter = new StringDelimiter(";")` or
  `Delimiter = new RegexDelimiter("[;,]")`.
- Legacy charset input: `Charset = Encoding.Latin1` on modern TFMs or
  `Encoding.GetEncoding("iso-8859-1")` where `Encoding.Latin1` is unavailable;
  use `CharsetSentinel = true` when a form may include `utf8=...` to signal the
  real charset.
- HTML numeric entities: `InterpretNumericEntities = true`, usually with
  ISO-8859-1 or charset sentinel handling.
- Custom scalar decoding: use `DecoderWithKind` when key/value behavior differs;
  key decoding must return `string` or `null`.
- Untrusted input: keep `Depth`, `ParameterLimit`, and `ListLimit` bounded; use
  `StrictDepth = true` and `ThrowOnLimitExceeded = true` when callers need hard
  failures instead of soft limiting.

Example for a request query:

```csharp
using QsNet;
using QsNet.Enums;
using QsNet.Models;

var values = Qs.Decode(
    "?filter.status=open&tag=dotnet&tag=qs",
    new DecodeOptions
    {
        IgnoreQueryPrefix = true,
        AllowDots = true,
        Duplicates = Duplicates.Combine,
    }
);
```

## Encode Recipes

Use these options with `Qs.Encode(data, new EncodeOptions { ... })`:

- List style defaults to `ListFormat.Indices`:
  `tag%5B0%5D=dotnet&tag%5B1%5D=qs`.
- Empty brackets: `ListFormat = ListFormat.Brackets`.
- Repeated keys: `ListFormat = ListFormat.Repeat`.
- Comma-separated values: `ListFormat = ListFormat.Comma`.
- Single-item comma lists that must round-trip as lists:
  `CommaRoundTrip = true`.
- Drop `null` items before comma-joining lists: `CommaCompactNulls = true`.
- Dot notation for nested dictionaries: `AllowDots = true`.
- Literal dots in keys: `EncodeDotInKeys = true`; `AllowDots` is implied when it
  is not explicitly set.
- Add a leading `?`: `AddQueryPrefix = true`.
- Custom pair delimiter: `Delimiter = ";"`.
- Preserve readable bracket/dot keys while encoding values:
  `EncodeValuesOnly = true`.
- Disable percent encoding entirely for debugging or documented examples:
  `Encode = false`.
- Emit `null` without `=`: `StrictNullHandling = true`.
- Omit `null` keys: `SkipNulls = true`.
- Emit empty lists as `foo[]`: `AllowEmptyLists = true`.
- Legacy form spaces as `+`: `Format = Format.Rfc1738`; the default is
  `Format.Rfc3986`, which emits spaces as `%20`.
- Legacy charset output: `Charset = Encoding.Latin1` on modern TFMs or
  `Encoding.GetEncoding("iso-8859-1")` where needed; use
  `CharsetSentinel = true` to prepend the `utf8=...` sentinel.
- Custom behavior: use `Encoder`, `DateSerializer`, `Sort`, or `Filter` when
  the target API needs special scalar encoding, date formatting, stable key
  order, or selected fields.

Example for an API that expects repeated keys:

```csharp
using QsNet;
using QsNet.Enums;
using QsNet.Models;

var query = Qs.Encode(
    new Dictionary<string, object?>
    {
        ["q"] = "query strings",
        ["tag"] = new List<object?> { "dotnet", "qs" },
    },
    new EncodeOptions
    {
        ListFormat = ListFormat.Repeat,
        AddQueryPrefix = true,
    }
);

// ?q=query%20strings&tag=dotnet&tag=qs
```

## Type And Shape Notes

Decoded values are strings by default. QsNet does not coerce `"15"`, `"true"`,
or `"null"` into numbers, booleans, or nulls unless the user supplies a custom
decoder or post-processes the result.

Use `Dictionary<string, object?>` for object-like values and `List<object?>` for
list-like values in examples. Avoid anonymous objects when callers need exact
dictionary/list query semantics.

Root scalar values, `null`, empty dictionaries, and empty containers generally
encode to an empty string. Empty lists only render when `AllowEmptyLists = true`
and the empty list is reachable from a dictionary key.

`Undefined` is an internal implementation detail in QsNet. Do not recommend
`Undefined.Create()` to application users. To omit values, prefer
`SkipNulls = true`, remove entries before encoding, or use public filter
behavior only when the desired output can be represented without internal
sentinels.

## Combinations To Check

Warn or adjust before giving code for these cases:

- `DecodeOptions { DecodeDotInKeys = true, AllowDots = false }` is invalid.
- `ParameterLimit` must be positive.
- `ThrowOnLimitExceeded = true` turns parameter and list limit violations into
  exceptions; without it, parsing truncates or falls back where possible.
- `StrictDepth = true` throws on well-formed depth overflow; with the default
  `false`, the remainder beyond `Depth` is kept as a trailing key segment.
- Built-in charset handling supports UTF-8 and ISO-8859-1/Latin1; other
  encodings require a custom `Encoder` or `Decoder`.
- `EncodeOptions.Encoder` is ignored when `Encode = false`.
- Combining `EncodeValuesOnly = true` and `EncodeDotInKeys = true` encodes only
  dots in keys; values remain otherwise unchanged.
- `DecodeOptions.Comma` parses simple comma-separated values, but does not
  decode nested dictionary syntax such as `a={b:1},{c:d}`.
- `ListFormat.Comma` changes how lists are represented and can be ambiguous for
  scalar values that themselves contain commas.
- Web framework query abstractions may flatten, group, sort, or pre-decode
  values. Prefer `Qs.Decode` on the raw query string when qs-style nested or
  repeated values matter.

## Response Shape

For code-generation requests, answer with:

1. A short statement of assumptions, especially runtime, list format, null
   handling, charset, prefix handling, ASP.NET/raw-query handling, and whether
   input is trusted.
2. One concrete C# snippet using `Qs.Decode`, `Qs.Encode`, `ToQueryMap`, or
   `ToQueryString`.
3. A brief explanation of only the options used.
4. A small verification example, such as an expected dictionary shape, expected
   query string, xUnit assertion, FluentAssertions assertion, or `Debug.Assert`.

Keep snippets application-oriented. Prefer public API imports from `QsNet`,
`QsNet.Models`, and `QsNet.Enums`; do not ask users to import from
`QsNet.Internal`.

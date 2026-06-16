---
name: qsnet
description: Use this skill whenever a user wants to install, configure, troubleshoot, or write C#/.NET code with QsNet or its adapter packages (QsNet.AspNetCore, QsNet.Flurl, QsNet.Refit, QsNet.RestSharp) for encoding and decoding nested query strings. This skill helps produce practical Qs.Decode, Qs.Encode, ToQueryMap, ToQueryString, AddQueryString, AppendQsQueryParams, SetQsQueryParams, QsQuery, and AddQsQueryParameters snippets, choose DecodeOptions and EncodeOptions, explain option tradeoffs, and avoid QsNet edge-case pitfalls around lists, dot notation, duplicates, null handling, charset sentinels, depth limits, web framework query normalization, double encoding, and untrusted input.
---

# QsNet Usage Assistant

Help users parse and build query strings with the C#/.NET `QsNet` package.
Focus on user application code and interoperability outcomes, not repository
maintenance.

## Start With Inputs

Before producing a final snippet, collect only the missing details that change
the code:

- Runtime: C# application, ASP.NET Core request handling, Flurl URL building,
  Refit interface calls, RestSharp requests, tests, library code, .NET
  Framework/netstandard consumer, or generated example.
- Direction: decode an incoming query string, encode .NET data, or normalize
  query-string handling around an existing URL/request object.
- Package choice: core `QsNet` only, or an adapter package such as
  `QsNet.AspNetCore`, `QsNet.Flurl`, `QsNet.Refit`, or `QsNet.RestSharp`.
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

Use the core NuGet package for normal .NET projects:

```bash
dotnet add package QsNet
```

Install an adapter package only when the app is using that integration surface:

```bash
dotnet add package QsNet.AspNetCore
dotnet add package QsNet.Flurl
dotnet add package QsNet.Refit
dotnet add package QsNet.RestSharp
```

Use `QsNet.AspNetCore` for ASP.NET Core `HttpRequest`, `QueryString`, `string`,
and `Uri` helpers. Use `QsNet.Flurl` for Flurl `Url` builders and Flurl.Http
chains. Use `QsNet.Refit` for `QsQuery` wrappers passed through Refit
interfaces. Use `QsNet.RestSharp` for adding QsNet-generated query parameters
to `RestRequest`.

`QsNet.Refit` intentionally has no runtime dependency on Refit. Install `Refit`
separately in the project that declares or consumes the Refit API interface.

Package Manager:

```powershell
Install-Package QsNet
```

Package reference:

```xml
<PackageReference Include="QsNet" Version="<version>" />
```

The core package targets `net10.0` and `netstandard2.0`, so it can be consumed
by modern .NET and compatible .NET Standard projects. `QsNet.AspNetCore`
targets `net10.0`; `QsNet.Flurl`, `QsNet.Refit`, and `QsNet.RestSharp` also
target `netstandard2.0`. For older TFMs that need Latin1/code-page encodings,
remind users to register the code pages provider before using
`Encoding.GetEncoding("iso-8859-1")`.

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

Adapter entry points: `QsNet.AspNetCore` adds `AddQueryString` and
`ToQueryMap`; `QsNet.Flurl` adds `AppendQsQueryParams` and
`SetQsQueryParams`; `QsNet.Refit` provides `QsQuery`/`QsQuery<T>` wrappers;
`QsNet.RestSharp` adds `AddQsQueryParameters`.

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

## Adapter Packages

Adapter packages keep the core `QsNet` package framework-agnostic. They encode
with QsNet first, then attach the already encoded query to the target URL or
request surface. This is intentional: do not pass complete qs-style query
strings back through framework helpers that would double-encode `%5B` and `%5D`.

Most adapter encoding methods accept dictionaries, non-generic dictionaries,
`IEnumerable<KeyValuePair<string, object?>>`, enumerables, arrays, and anonymous
or DTO objects with public readable instance properties. They normalize object
graphs before calling `Qs.Encode`; cyclic object graphs throw
`InvalidOperationException`.

Adapter methods ignore `EncodeOptions.AddQueryPrefix` by forcing no leading `?`;
the URL/request integration owns the separator.

### ASP.NET Core

Prefer `QsNet.AspNetCore` when an app already works with ASP.NET Core request
or URL types:

```csharp
using QsNet.AspNetCore;

var url = "/api/search#results".AddQueryString(new
{
    filter = new { name = "Alice" },
    tags = new[] { "one", "two" },
});
```

`AddQueryString` works on `string` and `Uri`, preserves fragments, chooses `?`
or `&` based on the existing URL, and appends QsNet output directly instead of
using ASP.NET Core `QueryHelpers.AddQueryString`.

For request parsing, use `HttpRequest.ToQueryMap()` or
`QueryString.ToQueryMap()`:

```csharp
using QsNet.AspNetCore;
using QsNet.Models;

var values = httpContext.Request.ToQueryMap(
    new DecodeOptions { AllowDots = true }
);
```

These helpers decode the raw `Request.QueryString`/`QueryString` value and
strip a leading `?` before calling `Qs.Decode`. Use `Request.Query` only when
the app is already comfortable with ASP.NET Core's query normalization and
grouping semantics. If a user starts from `IQueryCollection`, mention that
duplicate ordering and exact raw delimiters may already be lost.

Without the adapter package, fall back to `Qs.Decode` on
`httpContext.Request.QueryString.Value` with `IgnoreQueryPrefix = true`.

### Flurl

Use `QsNet.Flurl` when building URLs with Flurl:

```csharp
using Flurl;
using QsNet.Flurl;

var url = "https://api.example.com"
    .AppendPathSegment("products")
    .AppendQsQueryParams(new { filter = new { name = "John" } });
```

Use `AppendQsQueryParams` to keep existing query parameters and append QsNet
pairs. Use `SetQsQueryParams` to replace the entire query string; when encoding
returns an empty string, `SetQsQueryParams` clears the query. The `Url` overloads
mutate and return the same `Url` instance. String and `Uri` overloads create a
Flurl `Url`.

For Flurl.Http chains, append QsNet query parameters before sending, then call
the usual Flurl.Http method such as `GetJsonAsync<T>()`.

### Refit

Use `QsNet.Refit` when a Refit interface needs to send a QsNet-generated query
through Refit's `[Query]` pipeline:

```csharp
using QsNet.Refit;
using Refit;

public interface IProductsApi
{
    [Get("/products")]
    [QueryUriFormat(UriFormat.Unescaped)]
    Task<ProductSearchResponse> Search([Query] QsQuery query);
}

await api.Search(QsQuery.From(new
{
    filter = new { where = new { name = "John" } },
    tags = new[] { "a", "b" },
}));
```

Always include `[QueryUriFormat(UriFormat.Unescaped)]` on methods that accept
`QsQuery` or `QsQuery<T>` so Refit does not double-encode QsNet's already
encoded keys. Use `QsQuery<T>.From(source, options)` when preserving the source
DTO type helps the call site.

For ASP.NET Core-style indexed list keys, use `AllowDots = true`; this produces
shapes such as `Roles%5B0%5D.Name=Developer`.

Do not suggest plain `[Query] UserQuery query` when qs-style nested query
strings are required; `QsNet.Refit` does not replace Refit's native object
serializer. It wraps a pre-serialized QsNet query as read-only key/value pairs.

Refit's query pipeline joins normal `key=value` pairs with `&`, so `QsQuery`
rejects custom `EncodeOptions.Delimiter` values and key-only pairs from
`StrictNullHandling = true`. Repeated keys and bracket list keys are supported.

### RestSharp

Use `QsNet.RestSharp` when adding nested query parameters to a RestSharp
`RestRequest`:

```csharp
using QsNet.RestSharp;
using RestSharp;

var request = new RestRequest("products")
    .AddQsQueryParameters(new { filter = new { name = "John" } });
```

`AddQsQueryParameters` mutates and returns the same request, preserves existing
resource query strings and query parameters, adds each QsNet pair with RestSharp
query encoding disabled, and avoids `query=` wrapper parameters. Prefer it over
`AddObject`/`AddObjectStatic` when the server expects qs-style nested object
graphs or lists of complex objects.

Repeated keys and key-only pairs from `StrictNullHandling = true` are supported.
Custom `EncodeOptions.Delimiter` values are rejected when encoding produces
query output because RestSharp joins query parameters with `&`.

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
   handling, charset, prefix handling, adapter package choice, framework
   query normalization, and whether input is trusted.
2. One concrete C# snippet using the right public entry point for the runtime:
   `Qs.Decode`, `Qs.Encode`, `ToQueryMap`, `ToQueryString`,
   `AddQueryString`, `AppendQsQueryParams`, `SetQsQueryParams`, `QsQuery`,
   `QsQuery<T>`, or `AddQsQueryParameters`.
3. A brief explanation of only the options used.
4. A small verification example, such as an expected dictionary shape, expected
   query string, xUnit assertion, FluentAssertions assertion, or `Debug.Assert`.

Keep snippets application-oriented. Prefer public API imports from `QsNet`,
`QsNet.Models`, `QsNet.Enums`, or the relevant adapter namespace
(`QsNet.AspNetCore`, `QsNet.Flurl`, `QsNet.Refit`, or `QsNet.RestSharp`); do
not ask users to import from `QsNet.Internal`.

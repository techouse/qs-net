# QsNet.RestSharp Integration Package Spec

## Repo Findings

- Core `QsNet` lives in `QsNet/`, targets `net10.0;netstandard2.0`, and exposes `Qs.Encode(object? data, EncodeOptions? options = null)`.
- Optional integrations are root-level projects: `QsNet.AspNetCore`, `QsNet.Flurl`, and `QsNet.Refit`.
- `QsNet.Flurl` and `QsNet.Refit` target `net10.0;netstandard2.0`, use `$(QsNetPackageVersion)`, include SourceLink/package metadata, and keep tests in root-level `*.Tests` projects targeting `net10.0`.
- Docs are README plus DocFX pages under `docs/docs/`, with API metadata in `docs/docfx.json`.
- CI builds, tests, collects coverage, packs every package, and tag publishing pushes/uploads/releases all package artifacts.

## RestSharp Findings

- The local RestSharp source at `/Users/klemen/Work/RestSharp` shows `RestRequest.AddQueryParameter(name, value, encode: false)` creates `QueryParameter` values with `Encode = false`.
- `RestClient.BuildUriString(request)` joins query parameters with `&` and emits unencoded query parameter names/values when `Encode = false`.
- RestSharp has tests proving `encode: false` preserves commas, pipes, and already encoded resource query strings.
- `RestRequest(resourceWithQuery)` parses existing query strings into `encode: false` query parameters, preserving existing query text and appending later query parameters.
- `AddObject` / `AddObjectStatic` are property-flattening helpers for simple primitive values and primitive collections, not qs-style nested object graphs.

## Design

Add `QsNet.RestSharp` as an optional adapter package. It depends on `QsNet` and `RestSharp` `114.0.0`; core `QsNet` remains RestSharp-free.

Public API namespace: `QsNet.RestSharp`.

```csharp
public static class QsNetRestRequestExtensions
{
    public static RestRequest AddQsQueryParameters(
        this RestRequest request,
        object? values,
        EncodeOptions? options = null);
}
```

Behavior:

- Throw `ArgumentNullException` for a null `request`.
- Return the same `RestRequest` unchanged when `values is null`.
- Normalize dictionaries, anonymous objects, DTOs, nested objects, arrays, and lists before calling `Qs.Encode`.
- Force `EncodeOptions.AddQueryPrefix = false`.
- Return unchanged when QsNet produces an empty query string.
- Reject non-default `EncodeOptions.Delimiter` values for non-empty output because RestSharp query parameters are joined with `&`.
- Split QsNet output into query pairs and add each pair with `request.AddQueryParameter(name, value, encode: false)`.
- Preserve duplicate keys, encoded bracket notation, empty values, and key-only pairs by passing `null` for key-only values.

## Non-goals

- Do not modify core `QsNet`.
- Do not use RestSharp `AddObject` or `AddObjectStatic` internally.
- Do not add `SetQsQueryParameters` in v1.
- Do not mutate `RestRequest.Resource` to append raw query text unless RestSharp query parameters stop preserving QsNet output.

## Implementation Updates

- Add `QsNet.RestSharp/QsNet.RestSharp.csproj` targeting `net10.0;netstandard2.0`.
- Add `QsNet.RestSharp/QsNetRestRequestExtensions.cs`.
- Add `QsNet.RestSharp.Tests/QsNet.RestSharp.Tests.csproj` targeting `net10.0`.
- Add `QsNet.RestSharp.Tests/QsNetRestRequestExtensionsTests.cs`.
- Add both projects to `QsNet.sln`.
- Bump `$(QsNetPackageVersion)` to `1.4.3`.
- Update `coverlet.runsettings`, `.github/workflows/test.yml`, `.github/workflows/publish.yml`, README, installation docs, DocFX metadata, DocFX TOC, and `CHANGELOG.md`.

## Test Plan

- Unit tests for null input, empty output, same-instance return, simple objects, nested objects, arrays, lists of complex objects, dictionaries, non-generic dictionaries, key/value pair sequences, and custom delimiter rejection.
- RestSharp `BuildUriString` tests for existing query strings, existing query parameters, duplicate keys, empty values, key-only strict-null pairs, encoded bracket notation, no `%255B` double encoding, and no wrapper key such as `query=`.
- Validate with `rtk dotnet restore`, `rtk dotnet build`, `rtk dotnet test`, and `rtk dotnet pack QsNet.RestSharp/QsNet.RestSharp.csproj -c Release --no-build`.

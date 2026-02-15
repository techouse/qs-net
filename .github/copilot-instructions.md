# AI Assistant Project Instructions (QsNet)

Concise, project-specific guidance for automated coding agents. Keep answers grounded in existing patterns; cite concrete files (in backticks) when suggesting changes.

## 1. Purpose & Architecture
- Library: Query string encode/decode parity with JS `qs` but idiomatic C#.
- Public surface: `Qs` static API (`QsNet/Qs.cs`) + extension helpers (`Extensions.cs`). All other code under `Internal/`, `Models/`, `Enums/`, `Constants/` is implementation detail.
- Core flow (Decode): Raw string -> token split (`Internal/Decoder` + delimiters) -> key path parsing (`Decoder.ParseKeys`) -> progressive structure merge (`Utils.Merge`) -> compaction + string-key normalization (`Utils.Compact`, `Utils.ToStringKeyDeepNonRecursive`).
- Core flow (Encode): Input normalization to `Dictionary<string, object?>` -> optional filter/sort -> iterative encode traversal via `Internal/Encoder.Encode` producing `key=value` parts -> delimiter join + optional prefix/sentinel.
- Data model: Heterogeneous tree of `Dictionary<object, object?>`, `List<object?>`, primitives, sentinel `Undefined` (represents omitted vs null). Lists can degrade to dictionaries when sparse / large indices / list parsing disabled.

## 2. Key Option Objects
- `EncodeOptions` & `DecodeOptions` (in `Models/`): Immutable-with-`CopyWith` pattern; avoid mutating after creation. Support dot notation, custom charset (`UTF-8` / Latin1), list formats (`ListFormat` enum), duplicate handling (`Duplicates` enum), strict null controls, custom (kind-aware) decoders, filters, sorting, and comma semantics.
- Always prefer `CopyWith` to derive tweaks inside iterative logic rather than new manual constructors.

## 3. Performance & Safety Constraints
- Hot paths: `Utils.Merge`, `Decoder.ParseKeys`, `Decoder.ParseQueryStringValues`, `DecodeOptions.Decode()`, `EncodeOptions.Encode()`, and `Encoder.Encode`. Minimize allocations: prefer explicit loops, pre-sized collections, and list reuse; do not introduce LINQ allocation operators (`Cast`, `Where`, `Select`, `ToList`, `ToDictionary`, `GroupBy`) in these paths unless benchmark evidence justifies it.
- Depth / parameter / list limits guard untrusted input (see `DecodeOptions.Depth`, `ParameterLimit`, `ListLimit`). Do not remove; if extending, keep defaults protective and feature-gated behind options.
- Cycle detection in encode implemented with `SideChannelFrame`; maintain when adding new container handling.

## 4. Conventions
- Public API additions require: docs (`docs/api` via DocFX), tests (`QsNet.Tests`), README snippet if user-facing.
- Tests: xUnit + FluentAssertions. Naming style for new tests: `Should<Behavior>` in fact methods; extremely exhaustive existing tests—mirror patterns instead of new frameworks.
- Encoding/decoding examples in README serve as canonical behavior; keep them synchronized when changing logic.
- Use `Undefined.Create()` only to signal omission during encode filtering; never return it from public API results.

## 5. Adding Features / Changes
When implementing:
1. Introduce enums/flags in `Enums/` if expanding formatting or merge strategies; wire into option objects with backward-compatible defaults.
2. Update `Encoder` / `Decoder` via small, composable branches; avoid large monolithic condition blocks—follow existing structured `switch`/pattern matching approach.
3. Extend tests by cloning an analogous existing case (search in `EncodeTests.cs` or `DecodeTests.cs`) to ensure parity.
4. If behavior alters output ordering, ensure a deterministic `Sort` pathway or adjust affected tests.

## 6. Edge Cases Already Handled (Replicate Patterns)
- Mixed list/dictionary notations (indices + named keys) unify into dictionary (see tests around `a[0]=b&a[b]=c`).
- Large index escalation to dictionary when index > `ListLimit`.
- Strict null handling vs empty string differentiation (search: `StrictNullHandling` in tests).
- Charset sentinel logic (UTF-8 vs Latin1) prepends `utf8=...` only when `CharsetSentinel=true`.
- Comma list format round-tripping via `CommaRoundTrip` to append `[]` when needed.

## 7. Common Pitfalls
- Forgetting to encode dots only when `EncodeDotInKeys` is true (keys vs values distinction). Ensure coherent interplay with `AllowDots` / `DecodeDotInKeys` (see `DecodeOptions` guard throwing if inconsistent).
- Returning raw `Undefined` to consumers—must be filtered before final object emission.
- Introducing recursion without stack protection; existing decode compaction avoids deep recursion by iterative structure merges—keep that style.
- Breaking netstandard2.0 conditional code paths (`#if NETSTANDARD2_0`)—mirror dual implementations where regex source generators differ.

## 8. Build & Test Workflow
Run from repo root:
```bash
dotnet restore
dotnet build
dotnet test
```
Coverage (optional local):
```bash
dotnet test --collect:"XPlat Code Coverage"
```
Benchmarks (PR justification only): run the `QsNet.Benchmarks` project with BenchmarkDotNet (see `benchmarks/QsNet.Benchmarks`). Do NOT add benchmark results to source control.
Formatting:
```bash
dotnet format
```

## 9. Documentation
- Public surface changes: update `README.md` usage snippets + `docs/index.md` narrative if semantics shift.
- API reference generated via DocFX; keep XML docs on new public members concise and reflective of actual behavior.

## 10. When Unsure
- Search existing exhaustive tests before inventing new semantics—they likely exist.
- Preserve option backward compatibility; if a new feature interacts with existing flags, document precedence in XML comments + this file.

(End)

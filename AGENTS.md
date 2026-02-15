# Repository Guidelines

## Project Structure & Module Organization
- `QsNet/` holds the library; public API in `Qs.cs`, internal pipeline in `Internal/`, options in `Models/`.
- `QsNet.Tests/` contains xUnit suites; reusable fixtures in `Fixtures/` and sample payloads under `Fixtures/Data/`.
- `QsNet.Comparison/` is a behavioral parity/comparison harness (C# vs JS `qs`) and support scripts.
- `benchmarks/QsNet.Benchmarks/` contains BenchmarkDotNet perf harnesses; run only when validating performance changes.
- Generated artifacts (`benchmarks/`, `TestResults/`, `coveragereport/`) are optional outputs; never check in new ones without need.
- `docs/` feeds DocFX; update when public API or narratives shift.

## Build, Test, and Development Commands
```bash
dotnet restore                    # sync NuGet dependencies for the solution
dotnet build                      # compile all projects with current configuration
dotnet test                       # run the default xUnit suite
dotnet test --collect:"XPlat Code Coverage"   # emit coverage report into TestResults/
dotnet watch test                 # iterate with live test reruns
dotnet format                     # apply EditorConfig-driven formatting
```
Run commands from the repo root to target `QsNet.sln`.

## Coding Style & Naming Conventions
- Follow `.editorconfig`: 4 spaces, `var` when type is evident, expression-bodied members for concise methods.
- Public APIs use PascalCase; private fields camelCase or `_camelCase` for readonly.
- Keep parser/encoder hot paths allocation-light; avoid LINQ in hot paths (for example `Utils.Merge`, `Decoder.ParseKeys`, `Decoder.ParseQueryStringValues`, `Encoder.Encode`) and prefer explicit loops with pre-sized collections.
- Prefer `Span<T>` and `StringBuilder` per existing implementations where they improve hot-path performance.
- Favor descriptive method names like `EncodeComplexArray`; match namespace layout to folder structure.

## Testing Guidelines
- xUnit + FluentAssertions; place new tests alongside related classes under `QsNet.Tests`.
- Name tests in `Should...` form (`ShouldDecodeNestedObjects`); group with nested classes when scenarios grow.
- When touching parser logic, extend fixtures under `Fixtures/Data/` and prefer table-driven facts.
- Expect coverage checks in CI; ensure `dotnet test --collect:"XPlat Code Coverage"` stays green before PRs.

## Commit & Pull Request Guidelines
- Use focused, present-tense commit messages (`Add Latin1 sentinel decoder`); Conventional Commits are welcome but optional.
- Reference issues (`Fixes #123`) and document user-visible changes in the PR body.
- PRs should describe motivation, include test evidence or coverage snippets, and attach benchmark notes when altering perf-sensitive paths.
- Request review once CI passes; screenshots are only needed when docs or assets change.

## Security & Configuration Tips
- Store secrets outside the repo; see `SECURITY.md` for the disclosure process.
- Keep NuGet dependencies updated via `dotnet restore`; flag unusual dependency changes in PR descriptions.

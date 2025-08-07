# Contributing to `QsNet`

Thanks for your interest in improving **QsNet**! This project welcomes PRs, issues, and discussion.  
Please read this guide before contributing.

> A friendly reminder: this project follows a Code of Conduct. See `CODE-OF-CONDUCT.md`.

---

## Supported toolchain

- **.NET:** 8.0+
- **C#:** 12.0+
- **IDE:** JetBrains Rider, Visual Studio 2022, or Visual Studio Code
- **Testing:** xUnit + FluentAssertions

If you find breakage on newer .NET versions, open an issue with repro details.

---

## Getting started

```bash
# Clone
git clone https://github.com/techouse/QsNet.git
cd QsNet

# Restore dependencies
dotnet restore

# Run the full test suite
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run a specific test class
dotnet test --filter "ClassName=DecodeTests"

# Run a specific test method
dotnet test --filter "MethodName=ShouldDecodeNestedObjects"

# Build (without running tests)
dotnet build
```

---

## Code style & formatting (EditorConfig + Rider)

This repo uses **EditorConfig** and follows C# coding conventions.

**IDE setup (JetBrains Rider)**

1. Rider should automatically pick up the `.editorconfig` file in the repo root.
2. (Recommended) Turn on **Reformat code on Save** (Settings → Tools → Actions on Save → _Reformat and cleanup code_).
3. Enable **Code cleanup on save** with the default profile.

**Visual Studio setup**

1. Visual Studio automatically respects `.editorconfig` settings.
2. Enable **Format document on save** (Tools → Options → Text Editor → Code Cleanup → _Configure code cleanup on save_).

**General style guidelines**

- 4-space indentation, meaningful names, small/focused methods where reasonable.
- Use `var` for local variables when the type is obvious.
- Prefer expression-bodied members for simple properties and methods.
- Keep hot-path methods allocation-light; use `Span<T>` and `StringBuilder` where appropriate.
- Follow standard C# naming conventions (PascalCase for public members, camelCase for private fields).

---

## Tests

We use **xUnit** with **FluentAssertions** for unit tests. When you change code paths that touch parsing or encoding, add or update tests.

- Run all tests: `dotnet test`
- Run with coverage: `dotnet test --collect:"XPlat Code Coverage"`
- HTML coverage report: Use `reportgenerator` tool or IDE coverage features
- Run tests in watch mode: `dotnet watch test`

### Test structure

```
QsNet.Tests/
  DecodeTests.cs          # Core decoding functionality
  EncodeTests.cs          # Core encoding functionality
  ExampleTests.cs         # Real-world usage examples
  EndToEndTests.cs        # Integration tests
  UtilsTests.cs           # Utility method tests
  Fixtures/
    Data/                 # Test data and cases
    DummyEnum.cs          # Test enums and types
```

### Writing tests

- Use descriptive test method names: `ShouldDecodeNestedObjectsWithBracketNotation`
- Use FluentAssertions for readable assertions: `result.Should().ContainKey("foo")`
- Group related tests in nested classes or use test collections
- Add test cases for edge cases and error conditions

Example test structure:
```csharp
public class DecodeTests
{
    [Fact]
    public void ShouldDecodeSimpleKeyValuePair()
    {
        // Act
        var result = Qs.Decode("a=b");
        
        // Assert
        result.Should().ContainKey("a")
            .WhoseValue.Should().Be("b");
    }
}
```

---

## Project layout (high level)

```
QsNet/
  Qs.cs                   # Public API (Decode/Encode static methods)
  Extensions.cs           # Extension methods
  Constants/
    HexTable.cs           # Hex encoding lookup tables
  Enums/
    Duplicates.cs         # How to handle duplicate keys
    Format.cs             # RFC format options
    ListFormat.cs         # Array serialization formats
    Sentinel.cs           # Charset sentinel handling
  Internal/
    Decoder.cs            # Core decoding logic
    Encoder.cs            # Core encoding logic
    Utils.cs              # Utility methods and helpers
    SideChannelFrame.cs   # Side-channel data for nested parsing
  Models/
    DecodeOptions.cs      # Configuration for decoding
    EncodeOptions.cs      # Configuration for encoding
    Delimiter.cs          # Parameter delimiter abstractions
    Filter.cs             # Value filtering abstractions
    Undefined.cs          # Represents undefined values
QsNet.Tests/
  *Tests.cs              # xUnit test classes
  Fixtures/              # Test data and helper types
```

---

## Compatibility with JS `qs`

This port aims to mirror the semantics of [`qs`](https://github.com/ljharb/qs) (including edge cases).  
If you notice divergent behavior, please:

1. Add a failing test that demonstrates the difference.
2. Reference the `qs` test or behavior you expect.
3. Propose a fix, or open a focused issue.

---

## Performance notes

- Hot paths (parameter splitting, bracket scanning, URL encoding/decoding) should use `Span<T>` and avoid allocations where possible.
- Prefer `StringBuilder` and pre-sized collections when possible.
- Use `StringComparison.Ordinal` for performance-critical string operations.
- Avoid creating intermediate dictionaries/lists in tight loops.
- Watch for algorithmic complexity (e.g., nested scans, recursive parsing).
- Consider using `ArrayPool<T>` for temporary arrays in hot paths.

If you submit performance changes, include a short note and—if available—a benchmark using BenchmarkDotNet.

---

## Submitting a change

1. **Open an issue first** for big changes to align on approach.
2. **Small, focused PRs** are easier to review and land quickly.
3. **Add tests** that cover new behavior and edge cases.
4. **Keep public API stable** unless we agree on a version bump.
5. **Update documentation** if you change public APIs or behavior.
6. **Changelog entry** (in the PR description is fine) for user-visible changes.

### Commit/PR style

- Clear, descriptive commits. Conventional Commits welcome but not required.
- Reference issues as needed, e.g., "Fixes #123".
- Prefer present tense: "Add X", "Fix Y".
- Keep commits focused and atomic.

### Branch naming

Use a short, descriptive branch: `fix/latin1-entities`, `feat/custom-delimiters`, `perf/span-optimization`, etc.

---

## Code review checklist

Before submitting your PR, please verify:

- [ ] All tests pass: `dotnet test`
- [ ] Code builds without warnings: `dotnet build --configuration Release --verbosity normal`
- [ ] New functionality has corresponding tests
- [ ] Public API changes are documented
- [ ] Performance-sensitive changes include benchmarks or profiling data
- [ ] Code follows the established patterns and style

---

## Releasing (maintainers)

1. Update version in `QsNet.csproj` (`<Version>` property).
2. Update `CHANGELOG.md` with release notes.
3. Ensure `dotnet test` and `dotnet build --configuration Release` pass.
4. Create a git tag: `git tag v1.x.y`
5. Push tag: `git push origin v1.x.y`
6. Build and publish to NuGet:
   ```bash
   dotnet pack --configuration Release
   dotnet nuget push bin/Release/QsNet.*.nupkg --source https://api.nuget.org/v3/index.json
   ```
7. Create GitHub release with release notes:
   - Added/Changed/Fixed
   - Breaking changes (if any)
   - Migration guide for major versions

---

## Security

If you believe you've found a vulnerability, please **do not** open a public issue.  
Email the maintainer instead (see GitHub profile). We'll coordinate a fix and disclosure timeline.

See `SECURITY.md` for more details on our security policy.

---

## Questions?

Open a discussion or issue with as much detail as possible (input, expected vs actual output, environment, .NET version).  
Include code samples and stack traces when applicable.

Thanks again for helping make `QsNet` robust and performant!

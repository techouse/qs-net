---
name: Bug report
about: The library crashes, produces incorrect encoding/decoding, or behaves unexpectedly.
title: ''
labels: bug
assignees: techouse
---

<!--
  Since this is a port of `qs`, please check the original repo for related issues:
  https://github.com/ljharb/qs/issues
  If you find a relevant issue or spec note, please link it here.
-->

## Summary

<!-- A clear and concise description of what the bug is. -->

## Steps to Reproduce

<!-- Include full steps so we can reproduce the problem. Prefer a minimal repro. -->

1. ...
2. ...
3. ...

**Expected result**  
<!-- What did you expect to happen? -->

**Actual result**  
<!-- What actually happened? Include exact output / string values where relevant. -->

## Minimal Reproduction

> The simplest way is a **single unit test** that fails.  
> Create a minimal .NET project or use your existing one and add a failing test demonstrating the issue.

<details>
<summary>Failing C# test</summary>

```csharp
using QsNet;
using FluentAssertions;
using Xunit;

public class ReproTests
{
    [Fact]
    public void Repro()
    {
        // Replace with the minimal input that fails:
        var result = Qs.Decode("a[b]=1");
        result.Should().ContainKey("a")
            .WhoseValue.Should().BeEquivalentTo(new Dictionary<string, object?> { ["b"] = "1" });
    }
}
```
</details>

If the issue only appears when **encoding**, add the minimal input + options used:
```csharp
using QsNet;
using QsNet.Models;

var data = new Dictionary<string, object?> { ["a"] = new List<object?> { "x", "y" } };
var output = Qs.Encode(data, new EncodeOptions { Encode = false });
Console.WriteLine(output); // <-- paste the actual output and the expected output in the issue
```

## Logs

Please include relevant logs:

- .NET CLI + tests:
  ```bash
  dotnet test --logger "console;verbosity=detailed"
  ```

- If you created a small demo project, include the **full console output** from the failing run.

- If a specific input string causes the issue, paste that exact string together with the **actual** and **expected** decoded/encoded structures.

<details>
<summary>Console output</summary>

```
# paste here
```
</details>

## Environment

- **OS**: <!-- e.g., macOS 14.5 / Ubuntu 22.04 / Windows 11 -->
- **.NET**: output of `dotnet --version`
- **Runtime**: output of `dotnet --info` (framework versions)
- **C#**: <!-- e.g., 12.0 -->
- **QsNet** version: <!-- e.g., 1.0.0 -->
- **Charset** in use (if relevant): <!-- UTF-8 / Latin1 -->
- **Target Framework**: <!-- e.g., net8.0 -->

### Dependency snippet (from your .csproj file)

```xml
<PackageReference Include="QsNet" Version="<version>" />
<PackageReference Include="xunit" Version="<version>" />
<PackageReference Include="FluentAssertions" Version="<version>" />
```

> If you use custom encoders/decoders, filters, or other advanced options, please mention and show their configuration.

## ASP.NET Core / Web details (if applicable)

- **ASP.NET Core** version:
- **Target Framework**:
- **Hosting model** (Kestrel, IIS, etc.):
- **Request/Response context** (if the issue is related to web scenarios):

```csharp
// Example: Controller action or middleware where the issue occurs
[HttpGet]
public IActionResult Get([FromQuery] string queryString)
{
    var result = Qs.Decode(queryString);
    // Issue happens here...
}
```

## Is this a regression?

- Did this work in a previous version of QsNet? If so, which version?

## Additional context

- Links to any related `qs` JavaScript issues/spec notes:
- Any other libraries involved (ASP.NET Core, HTTP clients, frameworks, etc.) and versions:
- Edge cases (e.g., very deep nesting, extremely large strings, Latin1 with numeric entities, RFC1738 vs RFC3986 spaces, DateTime serialization, comma list format, etc.):
- Performance concerns (if the issue is performance-related, please include timing data):

## Stack Trace (if applicable)

<details>
<summary>Exception details</summary>

```
# paste full exception and stack trace here
```
</details>

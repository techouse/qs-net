## ASP.NET Core

`QsNet.AspNetCore` provides thin helpers for using QsNet with ASP.NET Core 10
applications. The core `QsNet` package stays framework-agnostic; install this
package only where ASP.NET Core APIs are useful.

```bash
dotnet add package QsNet.AspNetCore
```

### Add qs-style query strings to URLs

```csharp
using QsNet.AspNetCore;

var url = "/api/search#results".AddQueryString(
    new Dictionary<string, object?>
    {
        ["filter"] = new Dictionary<string, object?> { ["name"] = "Alice" },
        ["tags"] = new List<object?> { "one", "two" },
    }
);

// "/api/search?filter%5Bname%5D=Alice&tags%5B0%5D=one&tags%5B1%5D=two#results"
```

The helper appends the encoded QsNet output directly. It preserves URI fragments,
uses `?` or `&` as appropriate, and does not pass bracket notation through
ASP.NET Core `QueryHelpers.AddQueryString`.

### Decode an ASP.NET Core request query

```csharp
using QsNet.AspNetCore;

Dictionary<string, object?> query = httpContext.Request.ToQueryMap();
```

Decoder options flow through unchanged:

```csharp
using QsNet.Models;

var query = httpContext.Request.ToQueryMap(new DecodeOptions
{
    AllowDots = true,
});
```

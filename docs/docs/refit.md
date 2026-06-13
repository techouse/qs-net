## Refit

`QsNet.Refit` provides small query wrapper types for using QsNet with Refit
query parameters. The core `QsNet` package stays Refit-free, and `QsNet.Refit`
itself does not take a runtime dependency on Refit.

Install `QsNet.Refit` in the project that builds query wrappers:

```bash
dotnet add package QsNet.Refit
```

Install `Refit` separately in the project that declares or uses Refit API
interfaces.

### Pass a QsNet query through Refit

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
    filter = new
    {
        where = new
        {
            name = "John",
            age = new { gte = 30 },
        },
    },
    tags = new[] { "a", "b" },
}));
```

Expected query shape:

```text
/products?filter%5Bwhere%5D%5Bname%5D=John&filter%5Bwhere%5D%5Bage%5D%5Bgte%5D=30&tags%5B0%5D=a&tags%5B1%5D=b
```

`[QueryUriFormat(UriFormat.Unescaped)]` is required so Refit does not double
encode QsNet's already encoded keys. `QsQuery` exposes read-only key/value pairs
to Refit, so Refit never flattens the original DTO itself.

### ASP.NET Core-style indexed list keys

For APIs that bind query strings with ASP.NET Core-style indexed list keys, use
dot notation for object properties:

```csharp
using QsNet.Models;
using QsNet.Refit;
using Refit;

public interface IUsersApi
{
    [Get("/users")]
    [QueryUriFormat(UriFormat.Unescaped)]
    Task<List<User>> GetUsers([Query] QsQuery query);
}

await api.GetUsers(QsQuery.From(
    new UserQuery
    {
        UserId = 1,
        Roles = [new Role { Name = "Developer", Level = 1 }],
    },
    new EncodeOptions { AllowDots = true }));
```

Expected query shape:

```text
/users?UserId=1&Roles%5B0%5D.Name=Developer&Roles%5B0%5D.Level=1
```

This addresses the workaround discussed in Refit issue #1106: pre-serialize the
query with QsNet and let Refit send the formatted query pairs.

### Non-goal: Refit `[Query]` object serialization

`QsNet.Refit` does not change Refit's native `[Query]` object serializer. Avoid
this shape when qs-style nested query strings are required:

```csharp
[Get("/users")]
Task<List<User>> GetUsers([Query] UserQuery query);
```

Use the `QsQuery` wrapper instead:

```csharp
[Get("/users")]
[QueryUriFormat(UriFormat.Unescaped)]
Task<List<User>> GetUsers([Query] QsQuery query);
```

### Limitations

Refit's query pipeline sends normal `key=value` pairs joined with `&`.
`QsQuery` therefore rejects QsNet outputs that cannot be represented by that
pipeline, including custom `EncodeOptions.Delimiter` values and key-only pairs
from `StrictNullHandling = true`.

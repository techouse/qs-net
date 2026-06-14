## RestSharp

`QsNet.RestSharp` adds qs-style nested query parameters to RestSharp requests.
The core `QsNet` package stays RestSharp-free.

Install `QsNet.RestSharp` in the project that builds RestSharp requests:

```bash
dotnet add package QsNet.RestSharp
```

### Add QsNet query parameters

```csharp
using QsNet.RestSharp;
using RestSharp;

var request = new RestRequest("products")
    .AddQsQueryParameters(new
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
    });
```

Expected query shape:

```text
/products?filter%5Bwhere%5D%5Bname%5D=John&filter%5Bwhere%5D%5Bage%5D%5Bgte%5D=30&tags%5B0%5D=a&tags%5B1%5D=b
```

The helper uses QsNet to generate the nested query string, then adds each
already encoded key/value pair to RestSharp with query encoding disabled. This
avoids double-encoding bracket notation such as `%5B` into `%255B`.

### Lists of Complex Objects

```csharp
var request = new RestRequest("users")
    .AddQsQueryParameters(new
    {
        roles = new[]
        {
            new { name = "Developer", level = 1 },
        },
    });
```

Expected query shape:

```text
/users?roles%5B0%5D%5Bname%5D=Developer&roles%5B0%5D%5Blevel%5D=1
```

### Why Not AddObject?

RestSharp's `AddObject` and `AddObjectStatic` are useful for simple primitive
properties and primitive collections. They do not provide QsNet's nested object
graph serialization policy. Use `AddQsQueryParameters` when the server expects
qs-style bracket notation.

### Non-goal

`QsNet.RestSharp` does not replace RestSharp's existing query APIs. It only
adds an ergonomic path for appending QsNet-generated query parameters.

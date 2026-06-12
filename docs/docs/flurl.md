## Flurl

`QsNet.Flurl` provides thin helpers for adding QsNet-generated nested query
strings to Flurl URLs. The core `QsNet` package stays Flurl-free; install this
package only where Flurl URL builders are useful.

```bash
dotnet add package QsNet.Flurl
```

### Append qs-style query strings

```csharp
using Flurl;
using QsNet.Flurl;

var url = "https://api.example.com"
    .AppendPathSegment("products")
    .AppendQsQueryParams(new
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

// "https://api.example.com/products?filter%5Bwhere%5D%5Bname%5D=John&filter%5Bwhere%5D%5Bage%5D%5Bgte%5D=30&tags%5B0%5D=a&tags%5B1%5D=b"
```

`AppendQsQueryParams` appends QsNet's already encoded output directly to the
Flurl `Url.Query`. It does not pass the complete qs-style string through Flurl's
normal query-parameter APIs, so encoded bracket notation is not double-encoded.

### Replace query strings

```csharp
using QsNet.Flurl;

var url = "https://api.example.com/products?existing=1#results"
    .SetQsQueryParams(new { filter = new { name = "John" } });

// "https://api.example.com/products?filter%5Bname%5D=John#results"
```

### Flurl.Http chaining

```csharp
using Flurl;
using Flurl.Http;
using QsNet.Flurl;

var response = await "https://api.example.com"
    .AppendPathSegment("products")
    .AppendQsQueryParams(new
    {
        filter = new
        {
            where = new { name = "John" },
        },
    })
    .GetJsonAsync<ProductSearchResponse>();
```

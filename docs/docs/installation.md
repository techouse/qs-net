## Installation

### NuGet Package Manager

```
Install-Package QsNet
```

### .NET CLI

```bash
dotnet add package QsNet
```

### Package Reference

```xml
<PackageReference Include="QsNet" Version="<version>" />
```

## ASP.NET Core integration

For ASP.NET Core 10 / `net10.0` applications, install the optional integration
package:

```bash
dotnet add package QsNet.AspNetCore
```

```xml
<PackageReference Include="QsNet.AspNetCore" Version="<version>" />
```

## Flurl integration

For applications using Flurl URL builders, install the optional integration
package:

```bash
dotnet add package QsNet.Flurl
```

```xml
<PackageReference Include="QsNet.Flurl" Version="<version>" />
```

## Refit integration

For applications using Refit query parameters, install the optional
integration package:

```bash
dotnet add package QsNet.Refit
```

```xml
<PackageReference Include="QsNet.Refit" Version="<version>" />
```

`QsNet.Refit` does not depend on Refit at runtime. Install `Refit` separately in
the application or test project that declares the Refit API interface.

## RestSharp integration

For applications using RestSharp request builders, install the optional
integration package:

```bash
dotnet add package QsNet.RestSharp
```

```xml
<PackageReference Include="QsNet.RestSharp" Version="<version>" />
```

---

## Requirements

- `QsNet`: `net10.0`, `netstandard2.0`
- `QsNet.AspNetCore`: `net10.0`
- `QsNet.Flurl`: `net10.0`, `netstandard2.0`
- `QsNet.Refit`: `net10.0`, `netstandard2.0`
- `QsNet.RestSharp`: `net10.0`, `netstandard2.0`

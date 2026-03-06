# JG.WebKit.Router

[![NuGet](https://img.shields.io/nuget/v/JG.WebKit.Router?logo=nuget)](https://www.nuget.org/packages/JG.WebKit.Router)
[![Downloads](https://img.shields.io/nuget/dt/JG.WebKit.Router?color=%230099ff&logo=nuget)](https://www.nuget.org/packages/JG.WebKit.Router)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](./LICENSE)
[![CI](https://github.com/jamesgober/dotnet-webkit-router/actions/workflows/ci.yml/badge.svg)](https://github.com/jamesgober/dotnet-webkit-router/actions)

---

A high-performance trie-based HTTP router for ASP.NET Core. Built for speed with O(1) route matching, compiled per-route execution chains, and zero-allocation path parsing.

Part of the **JG WebKit** collection.

## Features

- **Trie-based matching** — O(1) average route lookups using an immutable trie data structure
- **Compiled execution chains** — routes execute pre-compiled handler chains, not middleware pipelines
- **Hot-reload** — add, modify, or remove routes at runtime without restarting
- **Route groups** — organize routes with shared prefixes and chain nodes
- **11 built-in constraints** — `int`, `long`, `guid`, `bool`, `slug`, `alpha`, `alphanum`, `filename`, `range(min,max)`, `length(min,max)`, `regex(pattern)`
- **Custom constraints** — implement `IRouteConstraint` for custom validation
- **Dynamic routes** — load routes from any source via `IRouteProvider`
- **Route metadata** — attach custom metadata to routes
- **Zero-allocation path parsing** — uses `ReadOnlySpan<char>` for minimal GC pressure
- **Thread-safe** — lock-free hot path with atomic trie swap on reload
- **Fluent API** — chainable route registration with `MapRoute()` and `MapRouteGroup()`

## Installation

```bash
dotnet add package JG.WebKit.Router
```

## Quick Start

### Basic Routing

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebKitRouter();

var app = builder.Build();
app.UseWebKitRouter();

// Simple route
app.MapRoute("GET", "/", (ctx, ct) => 
    ValueTask.FromResult(RouteResult.Ok("Hello, World!")));

// Route with parameter
app.MapRoute("GET", "/users/{id:int}", (ctx, ct) =>
{
    var userId = ctx.Match.Parameters["id"];
    return ValueTask.FromResult(RouteResult.Json(new { userId }));
});

app.Run();
```

### Route Groups

```csharp
app.MapRouteGroup("/api/v1", group =>
{
    group.MapRoute("GET", "/users", ListUsers);
    group.MapRoute("GET", "/users/{id:int}", GetUser);
    group.MapRoute("POST", "/users", CreateUser);
});
```

### Execution Chains

Chain nodes execute before the route handler and can short-circuit:

```csharp
public class AuthChainNode : IChainNode
{
    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? false)
            return ChainResult.Stop(RouteResult.Unauthorized());
        return ChainResult.Next();
    }
}

app.MapRoute("GET", "/secure", SecureHandler)
    .AddChainNode(new AuthChainNode());
```

## Documentation

- **[API Documentation](./docs/API.md)** — Complete API reference with examples
- **[Advanced Patterns](./docs/ADVANCED.md)** — Real-world patterns and use cases
- **[Practical Guides](./docs/GUIDES.md)** — Step-by-step implementation guides

## Performance

- **Route matching:** O(1) average lookup via trie dictionary
- **Path parsing:** Zero allocations using `ReadOnlySpan<char>`
- **Request handling:** No locks on hot path (volatile reads only)
- **Regex constraints:** Compiled and cached at build time

## License

Licensed under the Apache License 2.0. See [LICENSE](./LICENSE) for details.

---

**Get started:** Install from NuGet, then check out the [API documentation](./docs/API.md).

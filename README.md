# dotnet-webkit-router

[![NuGet](https://img.shields.io/nuget/v/JG.WebKit.Router?logo=nuget)](https://www.nuget.org/packages/JG.WebKit.Router)
[![Downloads](https://img.shields.io/nuget/dt/JG.WebKit.Router?color=%230099ff&logo=nuget)](https://www.nuget.org/packages/JG.WebKit.Router)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](./LICENSE)
[![CI](https://github.com/jamesgober/dotnet-webkit-router/actions/workflows/ci.yml/badge.svg)](https://github.com/jamesgober/dotnet-webkit-router/actions)

---

A high-performance, data-driven HTTP router for ASP.NET Core. Trie-based matching with O(1) lookups, database-backed dynamic routes, hot-reload without restarts, and per-route middleware. Supports static, dynamic, and hybrid routing strategies simultaneously.

Part of the **JG WebKit** collection.

## Features

- **Trie-based matching** — O(1) average route lookups, no regex scanning on every request
- **Three routing strategies** — static (code-defined), dynamic (DB-loaded), hybrid (both)
- **Hot-reload** — add, modify, or remove routes at runtime without restarting the application
- **Per-route middleware** — attach different middleware stacks to different routes or groups
- **Route groups** — shared prefixes, middleware, and constraints for related endpoints
- **Typed parameters** — `{id:int}`, `{slug:regex(^[a-z-]+$)}`, `{key:guid}`, `{**catchall}`
- **Route constraints** — built-in (int, guid, regex, length, range) and custom `IRouteConstraint`
- **Conflict detection** — catch ambiguous routes at registration time, not at runtime
- **Priority system** — explicit ordering when multiple routes could match
- **Proxy-aware URLs** — X-Forwarded-Host/Proto/Prefix, trailing slash normalization, base path detection
- **Route caching** — compiled trie cached with configurable TTL and manual invalidation

## Installation

```bash
dotnet add package JG.WebKit.Router
```

## Quick Start

```csharp
builder.Services.AddWebKitRouter(options =>
{
    options.EnableTrailingSlashRedirect = true;
    options.CacheDuration = TimeSpan.FromMinutes(10);
});

app.MapRoute("GET", "/api/users/{id:int}", UserHandler.GetById);
app.MapRoute("GET", "/blog/{year:int}/{slug}", BlogHandler.GetPost);

app.MapRouteGroup("/api/v1", group =>
{
    group.UseMiddleware<ApiKeyMiddleware>();
    group.MapRoute("GET", "/products", ProductHandler.List);
});
```

## Documentation

- **[API Reference](./docs/API.md)** — Full API documentation and examples

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

Licensed under the Apache License 2.0. See [LICENSE](./LICENSE) for details.

---

**Ready to get started?** Install via NuGet and check out the [API reference](./docs/API.md).

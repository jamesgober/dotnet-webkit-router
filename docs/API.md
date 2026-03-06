# JG.WebKit.Router API Documentation

Complete guide to the trie-based HTTP routing library for ASP.NET Core.

## Table of Contents

1. [Quick Start](#quick-start)
2. [Core Concepts](#core-concepts)
3. [Route Definition & Matching](#route-definition--matching)
4. [Constraints](#constraints)
5. [Execution Chains](#execution-chains)
6. [Route Groups](#route-groups)
7. [Route Providers](#route-providers)
8. [Hot-Reload](#hot-reload)
9. [Configuration](#configuration)
10. [Advanced Examples](#advanced-examples)
11. [API Reference](#api-reference)

---

## Quick Start

### Installation

```xml
<PackageReference Include="JG.WebKit.Router" Version="1.0.0" />
```

### Basic Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register the router
builder.Services.AddWebKitRouter(options =>
{
    options.CaseSensitive = false;
    options.EnableTrailingSlashRedirect = false;
});

var app = builder.Build();

// Use the router middleware
app.UseWebKitRouter();

// Define a simple route
app.MapRoute("GET", "/", (ctx, ct) => 
    ValueTask.FromResult(RouteResult.Ok("Hello, World!")));

app.Run();
```

---

## Core Concepts

### What is JG.WebKit.Router?

JG.WebKit.Router is a **trie-based HTTP router** that:
- Matches incoming requests to routes using an immutable trie structure
- Executes pre-compiled per-route execution chains (not middleware)
- Provides zero-allocation path parsing
- Supports hot-reload with atomic trie swapping
- Offers 11 built-in parameter constraints

### Why Use JG.WebKit.Router?

1. **Performance**: O(1) route matching, zero allocations in hot path
2. **Type Safety**: Built-in constraints validate parameters
3. **Clean API**: Fluent route mapping without middleware complexity
4. **Extensibility**: Custom constraints, providers, and chain nodes
5. **Thread-Safe**: Lock-free design, safe hot-reload

### Request Flow

```
1. Incoming Request
   ↓
2. URL Normalization
   (trailing slash, case, path segments)
   ↓
3. Trie Matching
   (literal → constrained param → unconstrained param → wildcard)
   ↓
4. Constraint Validation
   (int, long, guid, bool, slug, alpha, alphanum, filename, range, length, regex)
   ↓
5. Chain Node Execution
   (sequential pre-route filters with early termination)
   ↓
6. Handler Execution
   (your async handler function)
   ↓
7. Response
   (RouteResult with status, headers, body)
```

---

## Route Definition & Matching

### Basic Route

```csharp
app.MapRoute("GET", "/api/users", (ctx, ct) => 
    ValueTask.FromResult(RouteResult.Ok("User list")));
```

### Routes with Parameters

```csharp
// Unconstrained parameter
app.MapRoute("GET", "/users/{id}", (ctx, ct) =>
{
    var id = ctx.Match.Parameters["id"];
    return ValueTask.FromResult(RouteResult.Json(new { id }));
});

// Constrained parameter
app.MapRoute("GET", "/users/{id:int}", (ctx, ct) =>
{
    var userId = int.Parse(ctx.Match.Parameters["id"]);
    return ValueTask.FromResult(RouteResult.Json(new { userId }));
});

// Multiple parameters
app.MapRoute("GET", "/blog/{year:int}/{month:int}/{slug}", (ctx, ct) =>
{
    var year = ctx.Match.Parameters["year"];
    var month = ctx.Match.Parameters["month"];
    var slug = ctx.Match.Parameters["slug"];
    return ValueTask.FromResult(RouteResult.Ok($"Post: {year}/{month}/{slug}"));
});
```

### Wildcard Routes (Catch-All)

```csharp
// Capture remaining path
app.MapRoute("GET", "/{**path}", (ctx, ct) =>
{
    var remainingPath = ctx.Match.Parameters["path"];
    return ValueTask.FromResult(RouteResult.Ok($"Caught: {remainingPath}"));
});

// Matches: /foo/bar/baz → path="foo/bar/baz"
```

### Route Matching Priority

Routes are matched in this order:
1. **Literal segments** (exact match, O(1) lookup)
2. **Constrained parameters** (validated by constraint)
3. **Unconstrained parameters** (accept any value)
4. **Wildcard** (catch remaining segments)

```csharp
// These routes work together (different priorities):
app.MapRoute("GET", "/users/me", (ctx, ct) => /* special handling */);
app.MapRoute("GET", "/users/{id:int}", (ctx, ct) => /* numeric id */);
app.MapRoute("GET", "/users/{username}", (ctx, ct) => /* any username */);

// Requests matched:
// GET /users/me → First route (literal)
// GET /users/123 → Second route (constrained param)
// GET /users/john → Third route (unconstrained param)
```

### HTTP Methods

```csharp
app.MapRoute("GET", "/api/items", GetItems);
app.MapRoute("POST", "/api/items", CreateItem);
app.MapRoute("PUT", "/api/items/{id:int}", UpdateItem);
app.MapRoute("DELETE", "/api/items/{id:int}", DeleteItem);
app.MapRoute("PATCH", "/api/items/{id:int}", PatchItem);

// Same path, different methods = different routes
```

---

## Constraints

### What are Constraints?

Constraints validate route parameters before execution. Use syntax: `{paramName:constraintType}`

### Built-in Constraints

#### Integer Constraints

```csharp
// 32-bit integer
app.MapRoute("GET", "/posts/{id:int}", (ctx, ct) =>
{
    var id = int.Parse(ctx.Match.Parameters["id"]);
    return ValueTask.FromResult(RouteResult.Json(new { id }));
});

// 64-bit integer
app.MapRoute("GET", "/data/{ref:long}", (ctx, ct) =>
{
    var reference = long.Parse(ctx.Match.Parameters["ref"]);
    return ValueTask.FromResult(RouteResult.Json(new { reference }));
});
```

#### GUID Constraint

```csharp
app.MapRoute("GET", "/items/{id:guid}", (ctx, ct) =>
{
    var itemId = Guid.Parse(ctx.Match.Parameters["id"]);
    return ValueTask.FromResult(RouteResult.Json(new { itemId }));
});
```

#### Boolean Constraint

```csharp
app.MapRoute("GET", "/config/{enabled:bool}", (ctx, ct) =>
{
    var enabled = bool.Parse(ctx.Match.Parameters["enabled"]);
    return ValueTask.FromResult(RouteResult.Json(new { enabled }));
});
```

#### String Format Constraints

```csharp
// URL-friendly slug
app.MapRoute("GET", "/posts/{slug:slug}", (ctx, ct) =>
{
    var slug = ctx.Match.Parameters["slug"];
    return ValueTask.FromResult(RouteResult.Ok($"Post: {slug}"));
});

// Letters only
app.MapRoute("GET", "/{name:alpha}", (ctx, ct) =>
{
    var name = ctx.Match.Parameters["name"];
    return ValueTask.FromResult(RouteResult.Ok($"Hello {name}"));
});

// Alphanumeric
app.MapRoute("GET", "/{code:alphanum}", (ctx, ct) =>
{
    var code = ctx.Match.Parameters["code"];
    return ValueTask.FromResult(RouteResult.Ok($"Code: {code}"));
});

// Safe filename
app.MapRoute("GET", "/download/{file:filename}", (ctx, ct) =>
{
    var file = ctx.Match.Parameters["file"];
    return ValueTask.FromResult(RouteResult.Ok($"Download: {file}"));
});
```

#### Range Constraint

```csharp
app.MapRoute("GET", "/page/{number:range(1,100)}", (ctx, ct) =>
{
    var page = int.Parse(ctx.Match.Parameters["number"]);
    return ValueTask.FromResult(RouteResult.Json(new { page }));
});
```

#### Length Constraint

```csharp
app.MapRoute("GET", "/search/{query:length(2,100)}", (ctx, ct) =>
{
    var query = ctx.Match.Parameters["query"];
    return ValueTask.FromResult(RouteResult.Json(new { query }));
});
```

#### Regex Constraint

```csharp
// Pattern validation
app.MapRoute("GET", "/code/{value:regex(^[A-Z]{3}\\d{3}$)}", (ctx, ct) =>
{
    var code = ctx.Match.Parameters["value"];
    return ValueTask.FromResult(RouteResult.Json(new { code }));
});
```

### Custom Constraints

```csharp
public class EvenNumberConstraint : IRouteConstraint
{
    public bool Match(string parameterName, ReadOnlySpan<char> value)
    {
        if (!int.TryParse(value, out int num))
            return false;
        return num % 2 == 0;
    }
}

builder.Services.AddRouteConstraint<EvenNumberConstraint>("even");

app.MapRoute("GET", "/numbers/{value:even}", (ctx, ct) =>
{
    var num = int.Parse(ctx.Match.Parameters["value"]);
    return ValueTask.FromResult(RouteResult.Json(new { num }));
});
```

---

## Execution Chains

### What are Chain Nodes?

Chain nodes execute **before the route handler**. They can validate, modify context, or short-circuit.

### Basic Chain Node

```csharp
public class LoggingChainNode : IChainNode
{
    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        Console.WriteLine($"Route: {context.Match.Path}");
        return ChainResult.Next();
    }
}

app.MapRoute("GET", "/api/users", handler)
    .AddChainNode(new LoggingChainNode());
```

### Authorization Chain Node

```csharp
public class AuthChainNode : IChainNode
{
    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? false)
            return ChainResult.Stop(RouteResult.Unauthorized());
        return ChainResult.Next();
    }
}

app.MapRoute("GET", "/admin", AdminHandler)
    .AddChainNode(new AuthChainNode());
```

### Rate Limiting Chain Node

```csharp
public class RateLimitChainNode : IChainNode
{
    private readonly Dictionary<string, (int count, DateTime reset)> _limits = new();

    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        var clientId = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        if (_limits.TryGetValue(clientId, out var limit) && DateTime.UtcNow < limit.reset)
        {
            if (limit.count >= 10)
            {
                var retryAfter = (int)(limit.reset - DateTime.UtcNow).TotalSeconds;
                return ChainResult.Stop(RouteResult.TooManyRequests(retryAfter.ToString()));
            }
        }
        
        return ChainResult.Next();
    }
}
```

### Multiple Chain Nodes

```csharp
app.MapRoute("POST", "/api/secure", Handler)
    .AddChainNode(new AuthChainNode())
    .AddChainNode(new RateLimitChainNode())
    .AddChainNode(new ValidationChainNode());
```

---

## Route Groups

### Basic Group

```csharp
app.MapRouteGroup("/api/v1", group =>
{
    group.MapRoute("GET", "/users", ListUsers);
    group.MapRoute("GET", "/users/{id:int}", GetUser);
    group.MapRoute("POST", "/users", CreateUser);
});
```

### Group with Shared Chain Nodes

```csharp
app.MapRouteGroup("/api/admin", group =>
{
    group.AddChainNode(new AuthChainNode());
    group.AddChainNode(new AdminAuthChainNode());
    
    group.MapRoute("GET", "/users", AdminListUsers);
    group.MapRoute("POST", "/users/{id:int}/suspend", SuspendUser);
    group.MapRoute("DELETE", "/users/{id:int}", DeleteUser);
});
```

---

## Route Providers

### Custom Database Provider

```csharp
public class DatabaseRouteProvider : IRouteProvider
{
    private readonly IDbContext _db;

    public DatabaseRouteProvider(IDbContext db) => _db = db;

    public async ValueTask<IReadOnlyList<RouteDefinition>> GetRoutesAsync(CancellationToken ct)
    {
        var routes = await _db.Routes.ToListAsync(ct);
        
        return routes.Select(r => new RouteDefinition
        {
            Method = r.HttpMethod,
            Path = r.UrlPattern,
            Handler = ResolveHandler(r.HandlerName),
        }).ToList();
    }

    private RouteHandler ResolveHandler(string name) => 
        async (ctx, ct) => await _db.InvokeHandlerAsync(name, ctx, ct);
}

builder.Services.AddRouteProvider<DatabaseRouteProvider>();
```

### Custom File-Based Provider

```csharp
public class YamlRouteProvider : IRouteProvider
{
    private readonly string _filePath;

    public YamlRouteProvider(string filePath) => _filePath = filePath;

    public async ValueTask<IReadOnlyList<RouteDefinition>> GetRoutesAsync(CancellationToken ct)
    {
        var yaml = await File.ReadAllTextAsync(_filePath, ct);
        var routes = ParseYaml(yaml);
        
        return routes.Select(r => new RouteDefinition
        {
            Method = r.Method,
            Path = r.Path,
            Handler = CreateHandler(r),
        }).ToList();
    }
}
```

---

## Hot-Reload

### Manual Reload

```csharp
var router = app.Services.GetRequiredService<IRouter>();
await router.ReloadAsync(CancellationToken.None);
```

### File Watcher

```csharp
public class RouteReloader : IHostedService
{
    private readonly IRouter _router;
    private FileSystemWatcher? _watcher;

    public RouteReloader(IRouter router) => _router = router;

    public Task StartAsync(CancellationToken ct)
    {
        _watcher = new FileSystemWatcher(".", "routes.yaml")
        {
            EnableRaisingEvents = true
        };

        _watcher.Changed += async (s, e) =>
            await _router.ReloadAsync(ct);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _watcher?.Dispose();
        return Task.CompletedTask;
    }
}

builder.Services.AddHostedService<RouteReloader>();
```

---

## Configuration

### RouterOptions

```csharp
builder.Services.AddWebKitRouter(options =>
{
    options.CaseSensitive = false;
    options.EnableTrailingSlashRedirect = false;
    options.ConflictPolicy = RouteConflictPolicy.LastWins;
    options.RespectProxyHeaders = true;
    options.MaxCompiledRegexCache = 100;
});
```

---

## Advanced Examples

### Full REST API

```csharp
public class Product { public int Id { get; set; } public string Name { get; set; } }

public class ProductApi
{
    private readonly List<Product> _products = new();

    public ValueTask<RouteResult> List(RequestContext ctx, CancellationToken ct) =>
        ValueTask.FromResult(RouteResult.Json(_products));

    public ValueTask<RouteResult> Get(RequestContext ctx, CancellationToken ct)
    {
        var id = int.Parse(ctx.Match.Parameters["id"]);
        var product = _products.FirstOrDefault(p => p.Id == id);
        return ValueTask.FromResult(product == null 
            ? RouteResult.NotFound() 
            : RouteResult.Json(product));
    }

    public async ValueTask<RouteResult> Create(RequestContext ctx, CancellationToken ct)
    {
        var product = await ctx.HttpContext.Request.ReadAsAsync<Product>(ct);
        product.Id = _products.Max(p => p.Id) + 1;
        _products.Add(product);
        return RouteResult.Json(product, 201);
    }
}

var api = new ProductApi();
app.MapRouteGroup("/api/products", group =>
{
    group.MapRoute("GET", "", api.List);
    group.MapRoute("GET", "/{id:int}", api.Get);
    group.MapRoute("POST", "", api.Create);
});
```

### Health Check Endpoint

```csharp
app.MapRoute("GET", "/health", async (ctx, ct) =>
{
    var health = new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        checks = new { database = "ok", cache = "ok" }
    };
    return RouteResult.Json(health);
});

app.MapRoute("GET", "/health/ready", async (ctx, ct) =>
{
    var ready = await CheckReadiness(ct);
    return ready ? RouteResult.Ok() : RouteResult.Error(503);
});
```

---

## API Reference

### RouteResult

```csharp
RouteResult.Ok()                              // 200
RouteResult.Ok(body)                          // 200 with body
RouteResult.Json(object)                      // 200 JSON
RouteResult.Json(object, statusCode)          // Custom status
RouteResult.Html(string)                      // 200 HTML
RouteResult.Redirect(url)                     // 302
RouteResult.BadRequest()                      // 400
RouteResult.Unauthorized()                    // 401
RouteResult.Forbidden()                       // 403
RouteResult.NotFound()                        // 404
RouteResult.TooManyRequests(retryAfter)       // 429
RouteResult.Error(statusCode)                 // Custom
```

### RequestContext

```csharp
public readonly struct RequestContext
{
    public HttpContext HttpContext { get; }
    public RouteMatch Match { get; }
    public IReadOnlyDictionary<string, object> RouteMetadata { get; }
}
```

### RouteMatch

```csharp
public readonly struct RouteMatch
{
    public string Method { get; }
    public string Path { get; }
    public IReadOnlyDictionary<string, string> Parameters { get; }
}
```

### ChainResult

```csharp
ChainResult.Next()                            // Continue
ChainResult.Stop(RouteResult)                 // Stop execution
```

### IChainNode

```csharp
public interface IChainNode
{
    ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct);
}
```

### IRouteConstraint

```csharp
public interface IRouteConstraint
{
    bool Match(string parameterName, ReadOnlySpan<char> value);
}
```

### IRouteProvider

```csharp
public interface IRouteProvider
{
    ValueTask<IReadOnlyList<RouteDefinition>> GetRoutesAsync(CancellationToken ct);
}
```

### IRouter

```csharp
public interface IRouter
{
    ValueTask<RouteResult> HandleRequestAsync(HttpContext httpContext, CancellationToken ct);
    ValueTask ReloadAsync(CancellationToken ct);
    ValueTask RegisterRouteAsync(RouteDefinition definition, CancellationToken ct);
}

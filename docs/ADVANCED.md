# Advanced Routing Patterns

Real-world examples and advanced patterns for JG.WebKit.Router.

## Table of Contents

1. [Versioned APIs](#versioned-apis)
2. [Multi-Tenant Routing](#multi-tenant-routing)
3. [Microservice Gateway](#microservice-gateway)
4. [CORS Handling](#cors-handling)
5. [Request/Response Logging](#requestresponse-logging)
6. [Circuit Breaker Pattern](#circuit-breaker-pattern)
7. [Caching Strategy](#caching-strategy)
8. [GraphQL Federation](#graphql-federation)
9. [OpenAPI Integration](#openapi-integration)
10. [Testing Patterns](#testing-patterns)

---

## Versioned APIs

Managing multiple API versions with JG.WebKit.Router.

### Route-Based Versioning

```csharp
// V1 API
app.MapRouteGroup("/api/v1", group =>
{
    group.MapRoute("GET", "/users", GetUsersV1);
    group.MapRoute("POST", "/users", CreateUserV1);
});

// V2 API (breaking changes)
app.MapRouteGroup("/api/v2", group =>
{
    group.MapRoute("GET", "/users", GetUsersV2);  // Different handler
    group.MapRoute("POST", "/users", CreateUserV2);
});

// V3 API
app.MapRouteGroup("/api/v3", group =>
{
    group.MapRoute("GET", "/users", GetUsersV3);
});

// Both versions coexist
// GET /api/v1/users → GetUsersV1
// GET /api/v2/users → GetUsersV2
// GET /api/v3/users → GetUsersV3
```

### Header-Based Versioning

```csharp
public class ApiVersionChainNode : IChainNode
{
    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        var version = context.HttpContext.Request.Headers["Api-Version"].ToString() ?? "1.0";
        context.HttpContext.Items["ApiVersion"] = version;
        return ChainResult.Next();
    }
}

app.MapRoute("GET", "/users", (ctx, ct) =>
{
    var version = ctx.HttpContext.Items["ApiVersion"]?.ToString() ?? "1.0";
    
    return version switch
    {
        "2.0" => ValueTask.FromResult(RouteResult.Json(GetUsersV2())),
        "3.0" => ValueTask.FromResult(RouteResult.Json(GetUsersV3())),
        _ => ValueTask.FromResult(RouteResult.Json(GetUsersV1()))
    };
})
.AddChainNode(new ApiVersionChainNode());
```

### Deprecation Warnings

```csharp
public class DeprecationWarningChainNode : IChainNode
{
    private readonly string _deprecatedSince;
    private readonly string _replacementRoute;

    public DeprecationWarningChainNode(string since, string replacement)
    {
        _deprecatedSince = since;
        _replacementRoute = replacement;
    }

    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        context.HttpContext.Response.Headers["Deprecated"] = "true";
        context.HttpContext.Response.Headers["Sunset"] = "Tue, 01 Jan 2025 00:00:00 GMT";
        context.HttpContext.Response.Headers["Link"] = $"<{_replacementRoute}>; rel=\"successor-version\"";
        
        return ChainResult.Next();
    }
}

app.MapRoute("GET", "/api/v1/old-endpoint", LegacyHandler)
    .AddChainNode(new DeprecationWarningChainNode("2024-01-01", "/api/v2/new-endpoint"));
```

---

## Multi-Tenant Routing

Routing requests to different handlers based on tenant.

### Subdomain-Based Tenancy

```csharp
public class TenantExtractionChainNode : IChainNode
{
    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        var host = context.HttpContext.Request.Host.Host;
        var tenant = host.Split('.')[0];  // Extract subdomain
        
        if (tenant == "www" || host.Contains("localhost"))
            return ChainResult.Stop(RouteResult.BadRequest("No tenant specified"));
        
        context.HttpContext.Items["TenantId"] = tenant;
        return ChainResult.Next();
    }
}

app.MapRoute("GET", "/api/data", (ctx, ct) =>
{
    var tenant = ctx.HttpContext.Items["TenantId"]?.ToString() ?? "";
    var data = GetTenantData(tenant);
    return ValueTask.FromResult(RouteResult.Json(data));
})
.AddChainNode(new TenantExtractionChainNode());

// Usage: customer1.example.com, customer2.example.com
```

### Path-Based Tenancy

```csharp
app.MapRouteGroup("/{tenant}", group =>
{
    group.AddChainNode(new TenantValidationChainNode());
    
    group.MapRoute("GET", "/api/data", (ctx, ct) =>
    {
        var tenant = ctx.Match.Parameters["tenant"];
        var data = GetTenantData(tenant);
        return ValueTask.FromResult(RouteResult.Json(data));
    });
});

// Usage: /acme/api/data, /globex/api/data
```

### Header-Based Tenancy

```csharp
public class TenantAuthorizationChainNode : IChainNode
{
    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        var tenantHeader = context.HttpContext.Request.Headers["X-Tenant-Id"].ToString();
        if (string.IsNullOrEmpty(tenantHeader))
            return ChainResult.Stop(RouteResult.BadRequest("Missing X-Tenant-Id header"));
        
        var user = context.HttpContext.User;
        var userTenants = GetUserTenants(user.FindFirst("sub")?.Value ?? "");
        
        if (!userTenants.Contains(tenantHeader))
            return ChainResult.Stop(RouteResult.Forbidden());
        
        context.HttpContext.Items["TenantId"] = tenantHeader;
        return ChainResult.Next();
    }
}
```

---

## Microservice Gateway

Routing to different microservices.

### Service Discovery

```csharp
public class ServiceRouterChainNode : IChainNode
{
    private readonly IServiceDiscovery _discovery;
    private readonly HttpClient _httpClient;

    public ServiceRouterChainNode(IServiceDiscovery discovery)
    {
        _discovery = discovery;
        _httpClient = new HttpClient();
    }

    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        var service = context.Match.Parameters.GetValueOrDefault("service");
        var serviceUrl = await _discovery.ResolveServiceAsync(service!, ct);
        
        if (serviceUrl == null)
            return ChainResult.Stop(RouteResult.NotFound($"Service {service} not found"));
        
        context.HttpContext.Items["ServiceUrl"] = serviceUrl;
        return ChainResult.Next();
    }
}

app.MapRouteGroup("/gateway/{service}", group =>
{
    group.AddChainNode(new ServiceRouterChainNode(serviceDiscovery));
    
    group.MapRoute("GET", "/{**path}", async (ctx, ct) =>
    {
        var serviceUrl = ctx.HttpContext.Items["ServiceUrl"]?.ToString() ?? "";
        var path = ctx.Match.Parameters.GetValueOrDefault("path") ?? "";
        
        var response = await new HttpClient().GetAsync($"{serviceUrl}/{path}", ct);
        var content = await response.Content.ReadAsStringAsync(ct);
        
        return RouteResult.Ok(content);
    });
});
```

### Load Balancing

```csharp
public class LoadBalancingChainNode : IChainNode
{
    private readonly string[] _instances;
    private int _currentIndex = 0;

    public LoadBalancingChainNode(string[] instances)
    {
        _instances = instances;
    }

    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        var instance = _instances[_currentIndex % _instances.Length];
        _currentIndex++;
        
        context.HttpContext.Items["TargetInstance"] = instance;
        return ChainResult.Next();
    }
}

var userServiceInstances = new[] { "http://user-1:5000", "http://user-2:5000", "http://user-3:5000" };

app.MapRouteGroup("/api/users", group =>
{
    group.AddChainNode(new LoadBalancingChainNode(userServiceInstances));
    
    group.MapRoute("GET", "/{id:int}", ForwardToService);
});
```

---

## CORS Handling

Managing Cross-Origin Resource Sharing.

### Simple CORS Chain Node

```csharp
public class CorsChainNode : IChainNode
{
    private readonly string[] _allowedOrigins;

    public CorsChainNode(params string[] origins)
    {
        _allowedOrigins = origins;
    }

    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        var origin = context.HttpContext.Request.Headers["Origin"].ToString();
        
        if (string.IsNullOrEmpty(origin))
            return ChainResult.Next();
        
        if (!_allowedOrigins.Contains(origin) && !_allowedOrigins.Contains("*"))
            return ChainResult.Stop(RouteResult.Forbidden());
        
        context.HttpContext.Response.Headers["Access-Control-Allow-Origin"] = origin;
        context.HttpContext.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE";
        context.HttpContext.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
        context.HttpContext.Response.Headers["Access-Control-Max-Age"] = "3600";
        
        // Handle preflight
        if (context.HttpContext.Request.Method == "OPTIONS")
            return ChainResult.Stop(RouteResult.Ok());
        
        return ChainResult.Next();
    }
}

app.MapRoute("GET", "/api/public", Handler)
    .AddChainNode(new CorsChainNode("https://app.example.com", "https://admin.example.com"));
```

---

## Request/Response Logging

Comprehensive request and response logging.

### Logging Chain Node

```csharp
public class RequestLoggingChainNode : IChainNode
{
    private readonly ILogger<RequestLoggingChainNode> _logger;

    public RequestLoggingChainNode(ILogger<RequestLoggingChainNode> logger)
    {
        _logger = logger;
    }

    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        var request = context.HttpContext.Request;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        _logger.LogInformation("Incoming {Method} {Path}", request.Method, request.Path);
        _logger.LogInformation("Headers: {@Headers}", request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()));
        
        context.HttpContext.Items["RequestStart"] = stopwatch;
        return ChainResult.Next();
    }
}

public class ResponseLoggingWrapper : IRouteWrapper
{
    private readonly ILogger<ResponseLoggingWrapper> _logger;

    public async ValueTask<RouteResult> WrapAsync(RequestContext context, Func<ValueTask<RouteResult>> nextHandler, CancellationToken ct)
    {
        var stopwatch = (System.Diagnostics.Stopwatch)context.HttpContext.Items["RequestStart"]!;
        var result = await nextHandler();
        stopwatch.Stop();
        
        _logger.LogInformation("Response {StatusCode} in {ElapsedMs}ms", result.StatusCode, stopwatch.ElapsedMilliseconds);
        
        return result;
    }
}
```

---

## Circuit Breaker Pattern

Handling service failures gracefully.

```csharp
public class CircuitBreakerChainNode : IChainNode
{
    private readonly Dictionary<string, CircuitState> _states = new();
    private readonly int _failureThreshold = 5;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        var serviceKey = context.Match.Path;
        
        if (!_states.TryGetValue(serviceKey, out var state))
        {
            state = new CircuitState();
            _states[serviceKey] = state;
        }
        
        if (state.IsOpen && DateTime.UtcNow < state.OpenedAt + _timeout)
        {
            _logger.LogWarning("Circuit breaker OPEN for {Service}", serviceKey);
            return ChainResult.Stop(RouteResult.Error(503, "Service temporarily unavailable"));
        }
        
        state.IsOpen = false;
        return ChainResult.Next();
    }

    private class CircuitState
    {
        public int FailureCount { get; set; }
        public bool IsOpen { get; set; }
        public DateTime OpenedAt { get; set; }
    }
}
```

---

## Caching Strategy

Implementing different caching strategies.

### Response Caching

```csharp
public class CachingChainNode : IChainNode
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _duration;

    public CachingChainNode(IMemoryCache cache, TimeSpan? duration = null)
    {
        _cache = cache;
        _duration = duration ?? TimeSpan.FromMinutes(5);
    }

    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        var cacheKey = $"{context.Match.Method}:{context.Match.Path}";
        
        // Only cache GET requests
        if (context.Match.Method != "GET")
            return ChainResult.Next();
        
        if (_cache.TryGetValue(cacheKey, out var cachedResponse))
        {
            _logger.LogInformation("Cache HIT for {CacheKey}", cacheKey);
            return ChainResult.Stop((RouteResult)cachedResponse!);
        }
        
        context.HttpContext.Items["CacheKey"] = cacheKey;
        return ChainResult.Next();
    }
}

public class CachingWrapper : IRouteWrapper
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _duration;

    public async ValueTask<RouteResult> WrapAsync(RequestContext context, Func<ValueTask<RouteResult>> nextHandler, CancellationToken ct)
    {
        var result = await nextHandler();
        
        if (context.HttpContext.Items.TryGetValue("CacheKey", out var cacheKeyObj))
        {
            var cacheKey = cacheKeyObj?.ToString() ?? "";
            _cache.Set(cacheKey, result, _duration);
        }
        
        return result;
    }
}
```

---

## GraphQL Federation

Integrating with GraphQL federation.

```csharp
app.MapRoute("POST", "/graphql", async (ctx, ct) =>
{
    var request = await ctx.HttpContext.Request.ReadAsAsync<GraphQLRequest>(ct);
    var result = await ExecuteGraphQLQuery(request.Query, request.Variables, ct);
    return RouteResult.Json(result);
});

public class GraphQLRequest
{
    public string Query { get; set; } = "";
    public Dictionary<string, object>? Variables { get; set; }
}
```

---

## OpenAPI Integration

Serving OpenAPI/Swagger documentation.

```csharp
app.MapRoute("GET", "/swagger/v1/swagger.json", async (ctx, ct) =>
{
    var swaggerJson = await GenerateOpenAPISpec(ct);
    return RouteResult.Json(swaggerJson);
});

app.MapRoute("GET", "/swagger/index.html", async (ctx, ct) =>
{
    var html = await File.ReadAllTextAsync("swagger-ui.html", ct);
    return RouteResult.Html(html);
});
```

---

## Testing Patterns

Testing routes and chain nodes.

### Unit Testing Chain Nodes

```csharp
[Fact]
public async Task AuthChainNode_WithoutUser_ReturnsForbidden()
{
    // Arrange
    var context = new DefaultHttpContext();
    var chainNode = new AuthChainNode();
    var requestContext = new RequestContext(context, new RouteMatch("GET", "/api", new Dictionary<string, string>()), new Dictionary<string, object>());
    
    // Act
    var result = await chainNode.ExecuteAsync(requestContext, CancellationToken.None);
    
    // Assert
    Assert.True(result.IsTerminal);
    Assert.Equal(401, result.Response.StatusCode);
}
```

### Integration Testing Routes

```csharp
[Fact]
public async Task GetUser_WithValidId_ReturnsUser()
{
    // Arrange
    var router = new TestRouterBuilder().Build();
    var userId = 123;
    
    // Act
    var result = await router.HandleRequestAsync(
        CreateHttpContext("GET", $"/users/{userId}"),
        CancellationToken.None);
    
    // Assert
    Assert.Equal(200, result.StatusCode);
    // Parse JSON and verify user
}

private DefaultHttpContext CreateHttpContext(string method, string path)
{
    var context = new DefaultHttpContext();
    context.Request.Method = method;
    context.Request.Path = path;
    return context;
}
```

---

**End of Advanced Patterns**

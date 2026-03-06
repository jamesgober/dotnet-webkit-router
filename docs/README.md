# JG.WebKit.Router Documentation

Complete documentation for the trie-based HTTP router for ASP.NET Core.

## Documentation Files

### [API.md](API.md) - Complete API Reference
Full API documentation with examples for:
- Quick start setup
- Core concepts and request flow
- Route definition and matching (literals, parameters, wildcards)
- All 11 built-in constraints (int, long, guid, bool, slug, alpha, alphanum, filename, range, length, regex)
- Custom constraints implementation
- Execution chains (IChainNode)
- Route groups and organization
- Route providers (dynamic routes)
- Hot-reload system
- Configuration options
- Advanced examples (REST API, health checks, webhooks)
- Complete API reference for all public types

**Use this when:** You need to understand how to use the router, learn about constraints, or reference the API.

### [ADVANCED.md](ADVANCED.md) - Advanced Patterns
Real-world advanced patterns for:
- API versioning (route-based, header-based, deprecation)
- Multi-tenant routing (subdomain, path-based, header-based)
- Microservice gateway with service discovery and load balancing
- CORS handling
- Request/response logging
- Circuit breaker pattern
- Caching strategies
- GraphQL federation
- OpenAPI integration
- Testing patterns

**Use this when:** You're building complex systems or need advanced patterns.

### [GUIDES.md](GUIDES.md) - Practical Step-by-Step Guides
Complete working examples for:
- Building a full REST API with CRUD operations
- Building a protected admin dashboard API
- Building a webhook service
- Building a file upload service
- Building a search API with filters and pagination
- Building a rate limiting service with tiers

**Use this when:** You want to see complete, working examples of common scenarios.

---

## Quick Navigation

### For Beginners
1. Start with [API.md - Quick Start](#apimmd)
2. Read [API.md - Core Concepts](#apimmd)
3. Try [GUIDES.md - Building a REST API](#guidesmd)

### For Intermediate Users
1. Learn about [API.md - Constraints](#apimmd)
2. Understand [API.md - Execution Chains](#apimmd)
3. Explore [API.md - Route Groups](#apimmd)
4. Check [ADVANCED.md - API Versioning](#advancedmd)

### For Advanced Users
1. Review [ADVANCED.md](#advancedmd) for patterns
2. Reference [API.md - API Reference](#apimmd) for details
3. Use [GUIDES.md](#guidesmd) for working examples
4. Build custom [API.md - Custom Constraints](#apimmd)

---

## Key Concepts

### Routes
Routes map HTTP requests (method + path) to handler functions.

```csharp
app.MapRoute("GET", "/api/users", GetUsersHandler);
```

### Parameters
Routes can have:
- **Literal segments:** `/api/users` - exact match
- **Unconstrained parameters:** `/users/{id}` - any value
- **Constrained parameters:** `/users/{id:int}` - validated values
- **Wildcards:** `/{**path}` - remaining path

### Constraints (11 Built-in)
Validate parameters before execution: `int`, `long`, `guid`, `bool`, `slug`, `alpha`, `alphanum`, `filename`, `range`, `length`, `regex`

### Chain Nodes (IChainNode)
Pre-route filters that can validate, log, or short-circuit. Execute in order.

### Route Groups
Organize routes with shared prefixes and chain nodes.

### Route Providers (IRouteProvider)
Load routes from any source (code, config, database, files) dynamically.

### Hot-Reload
Refresh routes without restarting the application.

---

## Common Tasks

### Create a Basic Route
See [API.md - Basic Route](API.md#basic-route)

### Add Authentication
See [ADVANCED.md - Authorization Chain Node](ADVANCED.md#versioned-apis)

### Implement Rate Limiting
See [GUIDES.md - Rate Limiting Service](GUIDES.md#building-a-rate-limiting-service)

### Build a REST API
See [GUIDES.md - REST API](GUIDES.md#building-a-rest-api)

### Handle Multiple API Versions
See [ADVANCED.md - Versioned APIs](ADVANCED.md#versioned-apis)

### Enable Hot-Reload
See [API.md - Hot-Reload](API.md#hot-reload)

### Load Routes from Database
See [API.md - Route Providers](API.md#route-providers)

### Validate File Uploads
See [GUIDES.md - File Upload Service](GUIDES.md#building-a-file-upload-service)

---

## API Quick Reference

### MapRoute - Define a route
```csharp
app.MapRoute("GET", "/path/{param:constraint}", handler);
```

### MapRouteGroup - Group routes
```csharp
app.MapRouteGroup("/prefix", group =>
{
    group.AddChainNode(sharedNode);
    group.MapRoute("GET", "/endpoint", handler);
});
```

### RouteResult - Return a response
```csharp
RouteResult.Ok()
RouteResult.Json(data)
RouteResult.NotFound()
RouteResult.Unauthorized()
RouteResult.Forbidden()
RouteResult.BadRequest()
RouteResult.Error(statusCode)
```

### ChainResult - Chain node result
```csharp
ChainResult.Next()  // Continue to next node
ChainResult.Stop(result)  // Stop and return response
```

### IChainNode - Custom filter
```csharp
public class MyChainNode : IChainNode
{
    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        // Custom logic
        return ChainResult.Next();
    }
}
```

### IRouteConstraint - Custom validation
```csharp
public class MyConstraint : IRouteConstraint
{
    public bool Match(string parameterName, ReadOnlySpan<char> value)
    {
        // Validation logic
        return true; // or false
    }
}
```

---

## Performance Notes

- **Route Matching:** O(1) average lookup using trie with dictionary
- **Path Parsing:** Zero allocations using ReadOnlySpan<char>
- **Request Path:** Lock-free, no contention
- **Regex Patterns:** Compiled and cached
- **Hot-Reload:** Atomic swap, zero downtime

---

## Thread Safety

- Request handling: No locks (volatile reads)
- Hot-reload: Atomic trie swap via Interlocked.Exchange
- Route providers: Can be called concurrently during reload
- Custom chain nodes: Should be thread-safe for concurrent requests

---

## Support

For issues or questions:
1. Check the relevant documentation file
2. Look for similar examples in guides
3. Review constraint/pattern details in API reference
4. Check advanced patterns for complex scenarios

---

**Documentation generated for JG.WebKit.Router v1.0.0**

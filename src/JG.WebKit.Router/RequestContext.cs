namespace JG.WebKit.Router;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Context passed to chain nodes and route handlers during execution.
/// Immutable readonly struct containing the HTTP context and matched route information.
/// </summary>
public readonly struct RequestContext
{
    /// <summary>
    /// The ASP.NET Core HttpContext for this request.
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <summary>
    /// The matched route information including parameters.
    /// </summary>
    public RouteMatch Match { get; }

    /// <summary>
    /// Route metadata (e.g., cache_ttl, layout, permissions).
    /// </summary>
    public IReadOnlyDictionary<string, object> RouteMetadata { get; }

    /// <summary>
    /// Creates a new RequestContext with the specified HttpContext, route match, and metadata.
    /// </summary>
    public RequestContext(HttpContext httpContext, RouteMatch match, IReadOnlyDictionary<string, object> routeMetadata)
    {
        HttpContext = httpContext;
        Match = match;
        RouteMetadata = routeMetadata ?? new Dictionary<string, object>();
    }
}

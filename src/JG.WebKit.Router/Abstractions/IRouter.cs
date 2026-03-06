namespace JG.WebKit.Router.Abstractions;

using Microsoft.AspNetCore.Http;

/// <summary>
/// The main router interface. Matches requests to routes and executes them.
/// </summary>
public interface IRouter
{
    /// <summary>
    /// Handle an HTTP request and return a RouteResult.
    /// </summary>
    ValueTask<RouteResult> HandleRequestAsync(HttpContext httpContext, CancellationToken cancellationToken);

    /// <summary>
    /// Reload routes from all providers. Atomic trie swap, zero downtime.
    /// </summary>
    ValueTask ReloadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Register a single route (primarily for testing).
    /// </summary>
    ValueTask RegisterRouteAsync(RouteDefinition definition, CancellationToken cancellationToken = default);
}

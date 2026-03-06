namespace JG.WebKit.Router.Providers;

using Microsoft.Extensions.Configuration;
using JG.WebKit.Router.Abstractions;

/// <summary>
/// Provides routes defined in code via MapRoute() calls.
/// </summary>
public class StaticRouteProvider : IRouteProvider
{
    private readonly List<RouteDefinition> _routes = [];

    /// <summary>
    /// Add a static route.
    /// </summary>
    public void AddRoute(RouteDefinition route)
    {
        _routes.Add(route);
    }

    /// <summary>
    /// Returns all routes registered in this provider.
    /// </summary>
    public ValueTask<IReadOnlyList<RouteDefinition>> GetRoutesAsync(CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<IReadOnlyList<RouteDefinition>>(_routes.AsReadOnly());
    }
}

/// <summary>
/// Provides routes loaded from IConfiguration (appsettings.json).
/// Configuration format:
/// {
///   "Routes": [
///     {
///       "Method": "GET",
///       "Path": "/api/users/{id:int}",
///       "Handler": "UserHandler.GetById"
///     }
///   ]
/// }
/// </summary>
public class ConfigurationRouteProvider : IRouteProvider
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Creates a new ConfigurationRouteProvider that reads routes from the specified IConfiguration instance.
    /// </summary>
    /// <param name="configuration">The configuration instance to read routes from.</param>
    public ConfigurationRouteProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Loads and returns all routes from the configuration.
    /// </summary>
    public ValueTask<IReadOnlyList<RouteDefinition>> GetRoutesAsync(CancellationToken cancellationToken)
    {
        var routes = new List<RouteDefinition>();
        var routesSection = _configuration.GetSection("Routes");

        foreach (var routeSection in routesSection.GetChildren())
        {
            var method = routeSection["Method"];
            var path = routeSection["Path"];

            if (string.IsNullOrEmpty(method) || string.IsNullOrEmpty(path))
                continue;

            var route = new RouteDefinition
            {
                Method = method,
                Path = path,
                Handler = (_, _) => ValueTask.FromResult(RouteResult.NotFound())
            };

            routes.Add(route);
        }

        return ValueTask.FromResult<IReadOnlyList<RouteDefinition>>(routes);
    }
}

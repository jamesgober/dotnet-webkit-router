namespace JG.WebKit.Router.Extensions;

using Microsoft.AspNetCore.Builder;
using JG.WebKit.Router.Abstractions;
using JG.WebKit.Router.Providers;

/// <summary>
/// Extension methods for mapping routes fluently.
/// </summary>
public static class RouteMappingExtensions
{
    /// <summary>
    /// Register a route with the static route provider.
    /// </summary>
    public static RouteMappingBuilder MapRoute(
        this IApplicationBuilder app,
        string method,
        string path,
        RouteHandler handler)
    {
        var staticProvider = app.ApplicationServices.GetService(typeof(StaticRouteProvider)) as StaticRouteProvider
            ?? throw new InvalidOperationException("StaticRouteProvider not registered. Call AddWebKitRouter() first.");

        var routeDefinition = new RouteDefinition
        {
            Method = method,
            Path = path,
            Handler = handler
        };

        staticProvider.AddRoute(routeDefinition);
        return new RouteMappingBuilder(routeDefinition);
    }

    /// <summary>
    /// Register a route group with shared prefix and chain nodes.
    /// </summary>
    public static void MapRouteGroup(
        this IApplicationBuilder app,
        string prefix,
        Action<RouteGroupBuilder> configureGroup)
    {
        var staticProvider = app.ApplicationServices.GetService(typeof(StaticRouteProvider)) as StaticRouteProvider
            ?? throw new InvalidOperationException("StaticRouteProvider not registered. Call AddWebKitRouter() first.");

        var groupBuilder = new RouteGroupBuilder(prefix, staticProvider);
        configureGroup(groupBuilder);
    }
}

/// <summary>
/// Fluent builder for configuring individual routes.
/// </summary>
public class RouteMappingBuilder
{
    private readonly RouteDefinition _route;

    /// <summary>
    /// Initializes a new RouteMappingBuilder.
    /// </summary>
    public RouteMappingBuilder(RouteDefinition route)
    {
        _route = route;
    }

    /// <summary>
    /// Add a chain node to this route.
    /// </summary>
    public RouteMappingBuilder AddChainNode(IChainNode node)
    {
        _route.ChainNodes.Add(node);
        return this;
    }

    /// <summary>
    /// Add a chain node of type T to this route.
    /// </summary>
    public RouteMappingBuilder AddChainNode<T>() where T : IChainNode, new()
    {
        _route.ChainNodes.Add(new T());
        return this;
    }

    /// <summary>
    /// Add a wrapper to this route.
    /// </summary>
    public RouteMappingBuilder AddWrapper(IRouteWrapper wrapper)
    {
        _route.Wrappers.Add(wrapper);
        return this;
    }

    /// <summary>
    /// Add a wrapper of type T to this route.
    /// </summary>
    public RouteMappingBuilder AddWrapper<T>() where T : IRouteWrapper, new()
    {
        _route.Wrappers.Add(new T());
        return this;
    }

    /// <summary>
    /// Add metadata to this route.
    /// </summary>
    public RouteMappingBuilder WithMetadata(string key, object value)
    {
        _route.Metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Mark this route as requiring authentication.
    /// </summary>
    public RouteMappingBuilder RequireAuth()
    {
        _route.Metadata["RequireAuth"] = true;
        return this;
    }

    /// <summary>
    /// Mark this route as requiring rate limiting.
    /// </summary>
    public RouteMappingBuilder RequireRateLimit()
    {
        _route.Metadata["RequireRateLimit"] = true;
        return this;
    }
}

/// <summary>
/// Fluent builder for route groups.
/// </summary>
public class RouteGroupBuilder
{
    private readonly string _prefix;
    private readonly StaticRouteProvider _provider;
    private readonly List<IChainNode> _groupChainNodes = [];

    /// <summary>
    /// Initializes a new RouteGroupBuilder.
    /// </summary>
    public RouteGroupBuilder(string prefix, StaticRouteProvider provider)
    {
        _prefix = prefix.TrimEnd('/');
        _provider = provider;
    }

    /// <summary>
    /// Add a chain node to all routes in this group.
    /// </summary>
    public RouteGroupBuilder AddChainNode(IChainNode node)
    {
        _groupChainNodes.Add(node);
        return this;
    }

    /// <summary>
    /// Add a chain node of type T to all routes in this group.
    /// </summary>
    public RouteGroupBuilder AddChainNode<T>() where T : IChainNode, new()
    {
        _groupChainNodes.Add(new T());
        return this;
    }

    /// <summary>
    /// Register a route within this group.
    /// </summary>
    public RouteMappingBuilder MapRoute(string method, string path, RouteHandler handler)
    {
        var fullPath = _prefix + (path.StartsWith('/') ? path : "/" + path);
        var routeDefinition = new RouteDefinition
        {
            Method = method,
            Path = fullPath,
            Handler = handler
        };

        foreach (var node in _groupChainNodes)
        {
            routeDefinition.ChainNodes.Add(node);
        }

        _provider.AddRoute(routeDefinition);
        return new RouteMappingBuilder(routeDefinition);
    }
}

namespace JG.WebKit.Router;

using JG.WebKit.Router.Abstractions;

/// <summary>
/// Defines a single HTTP route with its method, path, handler, and constraints.
/// </summary>
public sealed class RouteDefinition
{
    /// <summary>
    /// HTTP method (GET, POST, PUT, DELETE, etc.).
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    /// Route path template (e.g., "/api/users/{id:int}").
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// The handler to execute when this route matches.
    /// </summary>
    public required RouteHandler Handler { get; init; }

    /// <summary>
    /// Optional route metadata (e.g., cache_ttl, layout, permissions).
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];

    /// <summary>
    /// Optional chain nodes to execute before the handler.
    /// </summary>
    public List<IChainNode> ChainNodes { get; set; } = [];

    /// <summary>
    /// Optional wrappers that provide before/after execution hooks.
    /// </summary>
    public List<IRouteWrapper> Wrappers { get; set; } = [];
}

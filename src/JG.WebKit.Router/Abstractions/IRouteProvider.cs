namespace JG.WebKit.Router.Abstractions;

/// <summary>
/// Provides route definitions from any source (code, database, config, etc.).
/// </summary>
public interface IRouteProvider
{
    /// <summary>
    /// Return all routes from this provider. Called during router initialization and reload.
    /// </summary>
    ValueTask<IReadOnlyList<RouteDefinition>> GetRoutesAsync(CancellationToken cancellationToken);
}

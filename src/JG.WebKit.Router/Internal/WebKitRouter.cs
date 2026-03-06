namespace JG.WebKit.Router.Internal;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using JG.WebKit.Router.Abstractions;

/// <summary>
/// Main router implementation. Matches requests to routes via trie and executes compiled chains.
/// Hot-reload via atomic trie swap with Interlocked.Exchange.
/// </summary>
internal sealed class WebKitRouter : IRouter
{
    private readonly RouterOptions _options;
    private readonly List<IRouteProvider> _providers;
    private readonly Dictionary<string, IRouteConstraint> _constraints;
    private readonly ILogger<WebKitRouter> _logger;
    private volatile TrieNode _trie = new();

    public WebKitRouter(RouterOptions options, List<IRouteProvider> providers, Dictionary<string, IRouteConstraint> constraints, ILogger<WebKitRouter>? logger = null)
    {
        _options = options;
        _providers = providers;
        _constraints = constraints;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<WebKitRouter>.Instance;
    }

    /// <summary>
    /// Initialize the router by loading routes from all providers.
    /// </summary>
    public async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        await ReloadAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Handle an HTTP request: normalize URL, match route, execute chain, return result.
    /// </summary>
    public async ValueTask<RouteResult> HandleRequestAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var request = httpContext.Request;
        string path = request.Path.Value ?? "/";
        string method = request.Method;

        // Normalize path
        path = UrlNormalizer.NormalizePath(path, _options.CaseSensitive, _options.EnableTrailingSlashRedirect);

        // Check for trailing slash redirect
        if (_options.EnableTrailingSlashRedirect)
        {
            string normalized = UrlNormalizer.NormalizePath(path, _options.CaseSensitive, false);
            if (normalized != path)
            {
                string redirectUrl = UrlNormalizer.ToggleTrailingSlash(normalized);
                return RouteResult.Redirect(redirectUrl, 301);
            }
        }

        // Split path into segments (allocation-free)
        var segments = UrlNormalizer.SplitPathSegments(path);

        // Get current trie (volatile read)
        var trie = _trie;

        // Match route in trie
        var builder = new TrieBuilder(_options, _constraints);
        var matchResult = builder.Match(trie, method, segments);

        if (!matchResult.IsMatch)
        {
            return RouteResult.NotFound($"No route found for {method} {path}");
        }

        // Create route match and context
        var routeMatch = new RouteMatch(method, matchResult.MatchedPath, matchResult.Parameters);
        var context = new RequestContext(httpContext, routeMatch, matchResult.Route.Metadata);

        // Execute compiled route
        var result = await matchResult.Route.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Reload routes from all providers. Atomic trie swap, zero downtime.
    /// </summary>
    public async ValueTask ReloadAsync(CancellationToken cancellationToken)
    {
        var allRoutes = new List<RouteDefinition>();

        // Load routes from all providers
        foreach (var provider in _providers)
        {
            var routes = await provider.GetRoutesAsync(cancellationToken).ConfigureAwait(false);
            allRoutes.AddRange(routes);
        }

        // Build new trie
        var builder = new TrieBuilder(_options, _constraints);
        var newTrie = builder.BuildTrie(allRoutes);

        // Atomic swap
        Interlocked.Exchange(ref _trie!, newTrie);
    }

    /// <summary>
    /// Register a single route for testing.
    /// </summary>
    public async ValueTask RegisterRouteAsync(RouteDefinition definition, CancellationToken cancellationToken = default)
    {
        if (_providers.FirstOrDefault() is Providers.StaticRouteProvider staticProvider)
        {
            staticProvider.AddRoute(definition);
            await ReloadAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Get the current trie for testing.
    /// </summary>
    public TrieNode GetCurrentTrie() => _trie;
}

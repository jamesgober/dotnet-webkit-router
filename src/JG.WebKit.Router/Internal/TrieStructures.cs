namespace JG.WebKit.Router.Internal;

using JG.WebKit.Router.Abstractions;

/// <summary>
/// Immutable trie node for route matching.
/// Each node can have literal, constrained param, unconstrained param, and wildcard children.
/// </summary>
internal sealed class TrieNode
{
    /// <summary>
    /// Literal segment children (exact string match, O(1) lookup).
    /// </summary>
    public Dictionary<string, TrieNode> Literals { get; } = [];

    /// <summary>
    /// Constrained parameter children.
    /// (ParamName with constraint key, Constraint key, Actual param name, TrieNode)
    /// </summary>
    public List<(string ParamName, string ConstraintKey, string ActualParamName, TrieNode Node)> ConstrainedParams { get; } = [];

    /// <summary>
    /// Unconstrained parameter child (single, first match wins).
    /// </summary>
    public (string ParamName, TrieNode Node)? UnconstrainedParam { get; set; }

    /// <summary>
    /// Wildcard/catch-all parameter child (matches remaining segments).
    /// </summary>
    public (string ParamName, TrieNode Node)? Wildcard { get; set; }

    /// <summary>
    /// Routes registered at this node, keyed by HTTP method.
    /// </summary>
    public Dictionary<string, CompiledRoute> Routes { get; } = [];
}

/// <summary>
/// Compiled route with pre-built chain nodes and handler.
/// Executed when a route matches.
/// </summary>
internal readonly struct CompiledRoute
{
    public IChainNode[] ChainNodes { get; }
    public IRouteWrapper[] Wrappers { get; }
    public RouteHandler Handler { get; }
    public IReadOnlyDictionary<string, object> Metadata { get; }
    public string Path { get; }

    public CompiledRoute(
        IChainNode[] chainNodes,
        IRouteWrapper[] wrappers,
        RouteHandler handler,
        IReadOnlyDictionary<string, object> metadata,
        string path)
    {
        ChainNodes = chainNodes;
        Wrappers = wrappers;
        Handler = handler;
        Metadata = metadata;
        Path = path;
    }

    /// <summary>
    /// Execute this compiled route with all chain nodes and wrappers.
    /// </summary>
    public async ValueTask<RouteResult> ExecuteAsync(RequestContext ctx, CancellationToken ct)
    {
        // Execute chain nodes sequentially
        for (int i = 0; i < ChainNodes.Length; i++)
        {
            var result = await ChainNodes[i].ExecuteAsync(ctx, ct).ConfigureAwait(false);
            if (!result.Continue)
                return result.Response;
        }

        // Build wrapped handler
        var handler = Handler;
        var wrappers = Wrappers;
        Func<ValueTask<RouteResult>> execute = () => handler(ctx, ct);

        // Apply wrappers in reverse order (so first wrapper is outermost)
        for (int i = wrappers.Length - 1; i >= 0; i--)
        {
            var wrapper = wrappers[i];
            var capturedExecute = execute;
            execute = () => wrapper.WrapAsync(ctx, capturedExecute, ct);
        }

        return await execute().ConfigureAwait(false);
    }
}

/// <summary>
/// Result of route matching.
/// </summary>
internal readonly struct TrieMatchResult
{
    public bool IsMatch { get; }
    public CompiledRoute Route { get; }
    public Dictionary<string, string> Parameters { get; }
    public string MatchedPath { get; }

    private TrieMatchResult(bool isMatch, CompiledRoute route, Dictionary<string, string> parameters, string matchedPath)
    {
        IsMatch = isMatch;
        Route = route;
        Parameters = parameters;
        MatchedPath = matchedPath;
    }

    public static TrieMatchResult Success(CompiledRoute route, Dictionary<string, string> parameters, string matchedPath)
        => new(true, route, parameters, matchedPath);

    public static TrieMatchResult NotFound()
        => new(false, default, new(), "/");
}

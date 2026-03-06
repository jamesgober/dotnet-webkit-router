namespace JG.WebKit.Router.Internal;

using Microsoft.Extensions.Logging;

/// <summary>
/// Source-generated logging messages for the WebKit router.
/// </summary>
internal static partial class RouterLogMessages
{
    [LoggerMessage(EventId = 100, Level = LogLevel.Debug, Message = "Route matched: {Method} {Path}")]
    public static partial void RouteMatched(this ILogger logger, string method, string path);

    [LoggerMessage(EventId = 101, Level = LogLevel.Debug, Message = "No route found: {Method} {Path}")]
    public static partial void RouteNotFound(this ILogger logger, string method, string path);

    [LoggerMessage(EventId = 102, Level = LogLevel.Information, Message = "Route trie built with {RouteCount} routes")]
    public static partial void TrieBuilt(this ILogger logger, int routeCount);

    [LoggerMessage(EventId = 103, Level = LogLevel.Debug, Message = "Chain node short-circuited with status {StatusCode}")]
    public static partial void ChainShortCircuit(this ILogger logger, int statusCode);

    [LoggerMessage(EventId = 104, Level = LogLevel.Debug, Message = "Constraint '{Constraint}' rejected value for parameter '{Parameter}'")]
    public static partial void ConstraintFailed(this ILogger logger, string constraint, string parameter);

    [LoggerMessage(EventId = 105, Level = LogLevel.Warning, Message = "Route conflict detected: {Method} {Path}")]
    public static partial void RouteConflict(this ILogger logger, string method, string path);

    [LoggerMessage(EventId = 106, Level = LogLevel.Error, Message = "Route provider failed: {ProviderType}")]
    public static partial void ProviderError(this ILogger logger, string providerType);

    [LoggerMessage(EventId = 107, Level = LogLevel.Information, Message = "Route trie reloaded with {RouteCount} routes")]
    public static partial void TrieReloaded(this ILogger logger, int routeCount);
}

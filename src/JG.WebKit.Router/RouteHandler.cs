namespace JG.WebKit.Router;

/// <summary>
/// Delegate for executing a route handler and returning a RouteResult.
/// </summary>
public delegate ValueTask<RouteResult> RouteHandler(RequestContext context, CancellationToken cancellationToken);

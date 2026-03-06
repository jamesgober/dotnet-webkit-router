namespace JG.WebKit.Router.Abstractions;

/// <summary>
/// Wraps route execution with before/after hooks. Used sparingly for use cases
/// that genuinely need both before and after logic (timing, error handling, etc.).
/// </summary>
public interface IRouteWrapper
{
    /// <summary>
    /// Wraps the route execution. The <paramref name="nextHandler"/> function executes the actual route.
    /// </summary>
    ValueTask<RouteResult> WrapAsync(RequestContext context, Func<ValueTask<RouteResult>> nextHandler, CancellationToken cancellationToken);
}

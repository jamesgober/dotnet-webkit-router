namespace JG.WebKit.Router;

/// <summary>
/// Result of a chain node execution. Either continue to the next node, or short-circuit with a response.
/// Immutable readonly struct.
/// </summary>
public readonly struct ChainResult
{
    /// <summary>
    /// If true, execution continues to the next chain node or handler.
    /// If false, execution short-circuits and the response is returned.
    /// </summary>
    public bool Continue { get; }

    /// <summary>
    /// The response to return if Continue is false. Null if Continue is true.
    /// </summary>
    public RouteResult Response { get; }

    private ChainResult(bool @continue, RouteResult response)
    {
        Continue = @continue;
        Response = response;
    }

    /// <summary>
    /// Creates a result that continues to the next node.
    /// </summary>
    public static ChainResult Next() => new(true, default);

    /// <summary>
    /// Creates a result that short-circuits with the specified response.
    /// </summary>
    public static ChainResult Stop(RouteResult response) => new(false, response);
}

namespace JG.WebKit.Router;

/// <summary>
/// Represents a matched route with extracted parameters and metadata.
/// </summary>
public readonly struct RouteMatch
{
    /// <summary>
    /// HTTP method (GET, POST, etc.) that matched.
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Route path template that matched.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Extracted route parameters (e.g., {id} values).
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; }

    /// <summary>
    /// Creates a new RouteMatch with the specified method, path, and parameters.
    /// </summary>
    /// <param name="method">The HTTP method that matched.</param>
    /// <param name="path">The route path template that matched.</param>
    /// <param name="parameters">The extracted route parameters.</param>
    public RouteMatch(string method, string path, IReadOnlyDictionary<string, string> parameters)
    {
        Method = method;
        Path = path;
        Parameters = parameters ?? new Dictionary<string, string>();
    }
}

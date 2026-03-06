namespace JG.WebKit.Router;

/// <summary>
/// Represents the result of route matching and execution.
/// Immutable, stack-allocated readonly struct with zero GC pressure.
/// </summary>
public readonly struct RouteResult
{
    /// <summary>
    /// HTTP status code for the response.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Response body. Can be null, string, or any serializable object.
    /// </summary>
    public object? Body { get; }

    /// <summary>
    /// Indicates whether the response represents a successful HTTP status (2xx).
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Optional HTTP response headers.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Headers { get; }

    private RouteResult(int statusCode, object? body, bool isSuccess, IReadOnlyDictionary<string, string>? headers)
    {
        StatusCode = statusCode;
        Body = body;
        IsSuccess = isSuccess;
        Headers = headers;
    }

    /// <summary>
    /// Creates a successful 200 OK response.
    /// </summary>
    public static RouteResult Ok(object? body = null)
        => new(200, body, true, null);

    /// <summary>
    /// Creates a JSON response with the specified body and status code.
    /// </summary>
    public static RouteResult Json(object body, int statusCode = 200)
        => new(statusCode, body, statusCode >= 200 && statusCode < 300, null);

    /// <summary>
    /// Creates an HTML response with the specified HTML content.
    /// </summary>
    public static RouteResult Html(string html)
        => new(200, html, true, new Dictionary<string, string> { { "Content-Type", "text/html; charset=utf-8" } });

    /// <summary>
    /// Creates a redirect response.
    /// </summary>
    public static RouteResult Redirect(string url, int statusCode = 301)
        => new(statusCode, null, false, new Dictionary<string, string> { { "Location", url } });

    /// <summary>
    /// Creates a 404 Not Found response.
    /// </summary>
    public static RouteResult NotFound(string? detail = null)
        => new(404, detail, false, null);

    /// <summary>
    /// Creates a 401 Unauthorized response.
    /// </summary>
    public static RouteResult Unauthorized(string? detail = null)
        => new(401, detail, false, null);

    /// <summary>
    /// Creates a 403 Forbidden response.
    /// </summary>
    public static RouteResult Forbidden(string? detail = null)
        => new(403, detail, false, null);

    /// <summary>
    /// Creates a 429 Too Many Requests response.
    /// </summary>
    public static RouteResult TooManyRequests(string? retryAfter = null)
        => new(429, null, false, retryAfter != null ? new Dictionary<string, string> { { "Retry-After", retryAfter } } : null);

    /// <summary>
    /// Creates a 400 Bad Request response.
    /// </summary>
    public static RouteResult BadRequest(string? detail = null)
        => new(400, detail, false, null);

    /// <summary>
    /// Creates an error response with the specified status code.
    /// </summary>
    public static RouteResult Error(int statusCode, string? detail = null)
        => new(statusCode, detail, false, null);
}

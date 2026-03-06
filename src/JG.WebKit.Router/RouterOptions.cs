namespace JG.WebKit.Router;

/// <summary>
/// Configuration options for the WebKit router.
/// </summary>
public class RouterOptions
{
    /// <summary>
    /// If true, URLs ending with a slash will be redirected to non-slash version (or vice versa).
    /// Default: false (silent normalization).
    /// </summary>
    public bool EnableTrailingSlashRedirect { get; set; }

    /// <summary>
    /// If true, route matching is case-sensitive. Default: false.
    /// </summary>
    public bool CaseSensitive { get; set; }

    /// <summary>
    /// Policy for handling route conflicts (duplicate routes).
    /// </summary>
    public RouteConflictPolicy ConflictPolicy { get; set; } = RouteConflictPolicy.ThrowOnConflict;

    /// <summary>
    /// If true, proxy headers (X-Forwarded-*) are respected for URL reconstruction.
    /// </summary>
    public bool RespectProxyHeaders { get; set; } = true;

    /// <summary>
    /// Maximum number of cached regex patterns per constraint type. Default: 100.
    /// </summary>
    public int MaxCompiledRegexCache { get; set; } = 100;
}

/// <summary>
/// Policy for handling route conflicts.
/// </summary>
public enum RouteConflictPolicy
{
    /// <summary>
    /// Throw an exception when duplicate routes are detected.
    /// </summary>
    ThrowOnConflict,

    /// <summary>
    /// Use the last registered route and silently ignore earlier duplicates.
    /// </summary>
    LastWins
}

namespace JG.WebKit.Router.Internal;

/// <summary>
/// Handles URL normalization: trailing slash, case sensitivity, and path segment extraction.
/// All operations are allocation-free when feasible.
/// </summary>
internal static class UrlNormalizer
{
    /// <summary>
    /// Normalize a path: handle trailing slash and case sensitivity.
    /// Returns the normalized path.
    /// </summary>
    public static string NormalizePath(string path, bool caseSensitive, bool enableTrailingSlashRedirect)
    {
        if (string.IsNullOrEmpty(path))
            path = "/";

        // Ensure path starts with /
        if (!path.StartsWith('/'))
            path = "/" + path;

        // Handle trailing slash
        if (enableTrailingSlashRedirect)
        {
            // Keep as-is for redirect logic
        }
        else
        {
            // Silent normalization: remove trailing slash unless it's the root
            if (path.Length > 1 && path.EndsWith('/'))
                path = path[..^1];
        }

        // DO NOT normalize case here. Matching will use StringComparer.OrdinalIgnoreCase during lookups.
        return path;
    }

    /// <summary>
    /// Split a path into segments using ReadOnlySpan (minimal allocation).
    /// Empty segments are skipped.
    /// </summary>
    public static List<string> SplitPathSegments(ReadOnlySpan<char> path)
    {
        var segments = new List<string>(8);

        if (path.IsEmpty || path.Length == 1)
            return segments;

        // Skip leading /
        int start = path[0] == '/' ? 1 : 0;

        for (int i = start; i < path.Length; i++)
        {
            if (path[i] == '/')
            {
                if (i > start)
                {
                    // PERF: Consider ArrayPool<char> or stackalloc to avoid per-segment string allocation on hot path
                    string segment = path[start..i].ToString();
                    if (!string.IsNullOrEmpty(segment))
                        segments.Add(segment);
                }
                start = i + 1;
            }
        }

        // Add last segment
        if (start < path.Length)
        {
            // PERF: Consider ArrayPool<char> or stackalloc to avoid per-segment string allocation on hot path
            string segment = path[start..].ToString();
            if (!string.IsNullOrEmpty(segment))
                segments.Add(segment);
        }

        return segments;
    }

    /// <summary>
    /// Check if a path has a trailing slash.
    /// </summary>
    public static bool HasTrailingSlash(string path)
        => path.Length > 1 && path.EndsWith('/');

    /// <summary>
    /// Get the URL with trailing slash added or removed.
    /// </summary>
    public static string ToggleTrailingSlash(string path)
    {
        if (path == "/")
            return path;

        return HasTrailingSlash(path) ? path[..^1] : path + "/";
    }

    /// <summary>
    /// Decode a URL-encoded path segment.
    /// </summary>
    public static string DecodeSegment(string segment)
    {
        return Uri.UnescapeDataString(segment);
    }

    /// <summary>
    /// Encode a path segment for URL usage.
    /// </summary>
    public static string EncodeSegment(string segment)
    {
        return Uri.EscapeDataString(segment);
    }
}

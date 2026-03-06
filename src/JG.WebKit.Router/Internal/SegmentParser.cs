namespace JG.WebKit.Router.Internal;

/// <summary>
/// Parses a route path segment and extracts parameter name and constraint.
/// Example: "{id:int}" → name="id", constraint="int"
/// Example: "{name}" → name="name", constraint=null (unconstrained)
/// Example: "{**path}" → name="path", constraint="wildcard"
/// </summary>
internal static class SegmentParser
{
    /// <summary>
    /// Represents a parsed segment.
    /// </summary>
    public readonly struct ParsedSegment
    {
        public string? ParameterName { get; }
        public string? Constraint { get; }
        public bool IsWildcard { get; }
        public bool IsParameter { get; }

        public ParsedSegment(string? parameterName, string? constraint, bool isWildcard, bool isParameter)
        {
            ParameterName = parameterName;
            Constraint = constraint;
            IsWildcard = isWildcard;
            IsParameter = isParameter;
        }
    }

    /// <summary>
    /// Parses a route segment. Returns a ParsedSegment with name and constraint.
    /// </summary>
    public static ParsedSegment Parse(ReadOnlySpan<char> segment)
    {
        // Literal segment (no parameters)
        if (!segment.StartsWith("{"))
            return new ParsedSegment(null, null, false, false);

        if (!segment.EndsWith("}"))
            throw new InvalidOperationException($"Invalid segment: {segment.ToString()}");

        // Remove { and }
        ReadOnlySpan<char> inner = segment[1..^1];

        // Check for wildcard
        if (inner.StartsWith("**"))
        {
            string paramName = inner[2..].ToString();
            return new ParsedSegment(paramName, "wildcard", true, true);
        }

        // Find constraint separator ':'
        int colonIndex = inner.IndexOf(':');

        if (colonIndex == -1)
        {
            // No constraint, just parameter name
            string paramName = inner.ToString();
            return new ParsedSegment(paramName, null, false, true);
        }

        // Extract name and constraint
        string name = inner[..colonIndex].ToString();
        string constraint = inner[(colonIndex + 1)..].ToString();

        return new ParsedSegment(name, constraint, false, true);
    }

    /// <summary>
    /// Check if a segment is optional (ends with '?').
    /// </summary>
    public static bool IsOptional(ReadOnlySpan<char> segment)
    {
        return segment.EndsWith("?");
    }
}

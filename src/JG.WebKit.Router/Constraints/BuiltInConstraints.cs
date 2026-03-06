namespace JG.WebKit.Router.Constraints;

using JG.WebKit.Router.Abstractions;

/// <summary>
/// Validates that a parameter value is a valid 32-bit integer.
/// </summary>
public class IntConstraint : IRouteConstraint
{
    /// <summary>
    /// Validates that the value can be parsed as an integer.
    /// </summary>
    public bool Match(string parameterName, ReadOnlySpan<char> value)
        => int.TryParse(value, out _);
}

/// <summary>
/// Validates that a parameter value is a valid 64-bit integer.
/// </summary>
public class LongConstraint : IRouteConstraint
{
    /// <summary>
    /// Validates that the value can be parsed as a long.
    /// </summary>
    public bool Match(string parameterName, ReadOnlySpan<char> value)
        => long.TryParse(value, out _);
}

/// <summary>
/// Validates that a parameter value is a valid GUID.
/// </summary>
public class GuidConstraint : IRouteConstraint
{
    /// <summary>
    /// Validates that the value can be parsed as a GUID.
    /// </summary>
    public bool Match(string parameterName, ReadOnlySpan<char> value)
        => Guid.TryParse(value, out _);
}

/// <summary>
/// Validates that a parameter value is a valid boolean (true/false).
/// </summary>
public class BoolConstraint : IRouteConstraint
{
    /// <summary>
    /// Validates that the value can be parsed as a boolean.
    /// </summary>
    public bool Match(string parameterName, ReadOnlySpan<char> value)
        => bool.TryParse(value, out _);
}

/// <summary>
/// Validates that a parameter value is URL-friendly: lowercase letters, numbers, and hyphens only.
/// No regex — implemented as a Span loop.
/// </summary>
public class SlugConstraint : IRouteConstraint
{
    /// <summary>
    /// Validates that the value is a valid slug (lowercase letters, numbers, hyphens only).
    /// </summary>
    public bool Match(string parameterName, ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c is not ((>= 'a' and <= 'z') or (>= '0' and <= '9') or '-'))
                return false;
        }

        return true;
    }
}

/// <summary>
/// Validates that a parameter value contains letters only (a-z, A-Z).
/// </summary>
public class AlphaConstraint : IRouteConstraint
{
    /// <summary>
    /// Validates that the value contains only letters.
    /// </summary>
    public bool Match(string parameterName, ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c is not ((>= 'a' and <= 'z') or (>= 'A' and <= 'Z')))
                return false;
        }

        return true;
    }
}

/// <summary>
/// Validates that a parameter value contains letters and numbers only.
/// </summary>
public class AlphanumConstraint : IRouteConstraint
{
    /// <summary>
    /// Validates that the value contains only letters and numbers.
    /// </summary>
    public bool Match(string parameterName, ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c is not ((>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9')))
                return false;
        }

        return true;
    }
}

/// <summary>
/// Validates that a parameter value is a URL-safe filename.
/// Allows letters, numbers, hyphens, underscores, and dots.
/// </summary>
public class FilenameConstraint : IRouteConstraint
{
    /// <summary>
    /// Validates that the value is a valid filename (no leading/trailing dots, safe characters only).
    /// </summary>
    public bool Match(string parameterName, ReadOnlySpan<char> value)
    {
        if (value.IsEmpty || value[0] == '.' || value[^1] == '.')
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c is not ((>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-' or '_' or '.'))
                return false;
        }

        return true;
    }
}

namespace JG.WebKit.Router.Constraints;

using JG.WebKit.Router.Abstractions;

/// <summary>
/// Validates that a parameter value is an integer within a specified range (inclusive).
/// Usage: {page:range(1,100)}
/// </summary>
public class RangeConstraint : IRouteConstraint
{
    private readonly int _min;
    private readonly int _max;

    /// <summary>
    /// Creates a range constraint with the specified minimum and maximum values (inclusive).
    /// </summary>
    public RangeConstraint(int min, int max)
    {
        _min = min;
        _max = max;
    }

    /// <summary>
    /// Validates that the value is an integer within the specified range.
    /// </summary>
    public bool Match(string parameterName, ReadOnlySpan<char> value)
    {
        if (!int.TryParse(value, out int parsed))
            return false;

        return parsed >= _min && parsed <= _max;
    }
}

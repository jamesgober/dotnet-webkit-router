namespace JG.WebKit.Router.Constraints;

using JG.WebKit.Router.Abstractions;

/// <summary>
/// Validates that a parameter value string length is within the specified range (inclusive).
/// Usage: {name:length(2,50)}
/// </summary>
public class LengthConstraint : IRouteConstraint
{
    private readonly int _min;
    private readonly int _max;

    /// <summary>
    /// Creates a length constraint with the specified minimum and maximum string lengths (inclusive).
    /// </summary>
    public LengthConstraint(int min, int max)
    {
        _min = min;
        _max = max;
    }

    /// <summary>
    /// Validates that the value string length is within the specified range.
    /// </summary>
    public bool Match(string parameterName, ReadOnlySpan<char> value)
        => value.Length >= _min && value.Length <= _max;
}

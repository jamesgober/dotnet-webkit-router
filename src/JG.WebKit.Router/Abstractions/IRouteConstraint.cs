namespace JG.WebKit.Router.Abstractions;

/// <summary>
/// Custom constraint for validating route parameters.
/// </summary>
public interface IRouteConstraint
{
    /// <summary>
    /// Validate the parameter value. Return true if it matches the constraint, false otherwise.
    /// </summary>
    bool Match(string parameterName, ReadOnlySpan<char> value);
}

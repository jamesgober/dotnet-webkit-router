namespace JG.WebKit.Router.Constraints;

using System.Text.RegularExpressions;
using JG.WebKit.Router.Abstractions;

/// <summary>
/// Validates a parameter value against a regex pattern.
/// Pattern is compiled once at registration and cached for performance.
/// Usage: {code:regex(^[A-Z]{3}\\d{3}$)}
/// </summary>
public class RegexConstraint : IRouteConstraint
{
    private readonly Regex _regex;

    /// <summary>
    /// Creates a regex constraint with the specified pattern. Pattern is compiled and cached.
    /// </summary>
    public RegexConstraint(string pattern)
    {
        _regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// Validates that the value matches the regex pattern.
    /// </summary>
    public bool Match(string parameterName, ReadOnlySpan<char> value)
        => _regex.IsMatch(value);
}

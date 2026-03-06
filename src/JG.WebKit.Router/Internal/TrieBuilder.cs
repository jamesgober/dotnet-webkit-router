namespace JG.WebKit.Router.Internal;

using JG.WebKit.Router.Abstractions;

/// <summary>
/// Builds and manages the route trie.
/// Thread-safe: hot-reload via atomic trie swap with Interlocked.Exchange.
/// </summary>
internal sealed class TrieBuilder
{
    private readonly RouterOptions _options;
    private readonly Dictionary<string, IRouteConstraint> _constraints;

    public TrieBuilder(RouterOptions options, Dictionary<string, IRouteConstraint> constraints)
    {
        _options = options;
        _constraints = constraints;
    }

    /// <summary>
    /// Build a trie from a list of route definitions.
    /// </summary>
    public TrieNode BuildTrie(IReadOnlyList<RouteDefinition> routes)
    {
        var root = new TrieNode();

        foreach (var routeDefinition in routes)
        {
            InsertRoute(root, routeDefinition);
        }

        return root;
    }

    private void InsertRoute(TrieNode root, RouteDefinition definition)
    {
        var segments = UrlNormalizer.SplitPathSegments(definition.Path);
        var current = root;

        foreach (var segment in segments)
        {
            string normalizedSegment = _options.CaseSensitive ? segment : segment.ToLowerInvariant();

            var parsed = SegmentParser.Parse(normalizedSegment);

            if (!parsed.IsParameter)
            {
                if (!current.Literals.ContainsKey(normalizedSegment))
                    current.Literals[normalizedSegment] = new TrieNode();

                current = current.Literals[normalizedSegment];
            }
            else if (parsed.IsWildcard)
            {
                if (current.Wildcard == null)
                    current.Wildcard = (parsed.ParameterName ?? "path", new TrieNode());

                current = current.Wildcard.Value.Node;
            }
            else if (parsed.Constraint != null)
            {
                // Parse parameterized constraints at build time
                string constraintKey = parsed.Constraint;
                string paramName = parsed.ParameterName ?? "value";

                // Handle parameterized constraints: range(1,100), length(2,50), regex(pattern)
                if (parsed.Constraint.StartsWith("range(", StringComparison.Ordinal) && parsed.Constraint.EndsWith(')'))
                {
                    var paramsStr = parsed.Constraint[6..^1];
                    var parts = paramsStr.Split(',');
                    if (int.TryParse(parts[0].Trim(), out int min) && int.TryParse(parts[1].Trim(), out int max))
                    {
                        constraintKey = $"range_{min}_{max}";
                        if (!_constraints.ContainsKey(constraintKey))
                            _constraints[constraintKey] = new Constraints.RangeConstraint(min, max);
                    }
                    else
                        throw new InvalidOperationException($"Invalid range constraint syntax: {parsed.Constraint}");
                }
                else if (parsed.Constraint.StartsWith("length(", StringComparison.Ordinal) && parsed.Constraint.EndsWith(')'))
                {
                    var paramsStr = parsed.Constraint[7..^1];
                    var parts = paramsStr.Split(',');
                    if (int.TryParse(parts[0].Trim(), out int min) && int.TryParse(parts[1].Trim(), out int max))
                    {
                        constraintKey = $"length_{min}_{max}";
                        if (!_constraints.ContainsKey(constraintKey))
                            _constraints[constraintKey] = new Constraints.LengthConstraint(min, max);
                    }
                    else
                        throw new InvalidOperationException($"Invalid length constraint syntax: {parsed.Constraint}");
                }
                else if (parsed.Constraint.StartsWith("regex(", StringComparison.Ordinal) && parsed.Constraint.EndsWith(')'))
                {
                    var pattern = parsed.Constraint[6..^1];
                    constraintKey = $"regex_{pattern.GetHashCode()}";
                    if (!_constraints.ContainsKey(constraintKey))
                        _constraints[constraintKey] = new Constraints.RegexConstraint(pattern);
                }
                else if (!_constraints.ContainsKey(parsed.Constraint))
                {
                    throw new InvalidOperationException($"Unknown constraint: {parsed.Constraint}");
                }

                var key = $"{paramName}:{constraintKey}";
                var existing = current.ConstrainedParams.FirstOrDefault(x => x.ParamName == key);

                if (existing == default)
                {
                    var node = new TrieNode();
                    current.ConstrainedParams.Add((key, constraintKey, paramName, node));
                    current = node;
                }
                else
                {
                    current = existing.Node;
                }
            }
            else
            {
                if (current.UnconstrainedParam == null)
                    current.UnconstrainedParam = (parsed.ParameterName ?? "value", new TrieNode());

                current = current.UnconstrainedParam.Value.Node;
            }
        }

        // Register route at leaf
        var method = definition.Method.ToUpperInvariant();

        if (current.Routes.ContainsKey(method))
        {
            if (_options.ConflictPolicy == RouteConflictPolicy.ThrowOnConflict)
                throw new InvalidOperationException($"Duplicate route: {definition.Method} {definition.Path}");
            // Otherwise, last-wins: overwrite
        }

        // Compile the route
        var chainNodes = definition.ChainNodes?.ToArray() ?? [];
        var wrappers = definition.Wrappers?.ToArray() ?? [];

        var compiledRoute = new CompiledRoute(
            chainNodes,
            wrappers,
            definition.Handler,
            definition.Metadata,
            definition.Path);

        current.Routes[method] = compiledRoute;
    }

    /// <summary>
    /// Match a request against the trie and extract parameters.
    /// </summary>
    public TrieMatchResult Match(TrieNode root, string method, IReadOnlyList<string> segments)
    {
        var parameters = new Dictionary<string, string>();
        var current = root;
        string matchedPath = "/";

        int segmentIndex = 0;

        while (segmentIndex < segments.Count)
        {
            string segment = segments[segmentIndex];
            string normalizedSegment = _options.CaseSensitive ? segment : segment.ToLowerInvariant();

            if (current.Literals.TryGetValue(normalizedSegment, out var literalNode))
            {
                matchedPath += normalizedSegment + "/";
                current = literalNode;
                segmentIndex++;
                continue;
            }

            bool matched = false;

            foreach (var (paramKey, constraintKey, paramName, paramNode) in current.ConstrainedParams)
            {
                if (_constraints.TryGetValue(constraintKey, out var constraint) && constraint.Match(paramName, segment.AsSpan()))
                {
                    parameters[paramName] = segment;
                    matchedPath += segment + "/";
                    current = paramNode;
                    segmentIndex++;
                    matched = true;
                    break;
                }
            }

            if (matched)
                continue;

            if (current.UnconstrainedParam.HasValue)
            {
                var (paramName, paramNode) = current.UnconstrainedParam.Value;
                parameters[paramName] = segment;
                matchedPath += segment + "/";
                current = paramNode;
                segmentIndex++;
                continue;
            }

            if (current.Wildcard.HasValue)
            {
                var (paramName, wildcardNode) = current.Wildcard.Value;
                var remainingSegments = string.Join("/", segments.Skip(segmentIndex));
                parameters[paramName] = remainingSegments;
                current = wildcardNode;
                segmentIndex = segments.Count;
                continue;
            }

            return TrieMatchResult.NotFound();
        }

        string normalizedMethod = method.ToUpperInvariant();

        if (current.Routes.TryGetValue(normalizedMethod, out var route))
        {
            return TrieMatchResult.Success(route, parameters, matchedPath.TrimEnd('/') ?? "/");
        }

        return TrieMatchResult.NotFound();
    }
}

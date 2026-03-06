namespace JG.WebKit.Router.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using JG.WebKit.Router.Abstractions;
using JG.WebKit.Router.Internal;
using JG.WebKit.Router.Providers;

/// <summary>
/// Service collection extension for registering the WebKit router.
/// </summary>
public static class WebKitRouterServiceCollectionExtensions
{
    // Shared registry for custom constraints
    private static readonly object _constraintLock = new();
    private static readonly Dictionary<string, Action<Dictionary<string, IRouteConstraint>>> _customConstraintActions = [];

    /// <summary>
    /// Register the WebKit router with the dependency injection container.
    /// </summary>
    public static IServiceCollection AddWebKitRouter(this IServiceCollection services, Action<RouterOptions>? configureOptions = null)
    {
        var options = new RouterOptions();
        configureOptions?.Invoke(options);

        services.AddSingleton(options);
        
        services.AddSingleton<Dictionary<string, IRouteConstraint>>(sp =>
        {
            var constraints = new Dictionary<string, IRouteConstraint>
            {
                { "int", new Constraints.IntConstraint() },
                { "long", new Constraints.LongConstraint() },
                { "guid", new Constraints.GuidConstraint() },
                { "bool", new Constraints.BoolConstraint() },
                { "slug", new Constraints.SlugConstraint() },
                { "alpha", new Constraints.AlphaConstraint() },
                { "alphanum", new Constraints.AlphanumConstraint() },
                { "filename", new Constraints.FilenameConstraint() }
            };

            // Apply any custom constraints registered via AddRouteConstraint
            lock (_constraintLock)
            {
                foreach (var action in _customConstraintActions.Values)
                {
                    action(constraints);
                }
            }

            return constraints;
        });

        services.AddSingleton<IRouter>(sp =>
        {
            var opts = sp.GetRequiredService<RouterOptions>();
            var providers = sp.GetServices<IRouteProvider>().ToList();
            var constraints = sp.GetRequiredService<Dictionary<string, IRouteConstraint>>();

            var router = new WebKitRouter(opts, providers, constraints);
            return router;
        });

        services.AddRouteProvider<StaticRouteProvider>();

        return services;
    }

    /// <summary>
    /// Register a route provider.
    /// </summary>
    public static IServiceCollection AddRouteProvider<T>(this IServiceCollection services) where T : class, IRouteProvider
    {
        services.AddSingleton<IRouteProvider, T>();
        return services;
    }

    /// <summary>
    /// Register a custom route constraint. Call this before AddWebKitRouter.
    /// </summary>
    public static IServiceCollection AddRouteConstraint<T>(this IServiceCollection services, string name) where T : class, IRouteConstraint, new()
    {
        lock (_constraintLock)
        {
            _customConstraintActions[name] = constraints =>
            {
                constraints[name] = new T();
            };
        }

        return services;
    }
}

namespace JG.WebKit.Router.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using JG.WebKit.Router.Abstractions;
using JG.WebKit.Router.Internal;

/// <summary>
/// Extension methods for registering the WebKit router middleware.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Use the WebKit router middleware.
    /// </summary>
    public static IApplicationBuilder UseWebKitRouter(this IApplicationBuilder app)
    {
        var router = app.ApplicationServices.GetRequiredService<IRouter>();
        
        async Task Middleware(HttpContext httpContext, RequestDelegate next)
        {
            var result = await router.HandleRequestAsync(httpContext, httpContext.RequestAborted).ConfigureAwait(false);

            if (result.Headers != null)
            {
                foreach (var header in result.Headers)
                {
                    httpContext.Response.Headers[header.Key] = header.Value;
                }
            }

            httpContext.Response.StatusCode = result.StatusCode;

            if (result.Body != null)
            {
                if (result.Body is string stringBody)
                {
                    await httpContext.Response.WriteAsync(stringBody, httpContext.RequestAborted).ConfigureAwait(false);
                }
                else
                {
                    var json = JsonSerializer.Serialize(result.Body);
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsync(json, httpContext.RequestAborted).ConfigureAwait(false);
                }
            }
        }
        
        app.Use(Middleware);

        // Register router initialization on application startup
        var appLifetime = app.ApplicationServices.GetService(typeof(IHostApplicationLifetime)) as IHostApplicationLifetime;
        if (appLifetime == null)
        {
            throw new InvalidOperationException("IHostApplicationLifetime not available. UseWebKitRouter must be called in a full ASP.NET Core app context.");
        }

        appLifetime.ApplicationStarted.Register(async () =>
        {
            if (router is WebKitRouter webKitRouter)
            {
                // Let InitializeAsync exceptions propagate so startup fails if routes can't load
                await webKitRouter.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
            }
        });

        return app;
    }
}

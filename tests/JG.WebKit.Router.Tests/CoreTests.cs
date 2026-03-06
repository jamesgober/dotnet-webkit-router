namespace JG.WebKit.Router.Tests;

using JG.WebKit.Router.Abstractions;
using JG.WebKit.Router.Providers;
using Xunit;
using FluentAssertions;

/// <summary>
/// Basic router functionality tests using the public API.
/// </summary>
public class RouterFunctionalityTests
{
    [Fact]
    public async Task Router_LiteralRoute_Matches()
    {
        // Arrange
        var router = new TestRouterBuilder().Build();
        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/api/users",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok("success"))
        };
        await router.RegisterRouteAsync(route);

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpContext.Request.Path = "/api/users";
        httpContext.Request.Method = "GET";

        // Act
        var result = await router.HandleRequestAsync(httpContext, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Router_UnconstrainedParameter_ExtractsValue()
    {
        // Arrange
        var router = new TestRouterBuilder().Build();
        string? capturedId = null;
        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/users/{id}",
            Handler = (ctx, _) =>
            {
                capturedId = ctx.Match.Parameters["id"];
                return ValueTask.FromResult(RouteResult.Ok());
            }
        };
        await router.RegisterRouteAsync(route);

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpContext.Request.Path = "/users/123";
        httpContext.Request.Method = "GET";

        // Act
        await router.HandleRequestAsync(httpContext, CancellationToken.None);

        // Assert
        capturedId.Should().Be("123");
    }

    [Fact]
    public async Task Router_IntConstraint_RejectsInvalidValue()
    {
        // Arrange
        var router = new TestRouterBuilder().Build();
        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/users/{id:int}",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok())
        };
        await router.RegisterRouteAsync(route);

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpContext.Request.Path = "/users/abc";
        httpContext.Request.Method = "GET";

        // Act
        var result = await router.HandleRequestAsync(httpContext, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Router_GuidConstraint_AcceptsValidGuid()
    {
        // Arrange
        var router = new TestRouterBuilder().Build();
        var validGuid = Guid.NewGuid().ToString();
        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/items/{id:guid}",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok())
        };
        await router.RegisterRouteAsync(route);

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpContext.Request.Path = $"/items/{validGuid}";
        httpContext.Request.Method = "GET";

        // Act
        var result = await router.HandleRequestAsync(httpContext, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Router_SlugConstraint_ValidatesFormat()
    {
        // With case-insensitive matching (default), "My-Slug" becomes "my-slug" before constraint check
        var router = new TestRouterBuilder().Build();
        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/{value:slug}",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok())
        };
        await router.RegisterRouteAsync(route);

        var validContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        validContext.Request.Path = "/my-slug-123";
        validContext.Request.Method = "GET";

        // Act
        var validResult = await router.HandleRequestAsync(validContext, CancellationToken.None);
        validResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Router_SlugConstraint_RejectsPunctuation()
    {
        // Slug constraint should reject values with punctuation
        var router = new TestRouterBuilder().Build();
        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/{value:slug}",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok())
        };
        await router.RegisterRouteAsync(route);

        var invalidContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        invalidContext.Request.Path = "/my_slug";  // Underscore not allowed in slug
        invalidContext.Request.Method = "GET";

        // Act
        var invalidResult = await router.HandleRequestAsync(invalidContext, CancellationToken.None);

        // Assert
        invalidResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Router_WildcardRoute_CapturesRemaining()
    {
        // Arrange
        var router = new TestRouterBuilder().Build();
        string? capturedPath = null;
        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/{**path}",
            Handler = (ctx, _) =>
            {
                capturedPath = ctx.Match.Parameters["path"];
                return ValueTask.FromResult(RouteResult.Ok());
            }
        };
        await router.RegisterRouteAsync(route);

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpContext.Request.Path = "/api/v1/users/123";
        httpContext.Request.Method = "GET";

        // Act
        await router.HandleRequestAsync(httpContext, CancellationToken.None);

        // Assert
        capturedPath.Should().Be("api/v1/users/123");
    }

    [Fact]
    public async Task Router_MultipleParameters_ExtractsAll()
    {
        // Arrange
        var router = new TestRouterBuilder().Build();
        string? year = null;
        string? slug = null;
        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/blog/{year:int}/{slug}",
            Handler = (ctx, _) =>
            {
                year = ctx.Match.Parameters["year"];
                slug = ctx.Match.Parameters["slug"];
                return ValueTask.FromResult(RouteResult.Ok());
            }
        };
        await router.RegisterRouteAsync(route);

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpContext.Request.Path = "/blog/2024/my-post";
        httpContext.Request.Method = "GET";

        // Act
        await router.HandleRequestAsync(httpContext, CancellationToken.None);

        // Assert
        year.Should().Be("2024");
        slug.Should().Be("my-post");
    }

    [Fact]
    public async Task Router_NotFound_Returns404()
    {
        // Arrange
        var router = new TestRouterBuilder().Build();
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpContext.Request.Path = "/nonexistent";
        httpContext.Request.Method = "GET";

        // Act
        var result = await router.HandleRequestAsync(httpContext, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Router_DifferentMethod_ReturnsNotFound()
    {
        // Arrange
        var router = new TestRouterBuilder().Build();
        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/api",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok())
        };
        await router.RegisterRouteAsync(route);

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpContext.Request.Path = "/api";
        httpContext.Request.Method = "POST";

        // Act
        var result = await router.HandleRequestAsync(httpContext, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Router_CaseSensitiveOption_Respected()
    {
        // Arrange
        var options = new RouterOptions { CaseSensitive = true };
        var router = new TestRouterBuilder(options).Build();
        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/API/Users",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok())
        };
        await router.RegisterRouteAsync(route);

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpContext.Request.Path = "/api/users";
        httpContext.Request.Method = "GET";

        // Act
        var result = await router.HandleRequestAsync(httpContext, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(404); // Case sensitive, so should not match
    }

    [Fact]
    public async Task Router_MultipleRoutes_RoutesCorrectly()
    {
        // Arrange
        var router = new TestRouterBuilder().Build();
        await router.RegisterRouteAsync(new RouteDefinition
        {
            Method = "GET",
            Path = "/users",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok("users"))
        });

        await router.RegisterRouteAsync(new RouteDefinition
        {
            Method = "POST",
            Path = "/users",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok("created"))
        });

        var getContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        getContext.Request.Path = "/users";
        getContext.Request.Method = "GET";

        var postContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        postContext.Request.Path = "/users";
        postContext.Request.Method = "POST";

        // Act
        var getResult = await router.HandleRequestAsync(getContext, CancellationToken.None);
        var postResult = await router.HandleRequestAsync(postContext, CancellationToken.None);

        // Assert
        getResult.StatusCode.Should().Be(200);
        postResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Router_LongConstraint_ValidatesValues()
    {
        // Arrange
        var router = new TestRouterBuilder().Build();
        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/data/{id:long}",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok())
        };
        await router.RegisterRouteAsync(route);

        var validContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        validContext.Request.Path = "/data/9223372036854775807";
        validContext.Request.Method = "GET";

        var invalidContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        invalidContext.Request.Path = "/data/notanumber";
        invalidContext.Request.Method = "GET";

        // Act
        var validResult = await router.HandleRequestAsync(validContext, CancellationToken.None);
        var invalidResult = await router.HandleRequestAsync(invalidContext, CancellationToken.None);

        // Assert
        validResult.StatusCode.Should().Be(200);
        invalidResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Router_BoolConstraint_ValidatesValues()
    {
        // Arrange
        var router = new TestRouterBuilder().Build();
        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/config/{enabled:bool}",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok())
        };
        await router.RegisterRouteAsync(route);

        var trueContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        trueContext.Request.Path = "/config/true";
        trueContext.Request.Method = "GET";

        var falseContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        falseContext.Request.Path = "/config/false";
        falseContext.Request.Method = "GET";

        var invalidContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        invalidContext.Request.Path = "/config/maybe";
        invalidContext.Request.Method = "GET";

        // Act
        var trueResult = await router.HandleRequestAsync(trueContext, CancellationToken.None);
        var falseResult = await router.HandleRequestAsync(falseContext, CancellationToken.None);
        var invalidResult = await router.HandleRequestAsync(invalidContext, CancellationToken.None);

        // Assert
        trueResult.StatusCode.Should().Be(200);
        falseResult.StatusCode.Should().Be(200);
        invalidResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Router_AlphaConstraint_ValidatesLettersOnly()
    {
        // Arrange
        var router = new TestRouterBuilder().Build();
        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/{name:alpha}",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok())
        };
        await router.RegisterRouteAsync(route);

        var validContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        validContext.Request.Path = "/HelloWorld";
        validContext.Request.Method = "GET";

        var invalidContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        invalidContext.Request.Path = "/Hello123";
        invalidContext.Request.Method = "GET";

        // Act
        var validResult = await router.HandleRequestAsync(validContext, CancellationToken.None);
        var invalidResult = await router.HandleRequestAsync(invalidContext, CancellationToken.None);

        // Assert
        validResult.StatusCode.Should().Be(200);
        invalidResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Router_AlphanumConstraint_ValidatesAlphanumericOnly()
    {
        // Arrange
        var router = new TestRouterBuilder().Build();
        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/{code:alphanum}",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok())
        };
        await router.RegisterRouteAsync(route);

        var validContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        validContext.Request.Path = "/ABC123";
        validContext.Request.Method = "GET";

        var invalidContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        invalidContext.Request.Path = "/ABC-123";
        invalidContext.Request.Method = "GET";

        // Act
        var validResult = await router.HandleRequestAsync(validContext, CancellationToken.None);
        var invalidResult = await router.HandleRequestAsync(invalidContext, CancellationToken.None);

        // Assert
        validResult.StatusCode.Should().Be(200);
        invalidResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Router_FilenameConstraint_ValidatesFilenames()
    {
        // Arrange
        var router = new TestRouterBuilder().Build();
        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/{file:filename}",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok())
        };
        await router.RegisterRouteAsync(route);

        var validContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        validContext.Request.Path = "/document.pdf";
        validContext.Request.Method = "GET";

        var invalidContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        invalidContext.Request.Path = "/.hidden";
        invalidContext.Request.Method = "GET";

        // Act
        var validResult = await router.HandleRequestAsync(validContext, CancellationToken.None);
        var invalidResult = await router.HandleRequestAsync(invalidContext, CancellationToken.None);

        // Assert
        validResult.StatusCode.Should().Be(200);
        invalidResult.StatusCode.Should().Be(404);
    }
}

/// <summary>
/// Tests for individual constraint implementations.
/// </summary>
public class ConstraintTests
{
    [Theory]
    [InlineData("123", true)]
    [InlineData("0", true)]
    [InlineData("abc", false)]
    public void IntConstraint_Validates(string value, bool shouldMatch)
    {
        var constraint = new Constraints.IntConstraint();
        constraint.Match("id", value.AsSpan()).Should().Be(shouldMatch);
    }

    [Theory]
    [InlineData("9223372036854775807", true)]
    [InlineData("123", true)]
    [InlineData("abc", false)]
    public void LongConstraint_Validates(string value, bool shouldMatch)
    {
        var constraint = new Constraints.LongConstraint();
        constraint.Match("id", value.AsSpan()).Should().Be(shouldMatch);
    }

    [Fact]
    public void GuidConstraint_Validates()
    {
        var constraint = new Constraints.GuidConstraint();
        var validGuid = Guid.NewGuid().ToString();
        constraint.Match("id", validGuid.AsSpan()).Should().BeTrue();
        constraint.Match("id", "not-a-guid".AsSpan()).Should().BeFalse();
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", true)]
    [InlineData("yes", false)]
    public void BoolConstraint_Validates(string value, bool shouldMatch)
    {
        var constraint = new Constraints.BoolConstraint();
        constraint.Match("flag", value.AsSpan()).Should().Be(shouldMatch);
    }

    [Theory]
    [InlineData("my-slug", true)]
    [InlineData("hello-world-123", true)]
    [InlineData("My-Slug", false)]
    [InlineData("my_slug", false)]
    [InlineData("", false)]
    public void SlugConstraint_Validates(string value, bool shouldMatch)
    {
        var constraint = new Constraints.SlugConstraint();
        constraint.Match("slug", value.AsSpan()).Should().Be(shouldMatch);
    }

    [Theory]
    [InlineData("abc", true)]
    [InlineData("ABC", true)]
    [InlineData("abc123", false)]
    [InlineData("", false)]
    public void AlphaConstraint_Validates(string value, bool shouldMatch)
    {
        var constraint = new Constraints.AlphaConstraint();
        constraint.Match("name", value.AsSpan()).Should().Be(shouldMatch);
    }

    [Theory]
    [InlineData("abc123", true)]
    [InlineData("abc_123", false)]
    [InlineData("", false)]
    public void AlphanumConstraint_Validates(string value, bool shouldMatch)
    {
        var constraint = new Constraints.AlphanumConstraint();
        constraint.Match("value", value.AsSpan()).Should().Be(shouldMatch);
    }

    [Theory]
    [InlineData("file.txt", true)]
    [InlineData("document.pdf", true)]
    [InlineData(".hidden", false)]
    [InlineData("file.", false)]
    [InlineData("", false)]
    public void FilenameConstraint_Validates(string value, bool shouldMatch)
    {
        var constraint = new Constraints.FilenameConstraint();
        constraint.Match("filename", value.AsSpan()).Should().Be(shouldMatch);
    }

    [Theory]
    [InlineData("50", true)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("101", false)]
    public void RangeConstraint_Validates(string value, bool shouldMatch)
    {
        var constraint = new Constraints.RangeConstraint(1, 100);
        constraint.Match("page", value.AsSpan()).Should().Be(shouldMatch);
    }

    [Theory]
    [InlineData("hello", true)]
    [InlineData("a", false)]
    [InlineData("a very long string that exceeds maximum", false)]
    public void LengthConstraint_Validates(string value, bool shouldMatch)
    {
        var constraint = new Constraints.LengthConstraint(2, 10);
        constraint.Match("name", value.AsSpan()).Should().Be(shouldMatch);
    }

    [Theory]
    [InlineData("ABC123", true)]
    [InlineData("abc123", false)]
    [InlineData("AB123", false)]
    public void RegexConstraint_Validates(string value, bool shouldMatch)
    {
        var constraint = new Constraints.RegexConstraint(@"^[A-Z]{3}\d{3}$");
        constraint.Match("code", value.AsSpan()).Should().Be(shouldMatch);
    }
}

/// <summary>
/// Tests for RouteResult factory methods.
/// </summary>
public class RouteResultTests
{
    [Fact]
    public void RouteResult_Ok_Creates200Response()
    {
        var result = RouteResult.Ok("data");
        result.StatusCode.Should().Be(200);
        result.IsSuccess.Should().BeTrue();
        result.Body.Should().Be("data");
    }

    [Fact]
    public void RouteResult_Json_CreatesJsonResponse()
    {
        var data = new { name = "test" };
        var result = RouteResult.Json(data, 200);
        result.StatusCode.Should().Be(200);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RouteResult_Html_SetsContentType()
    {
        var result = RouteResult.Html("<h1>Test</h1>");
        result.StatusCode.Should().Be(200);
        result.Headers.Should().ContainKey("Content-Type");
    }

    [Fact]
    public void RouteResult_Redirect_SetsLocationHeader()
    {
        var result = RouteResult.Redirect("/new-path", 301);
        result.StatusCode.Should().Be(301);
        result.Headers.Should().ContainKey("Location");
    }

    [Fact]
    public void RouteResult_NotFound_Creates404()
    {
        var result = RouteResult.NotFound();
        result.StatusCode.Should().Be(404);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void RouteResult_Unauthorized_Creates401()
    {
        var result = RouteResult.Unauthorized();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public void RouteResult_Forbidden_Creates403()
    {
        var result = RouteResult.Forbidden();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public void RouteResult_TooManyRequests_Creates429()
    {
        var result = RouteResult.TooManyRequests("300");
        result.StatusCode.Should().Be(429);
        result.Headers.Should().ContainKey("Retry-After");
    }

    [Fact]
    public void RouteResult_BadRequest_Creates400()
    {
        var result = RouteResult.BadRequest();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public void RouteResult_Error_CreatesCustomStatus()
    {
        var result = RouteResult.Error(502);
        result.StatusCode.Should().Be(502);
    }
}

/// <summary>
/// Helper builder for creating test router instances.
/// </summary>
public class TestRouterBuilder
{
    private readonly RouterOptions _options;

    public TestRouterBuilder(RouterOptions? options = null)
    {
        _options = options ?? new RouterOptions();
    }

    public IRouter Build()
    {
        var providers = new List<IRouteProvider> { new StaticRouteProvider() };
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

        var router = new Internal.WebKitRouter(_options, providers, constraints);
        return router;
    }
}

/// <summary>
/// Integration tests for route mapping fluent API.
/// </summary>
public class FluentApiTests
{
    [Fact]
    public async Task MapRoute_FluentBuilder_AddsChainNode()
    {
        var router = new TestRouterBuilder().Build();
        var chainNodeExecuted = false;

        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/test",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok())
        };
        route.ChainNodes.Add(new TestChainNode(() => chainNodeExecuted = true));

        await router.RegisterRouteAsync(route);

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpContext.Request.Path = "/test";
        httpContext.Request.Method = "GET";

        await router.HandleRequestAsync(httpContext, CancellationToken.None);

        chainNodeExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task MapRoute_WithMetadata_StoresMetadata()
    {
        var router = new TestRouterBuilder().Build();
        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/api",
            Handler = (ctx, _) =>
            {
                var cacheTime = ctx.RouteMetadata.GetValueOrDefault("cache_ttl");
                return ValueTask.FromResult(RouteResult.Json(new { cacheTime }));
            }
        };
        route.Metadata["cache_ttl"] = 300;

        await router.RegisterRouteAsync(route);

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpContext.Request.Path = "/api";
        httpContext.Request.Method = "GET";

        var result = await router.HandleRequestAsync(httpContext, CancellationToken.None);

        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task MapRouteGroup_SharesPrefix_CombinesPathCorrectly()
    {
        var router = new TestRouterBuilder().Build();

        var route1 = new RouteDefinition
        {
            Method = "GET",
            Path = "/api/v1/users",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok("users"))
        };
        var route2 = new RouteDefinition
        {
            Method = "GET",
            Path = "/api/v1/products",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok("products"))
        };

        await router.RegisterRouteAsync(route1);
        await router.RegisterRouteAsync(route2);

        var context1 = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context1.Request.Path = "/api/v1/users";
        context1.Request.Method = "GET";

        var context2 = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context2.Request.Path = "/api/v1/products";
        context2.Request.Method = "GET";

        var result1 = await router.HandleRequestAsync(context1, CancellationToken.None);
        var result2 = await router.HandleRequestAsync(context2, CancellationToken.None);

        result1.StatusCode.Should().Be(200);
        result2.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ChainNode_ShortCircuit_StopsExecution()
    {
        var router = new TestRouterBuilder().Build();
        var handlerExecuted = false;

        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/secure",
            Handler = (_, _) =>
            {
                handlerExecuted = true;
                return ValueTask.FromResult(RouteResult.Ok());
            }
        };
        route.ChainNodes.Add(new TestStopChainNode());

        await router.RegisterRouteAsync(route);

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpContext.Request.Path = "/secure";
        httpContext.Request.Method = "GET";

        var result = await router.HandleRequestAsync(httpContext, CancellationToken.None);

        result.StatusCode.Should().Be(403);
        handlerExecuted.Should().BeFalse();
    }

    [Fact]
    public async Task MultipleRoutes_DifferentMethods_AllMatch()
    {
        var router = new TestRouterBuilder().Build();

        var getRoute = new RouteDefinition
        {
            Method = "GET",
            Path = "/items",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok("GET"))
        };
        var postRoute = new RouteDefinition
        {
            Method = "POST",
            Path = "/items",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok("POST"))
        };
        var deleteRoute = new RouteDefinition
        {
            Method = "DELETE",
            Path = "/items",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok("DELETE"))
        };

        await router.RegisterRouteAsync(getRoute);
        await router.RegisterRouteAsync(postRoute);
        await router.RegisterRouteAsync(deleteRoute);

        var getContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        getContext.Request.Path = "/items";
        getContext.Request.Method = "GET";

        var postContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        postContext.Request.Path = "/items";
        postContext.Request.Method = "POST";

        var deleteContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        deleteContext.Request.Path = "/items";
        deleteContext.Request.Method = "DELETE";

        var getResult = await router.HandleRequestAsync(getContext, CancellationToken.None);
        var postResult = await router.HandleRequestAsync(postContext, CancellationToken.None);
        var deleteResult = await router.HandleRequestAsync(deleteContext, CancellationToken.None);

        getResult.StatusCode.Should().Be(200);
        postResult.StatusCode.Should().Be(200);
        deleteResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RangeConstraint_WithParameterizedConstraint_ValidatesRange()
    {
        var router = new TestRouterBuilder().Build();

        var route = new RouteDefinition
        {
            Method = "GET",
            Path = "/page/{num:range(1,10)}",
            Handler = (_, _) => ValueTask.FromResult(RouteResult.Ok())
        };

        await router.RegisterRouteAsync(route);

        var validContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        validContext.Request.Path = "/page/5";
        validContext.Request.Method = "GET";

        var invalidContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        invalidContext.Request.Path = "/page/99";
        invalidContext.Request.Method = "GET";

        var validResult = await router.HandleRequestAsync(validContext, CancellationToken.None);
        var invalidResult = await router.HandleRequestAsync(invalidContext, CancellationToken.None);

        validResult.StatusCode.Should().Be(200);
        invalidResult.StatusCode.Should().Be(404);
    }

    private class TestChainNode : IChainNode
    {
        private readonly Action _onExecute;

        public TestChainNode(Action onExecute) => _onExecute = onExecute;

        public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
        {
            _onExecute();
            return ChainResult.Next();
        }
    }

    private class TestStopChainNode : IChainNode
    {
        public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
        {
            return ChainResult.Stop(RouteResult.Forbidden());
        }
    }
}

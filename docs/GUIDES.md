# Practical Guides

Step-by-step guides for common scenarios.

## Building a REST API

Complete REST API with full CRUD operations.

```csharp
// Data model
public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string AuthorId { get; set; } = "";
}

// Repository
public class ArticleRepository
{
    private readonly List<Article> _articles = new();

    public Article? GetById(int id) => _articles.FirstOrDefault(a => a.Id == id);
    public List<Article> GetAll() => _articles;
    public Article Create(Article article)
    {
        article.Id = _articles.Any() ? _articles.Max(a => a.Id) + 1 : 1;
        article.CreatedAt = DateTime.UtcNow;
        _articles.Add(article);
        return article;
    }
    public bool Update(int id, Article article)
    {
        var existing = GetById(id);
        if (existing == null) return false;
        existing.Title = article.Title;
        existing.Content = article.Content;
        return true;
    }
    public bool Delete(int id)
    {
        var article = GetById(id);
        if (article == null) return false;
        _articles.Remove(article);
        return true;
    }
}

// Setup
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebKitRouter();
builder.Services.AddSingleton<ArticleRepository>();

var app = builder.Build();
app.UseWebKitRouter();

var repo = app.Services.GetRequiredService<ArticleRepository>();

// Routes
app.MapRouteGroup("/api/articles", group =>
{
    // GET /api/articles
    group.MapRoute("GET", "", async (ctx, ct) =>
    {
        var articles = repo.GetAll();
        return RouteResult.Json(articles);
    });

    // GET /api/articles/{id}
    group.MapRoute("GET", "/{id:int}", async (ctx, ct) =>
    {
        var id = int.Parse(ctx.Match.Parameters["id"]);
        var article = repo.GetById(id);
        return article == null 
            ? RouteResult.NotFound($"Article {id} not found")
            : RouteResult.Json(article);
    });

    // POST /api/articles
    group.MapRoute("POST", "", async (ctx, ct) =>
    {
        var article = await ctx.HttpContext.Request.ReadAsAsync<Article>(ct);
        var created = repo.Create(article);
        return RouteResult.Json(created, 201);
    });

    // PUT /api/articles/{id}
    group.MapRoute("PUT", "/{id:int}", async (ctx, ct) =>
    {
        var id = int.Parse(ctx.Match.Parameters["id"]);
        var article = await ctx.HttpContext.Request.ReadAsAsync<Article>(ct);
        var success = repo.Update(id, article);
        return success 
            ? RouteResult.Json(repo.GetById(id))
            : RouteResult.NotFound();
    });

    // DELETE /api/articles/{id}
    group.MapRoute("DELETE", "/{id:int}", async (ctx, ct) =>
    {
        var id = int.Parse(ctx.Match.Parameters["id"]);
        var success = repo.Delete(id);
        return success 
            ? RouteResult.Ok() 
            : RouteResult.NotFound();
    });
});

app.Run();
```

---

## Building an Admin Dashboard API

Protected admin endpoints with authorization.

```csharp
// Auth service
public interface IAuthService
{
    bool IsAdmin(ClaimsPrincipal user);
    bool IsAuthenticated(ClaimsPrincipal user);
}

public class AuthService : IAuthService
{
    public bool IsAuthenticated(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin(ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Role)?.Value == "admin";
}

// Chain nodes
public class AdminAuthChainNode : IChainNode
{
    private readonly IAuthService _auth;

    public AdminAuthChainNode(IAuthService auth) => _auth = auth;

    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        var user = context.HttpContext.User;
        
        if (!_auth.IsAuthenticated(user))
            return ChainResult.Stop(RouteResult.Unauthorized());
        
        if (!_auth.IsAdmin(user))
            return ChainResult.Stop(RouteResult.Forbidden());
        
        return ChainResult.Next();
    }
}

public class AuditLoggingChainNode : IChainNode
{
    private readonly ILogger<AuditLoggingChainNode> _logger;

    public AuditLoggingChainNode(ILogger<AuditLoggingChainNode> logger) =>
        _logger = logger;

    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        var user = context.HttpContext.User.FindFirst("sub")?.Value ?? "unknown";
        _logger.LogInformation("Admin action: {User} {Method} {Path}", 
            user, context.Match.Method, context.Match.Path);
        return ChainResult.Next();
    }
}

// Setup
builder.Services.AddSingleton<IAuthService, AuthService>();

app.MapRouteGroup("/admin", group =>
{
    group.AddChainNode(new AdminAuthChainNode(app.Services.GetRequiredService<IAuthService>()));
    group.AddChainNode(new AuditLoggingChainNode(app.Services.GetRequiredService<ILogger<AuditLoggingChainNode>>()));

    group.MapRoute("GET", "/users", async (ctx, ct) =>
        RouteResult.Json(await GetAllUsers(ct)));

    group.MapRoute("GET", "/users/{id:int}", async (ctx, ct) =>
    {
        var id = int.Parse(ctx.Match.Parameters["id"]);
        var user = await GetUser(id, ct);
        return user == null 
            ? RouteResult.NotFound() 
            : RouteResult.Json(user);
    });

    group.MapRoute("POST", "/users/{id:int}/disable", async (ctx, ct) =>
    {
        var id = int.Parse(ctx.Match.Parameters["id"]);
        await DisableUser(id, ct);
        return RouteResult.Ok();
    });

    group.MapRoute("GET", "/audit-log", async (ctx, ct) =>
        RouteResult.Json(await GetAuditLog(ct)));
});
```

---

## Building a Webhook Service

Sending webhooks to external services.

```csharp
public class Webhook
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public string[] Events { get; set; } = Array.Empty<string>();
    public bool Active { get; set; } = true;
}

public class WebhookService
{
    private readonly List<Webhook> _webhooks = new();
    private readonly HttpClient _client;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(HttpClient client, ILogger<WebhookService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task TriggerEvent(string eventType, object data, CancellationToken ct)
    {
        var webhooks = _webhooks
            .Where(w => w.Active && w.Events.Contains(eventType))
            .ToList();

        foreach (var webhook in webhooks)
        {
            try
            {
                var payload = new { @event = eventType, data, timestamp = DateTime.UtcNow };
                var response = await _client.PostAsJsonAsync(webhook.Url, payload, ct);
                
                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("Webhook {Url} returned {StatusCode}", webhook.Url, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger webhook {Url}", webhook.Url);
            }
        }
    }

    public List<Webhook> GetAll() => _webhooks;
    
    public Webhook? GetById(int id) => _webhooks.FirstOrDefault(w => w.Id == id);
    
    public Webhook Register(Webhook webhook)
    {
        webhook.Id = _webhooks.Any() ? _webhooks.Max(w => w.Id) + 1 : 1;
        _webhooks.Add(webhook);
        return webhook;
    }
}

// Routes
var webhookService = new WebhookService(new HttpClient(), app.Services.GetRequiredService<ILogger<WebhookService>>());

app.MapRouteGroup("/api/webhooks", group =>
{
    group.AddChainNode(new AuthChainNode());

    group.MapRoute("GET", "", async (ctx, ct) =>
        RouteResult.Json(webhookService.GetAll()));

    group.MapRoute("POST", "", async (ctx, ct) =>
    {
        var webhook = await ctx.HttpContext.Request.ReadAsAsync<Webhook>(ct);
        var registered = webhookService.Register(webhook);
        return RouteResult.Json(registered, 201);
    });

    group.MapRoute("DELETE", "/{id:int}", async (ctx, ct) =>
    {
        var id = int.Parse(ctx.Match.Parameters["id"]);
        var webhook = webhookService.GetById(id);
        if (webhook == null) return RouteResult.NotFound();
        
        webhook.Active = false;
        return RouteResult.Ok();
    });
});

// Trigger events from elsewhere in your app
await webhookService.TriggerEvent("user.created", new { userId = 123 }, CancellationToken.None);
```

---

## Building a File Upload Service

Handling file uploads with validation.

```csharp
public class FileUploadChainNode : IChainNode
{
    private readonly long _maxSize = 10 * 1024 * 1024; // 10MB
    private readonly string[] _allowedTypes = { "image/jpeg", "image/png", "application/pdf" };

    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        if (context.HttpContext.Request.ContentLength > _maxSize)
            return ChainResult.Stop(RouteResult.BadRequest("File too large"));

        var contentType = context.HttpContext.Request.ContentType;
        if (!_allowedTypes.Contains(contentType))
            return ChainResult.Stop(RouteResult.BadRequest("File type not allowed"));

        return ChainResult.Next();
    }
}

app.MapRoute("POST", "/api/upload", async (ctx, ct) =>
{
    var fileName = $"{Guid.NewGuid()}.file";
    var filePath = Path.Combine("/uploads", fileName);
    
    using (var fileStream = System.IO.File.Create(filePath))
    {
        await ctx.HttpContext.Request.Body.CopyToAsync(fileStream, ct);
    }

    return RouteResult.Json(new { fileName, url = $"/files/{fileName}" }, 201);
})
.AddChainNode(new FileUploadChainNode());

app.MapRoute("GET", "/files/{filename:filename}", async (ctx, ct) =>
{
    var filename = ctx.Match.Parameters["filename"];
    var filePath = Path.Combine("/uploads", filename);
    
    if (!System.IO.File.Exists(filePath))
        return RouteResult.NotFound();

    var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath, ct);
    return RouteResult.Ok(System.Convert.ToBase64String(fileBytes));
});
```

---

## Building a Search API

Implementing search with filters and pagination.

```csharp
public class SearchRequest
{
    public string Query { get; set; } = "";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "relevance";
    public Dictionary<string, string> Filters { get; set; } = new();
}

app.MapRoute("POST", "/api/search", async (ctx, ct) =>
{
    var request = await ctx.HttpContext.Request.ReadAsAsync<SearchRequest>(ct);
    
    var results = Search(
        request.Query,
        request.Page,
        request.PageSize,
        request.SortBy,
        request.Filters);

    return RouteResult.Json(results);
});

private object Search(string query, int page, int pageSize, string sortBy, Dictionary<string, string> filters)
{
    // Implement search logic
    var items = AllItems
        .Where(item => item.Text.Contains(query, StringComparison.OrdinalIgnoreCase))
        .Where(item => ApplyFilters(item, filters))
        .OrderBy(item => sortBy switch 
        {
            "date" => item.CreatedAt,
            "title" => item.Title,
            _ => item.Relevance
        })
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();

    return new
    {
        query,
        page,
        pageSize,
        total = AllItems.Count,
        results = items
    };
}
```

---

## Building a Rate Limiting Service

Advanced rate limiting with different tiers.

```csharp
public class RateLimitTier
{
    public string Name { get; set; } = "";
    public int RequestsPerMinute { get; set; }
    public int RequestsPerHour { get; set; }
    public int RequestsPerDay { get; set; }
}

public class AdvancedRateLimitChainNode : IChainNode
{
    private readonly Dictionary<string, RateLimitTier> _tiers;
    private readonly Dictionary<string, (int minute, int hour, int day, DateTime reset)> _usage = new();

    public AdvancedRateLimitChainNode(Dictionary<string, RateLimitTier> tiers) =>
        _tiers = tiers;

    public async ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken ct)
    {
        var clientId = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var tier = GetClientTier(context.HttpContext.User);

        if (!_usage.TryGetValue(clientId, out var usage))
            usage = (0, 0, 0, DateTime.UtcNow);

        // Check limits
        if (usage.minute >= tier.RequestsPerMinute)
        {
            return ChainResult.Stop(RouteResult.TooManyRequests(
                ((int)(DateTime.UtcNow.AddMinutes(1) - DateTime.UtcNow).TotalSeconds).ToString()));
        }

        usage.minute++;
        usage.hour++;
        usage.day++;

        // Reset counters at intervals
        if (DateTime.UtcNow - usage.reset > TimeSpan.FromMinutes(1))
        {
            usage.minute = 1;
            usage.reset = DateTime.UtcNow;
        }

        _usage[clientId] = usage;
        return ChainResult.Next();
    }

    private RateLimitTier GetClientTier(ClaimsPrincipal user)
    {
        var tier = user.FindFirst("tier")?.Value ?? "free";
        return _tiers.GetValueOrDefault(tier, _tiers["free"])!;
    }
}
```

---

**End of Practical Guides**

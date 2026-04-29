namespace UserManagementAPI.Middleware;

public class TokenAuthenticationMiddleware
{
    private const string AuthHeader = "Authorization";
    private const string BearerPrefix = "Bearer ";

    // Paths that bypass the token check so devs can browse the API doc.
    private static readonly string[] PublicPathPrefixes =
    {
        "/swagger",
        "/openapi"
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<TokenAuthenticationMiddleware> _logger;
    private readonly string _expectedToken;

    public TokenAuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<TokenAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _expectedToken = configuration["Auth:ApiToken"]
            ?? throw new InvalidOperationException(
                "Auth:ApiToken is not configured. Set it in appsettings.json or via environment variable Auth__ApiToken.");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (IsPublic(path))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(AuthHeader, out var headerValues))
        {
            await Reject(context, "Missing Authorization header.");
            return;
        }

        var header = headerValues.ToString();
        if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await Reject(context, "Authorization header must use the Bearer scheme.");
            return;
        }

        var presented = header[BearerPrefix.Length..].Trim();
        if (string.IsNullOrEmpty(presented)
            || !string.Equals(presented, _expectedToken, StringComparison.Ordinal))
        {
            await Reject(context, "Invalid token.");
            return;
        }

        await _next(context);
    }

    private static bool IsPublic(string path) =>
        PublicPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private async Task Reject(HttpContext context, string reason)
    {
        _logger.LogWarning(
            "Auth rejected on {Method} {Path}: {Reason}",
            context.Request.Method,
            context.Request.Path,
            reason);

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        context.Response.Headers["WWW-Authenticate"] = "Bearer";
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized." });
    }
}

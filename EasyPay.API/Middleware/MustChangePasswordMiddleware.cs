using System.Net;
using System.Text.Json;
using EasyPay.Core.Common;

namespace EasyPay.API.Middleware;


public class MustChangePasswordMiddleware
{
    private readonly RequestDelegate _next;

    
    private static readonly HashSet<string> AllowedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/auth/login",
        "/api/v1/auth/refresh",
        "/api/v1/auth/change-password"
    };

    public MustChangePasswordMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var mustChange = context.User.FindFirst("MustChangePassword")?.Value;

            if (string.Equals(mustChange, "true", StringComparison.OrdinalIgnoreCase))
            {
                var path = context.Request.Path.Value ?? string.Empty;

                var normalised = System.Text.RegularExpressions.Regex
                    .Replace(path, @"^/api/v\d+\.\d+/", "/api/v1/");
                normalised = System.Text.RegularExpressions.Regex
                    .Replace(normalised, @"^/api/v\d+/", "/api/v1/");

                if (!AllowedPaths.Contains(normalised))
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;

                    var body = ApiResponse.Fail(
                        "You must change your password before accessing this resource. " +
                        "Please call POST /api/v1/auth/change-password.");

                    await context.Response.WriteAsync(
                        JsonSerializer.Serialize(body, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        }));
                    return;
                }
            }
        }

        await _next(context);
    }
}

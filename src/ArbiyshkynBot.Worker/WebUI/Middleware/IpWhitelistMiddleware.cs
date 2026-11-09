using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ArbiyshkynBot.Worker.WebUI.Middleware;

public class IpWhitelistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpWhitelistMiddleware> _logger;
    private readonly IPAddress? _allowedIp;

    public IpWhitelistMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<IpWhitelistMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        
        var allowedIpStr = configuration["ALLOWED_IP"];
        if (!string.IsNullOrEmpty(allowedIpStr) && IPAddress.TryParse(allowedIpStr, out var parsedIp))
        {
            _allowedIp = parsedIp;
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Пропускаем health check и публичные статические файлы при необходимости
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Проверяем реальный IP с учетом Nginx / Cloudflare прокси (X-Forwarded-For)
        IPAddress? remoteIp = context.Connection.RemoteIpAddress;
        
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var headerValue))
        {
            var ipString = headerValue.FirstOrDefault()?.Split(',')[0].Trim();
            if (IPAddress.TryParse(ipString, out var parsedForwardedIp))
            {
                remoteIp = parsedForwardedIp;
            }
        }

        if (_allowedIp != null && remoteIp != null && !remoteIp.Equals(_allowedIp))
        {
            _logger.LogWarning("Blocked unauthorized access from IP: {RemoteIp}", remoteIp);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Access Denied: IP is not whitelisted.");
            return;
        }

        await _next(context);
    }
}
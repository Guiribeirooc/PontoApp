using Microsoft.Extensions.Options;
using PontoApp.Infrastructure.Security;

namespace PontoApp.Web.Middleware;

public sealed class IpAllowListMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IpAllowListOptions _options;
    private readonly ILogger<IpAllowListMiddleware> _logger;

    public IpAllowListMiddleware(
        RequestDelegate next,
        IOptions<IpAllowListOptions> options,
        ILogger<IpAllowListMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        if (!_options.Enforce)
        {
            await _next(context);
            return;
        }

        var remoteIp = context.Connection.RemoteIpAddress;
        var ip = remoteIp?.ToString() ?? string.Empty;

        if (IsAllowed(ip))
        {
            await _next(context);
            return;
        }

        _logger.LogWarning("IP bloqueado pela allow-list: {Ip} - Path: {Path}", ip, context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("Acesso negado (IP não permitido).");
    }

    private bool IsAllowed(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return false;
        if (_options.Allowed is null || _options.Allowed.Length == 0) return false;

        foreach (var rule in _options.Allowed)
        {
            if (string.IsNullOrWhiteSpace(rule)) continue;

            if (ip.StartsWith(rule, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(ip, rule, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

using DebtManager.Contracts.Audit;
using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Audit;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DebtManager.Infrastructure.Audit;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(IAuditLogRepository auditLogRepository, IHttpContextAccessor httpContextAccessor)
    {
        _auditLogRepository = auditLogRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        string? entityId = null,
        string? details = null,
        CancellationToken ct = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var userEmail = httpContext.User.FindFirstValue(ClaimTypes.Email) ?? "system@adeva.local";
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();

        var auditLog = AuditLog.Create(
            userId,
            userEmail,
            action,
            entityType,
            entityId,
            details,
            ipAddress,
            userAgent);

        await _auditLogRepository.AddAsync(auditLog, ct);
        await _auditLogRepository.SaveChangesAsync(ct);
    }
}

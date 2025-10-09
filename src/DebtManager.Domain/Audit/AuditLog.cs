using DebtManager.Domain.Common;

namespace DebtManager.Domain.Audit;

public class AuditLog : Entity
{
    public string UserId { get; private set; } = string.Empty;
    public string UserEmail { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    public string? Details { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string userId,
        string userEmail,
        string action,
        string entityType,
        string? entityId = null,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new AuditLog
        {
            UserId = userId,
            UserEmail = userEmail,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }
}

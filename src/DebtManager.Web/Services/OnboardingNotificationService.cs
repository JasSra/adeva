using DebtManager.Domain.Communications;
using DebtManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using HandlebarsDotNet;

namespace DebtManager.Web.Services;

public interface IOnboardingNotificationService
{
    Task QueueClientWelcomeAsync(Guid orgId, string firstName, string lastName, string? email, CancellationToken ct = default);
    Task QueueAdminNewClientAlertAsync(Guid orgId, string firstName, string lastName, string? contactEmail, CancellationToken ct = default);
}

public class OnboardingNotificationService : IOnboardingNotificationService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<OnboardingNotificationService> _logger;

    public OnboardingNotificationService(AppDbContext db, IConfiguration config, ILogger<OnboardingNotificationService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task QueueClientWelcomeAsync(Guid orgId, string firstName, string lastName, string? email, CancellationToken ct = default)
    {
        var org = await _db.Organizations.FindAsync(new object?[] { orgId }, ct);
        if (org == null)
        {
            _logger.LogWarning("Organization {OrgId} not found for welcome email", orgId);
            return;
        }

        var template = await _db.Set<MessageTemplate>()
            .FirstOrDefaultAsync(t => t.Code == "client-onboarding-welcome", ct);

        if (template == null)
        {
            _logger.LogWarning("Template 'client-onboarding-welcome' not found");
            return;
        }

        var data = new Dictionary<string, object>
        {
            ["PlatformName"] = "Adeva Debt Management",
            ["ContactFirstName"] = firstName,
            ["ContactLastName"] = lastName,
            ["OrganizationName"] = org.Name,
            ["LegalName"] = org.LegalName,
            ["TradingName"] = org.TradingName ?? string.Empty,
            ["Abn"] = org.Abn,
            ["Subdomain"] = org.Subdomain ?? string.Empty,
            ["SupportEmail"] = org.SupportEmail,
            ["SupportPhone"] = org.SupportPhone
        };

        var subject = Handlebars.Compile(template.Subject)(data);
        var body = Handlebars.Compile(template.BodyTemplate)(data);

        var queued = new QueuedMessage(
            recipientEmail: email ?? org.SupportEmail,
            subject: subject,
            body: body,
            channel: MessageChannel.Email,
            relatedEntityType: "Organization",
            relatedEntityId: org.Id
        );
        _db.Set<QueuedMessage>().Add(queued);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Welcome email queued for organization {OrgId}", orgId);
    }

    public async Task QueueAdminNewClientAlertAsync(Guid orgId, string firstName, string lastName, string? contactEmail, CancellationToken ct = default)
    {
        var org = await _db.Organizations.FindAsync(new object?[] { orgId }, ct);
        if (org == null)
        {
            _logger.LogWarning("Organization {OrgId} not found for admin notification", orgId);
            return;
        }

        var template = await _db.Set<MessageTemplate>()
            .FirstOrDefaultAsync(t => t.Code == "client-onboarding-admin-notification", ct);

        if (template == null)
        {
            _logger.LogWarning("Template 'client-onboarding-admin-notification' not found");
            return;
        }

        var baseUrl = _config["App:BaseUrl"] ?? "https://localhost:5001";
        var data = new Dictionary<string, object>
        {
            ["OrganizationName"] = org.Name,
            ["LegalName"] = org.LegalName,
            ["TradingName"] = org.TradingName ?? string.Empty,
            ["Abn"] = org.Abn,
            ["Acn"] = string.Empty, // future
            ["Subdomain"] = org.Subdomain ?? string.Empty,
            ["ContactFirstName"] = firstName,
            ["ContactLastName"] = lastName,
            ["ContactEmail"] = contactEmail ?? string.Empty,
            ["RegisteredAt"] = org.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            ["AdminPortalUrl"] = baseUrl
        };

        var subject = Handlebars.Compile(template.Subject)(data);
        var body = Handlebars.Compile(template.BodyTemplate)(data);

        // Fan out to all admins via queued email
        var adminRoleId = await _db.Roles
            .Where(r => r.Name == "Admin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);
        if (adminRoleId == Guid.Empty)
        {
            _logger.LogWarning("Admin role not found");
        }
        else
        {
            var adminUsers = await _db.UserRoles
                .Where(ur => ur.RoleId == adminRoleId)
                .Join(_db.Users, ur => ur.UserId, u => u.Id, (ur, u) => u)
                .ToListAsync(ct);

            foreach (var admin in adminUsers)
            {
                var queued = new QueuedMessage(
                    recipientEmail: admin.Email ?? "admin@debtmanager.local",
                    subject: subject,
                    body: body,
                    channel: MessageChannel.Email,
                    relatedEntityType: "Organization",
                    relatedEntityId: org.Id
                );
                _db.Set<QueuedMessage>().Add(queued);
            }
        }

        // Internal message to admins
        var internalMsg = new InternalMessage(
            title: $"New Client Registration: {org.Name}",
            content: $"A new organization '{org.Name}' (ABN: {org.Abn}) has registered and requires approval. Contact: {firstName} {lastName} ({contactEmail})",
            priority: MessagePriority.High,
            category: "Client Onboarding"
        );

        if (adminRoleId != Guid.Empty)
        {
            var adminIds = await _db.UserRoles
                .Where(ur => ur.RoleId == adminRoleId)
                .Select(ur => ur.UserId)
                .ToListAsync(ct);
            foreach (var id in adminIds)
            {
                internalMsg.AddRecipient(id);
            }
        }

        _db.Set<InternalMessage>().Add(internalMsg);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Admin notifications created for organization {OrgId}", orgId);
    }
}

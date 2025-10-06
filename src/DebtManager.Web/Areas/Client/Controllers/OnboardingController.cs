using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DebtManager.Contracts.External;
using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Organizations;
using DebtManager.Domain.Communications;
using DebtManager.Infrastructure.Identity;
using DebtManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using HandlebarsDotNet;

namespace DebtManager.Web.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Policy = "RequireClientScope")]
public class OnboardingController : Controller
{
    private readonly IAbrValidator _abr;
    private readonly IBusinessLookupService _businessLookup;
    private readonly IOrganizationRepository _orgRepo;
    private readonly AppDbContext _db;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(
        IAbrValidator abr, 
        IBusinessLookupService businessLookup, 
        IOrganizationRepository orgRepo, 
        AppDbContext db,
        ILogger<OnboardingController> logger)
    {
        _abr = abr;
        _businessLookup = businessLookup;
        _orgRepo = orgRepo;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Step 1: ABN/ACN Validation
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return View(new ClientOnboardingVm());
    }

    /// <summary>
    /// Step 2: Validate ABN/ACN and extract business info
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidateBusiness(ClientOnboardingVm vm, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vm.Abn))
        {
            ModelState.AddModelError("Abn", "ABN is required.");
            return View("Index", vm);
        }

        // Validate ABN and get business details
        var abnResult = await _abr.ValidateAbnAsync(vm.Abn, ct);
        vm.IsValidAbn = abnResult.IsValid;
        
        if (!vm.IsValidAbn.Value)
        {
            ModelState.AddModelError("Abn", abnResult.ErrorMessage ?? "Invalid ABN. Please check and try again.");
            return View("Index", vm);
        }

        // Extract business information from ABN validation
        vm.ExtractedBusinessName = abnResult.BusinessName ?? abnResult.LegalName;
        vm.ExtractedLegalName = abnResult.LegalName ?? abnResult.BusinessName;
        vm.ExtractedTradingName = abnResult.TradingName;
        vm.ExtractedAbn = abnResult.Abn;
        vm.ExtractedAcn = abnResult.Acn;

        // Validate ACN if provided
        if (!string.IsNullOrWhiteSpace(vm.Acn))
        {
            vm.IsValidAcn = await _businessLookup.ValidateAcnAsync(vm.Acn, ct);
            if (vm.IsValidAcn != true)
            {
                ModelState.AddModelError("Acn", "Invalid ACN. Please check and try again.");
                return View("Index", vm);
            }
        }

        // If ABN validation didn't return business name, try business lookup service
        if (string.IsNullOrWhiteSpace(vm.ExtractedBusinessName))
        {
            var businessInfo = await _businessLookup.LookupByAbnAsync(vm.Abn, ct);
            if (businessInfo != null)
            {
                vm.ExtractedBusinessName = businessInfo.Name;
                vm.ExtractedLegalName = businessInfo.LegalName ?? businessInfo.Name;
                vm.ExtractedTradingName = businessInfo.TradingName;
                vm.ExtractedAbn = businessInfo.Abn;
                vm.ExtractedAcn = businessInfo.Acn;
            }
        }

        // Pre-fill user details from claims
        var firstName = User.FindFirstValue(ClaimTypes.GivenName);
        var lastName = User.FindFirstValue(ClaimTypes.Surname);
        var email = User.FindFirstValue(ClaimTypes.Email);

        vm.ContactFirstName = firstName ?? string.Empty;
        vm.ContactLastName = lastName ?? string.Empty;
        vm.ContactEmail = email ?? string.Empty;

        // Move to Step 2: Confirm Details
        return View("ConfirmDetails", vm);
    }

    /// <summary>
    /// Step 3: Review extracted business info and user details
    /// </summary>
    [HttpGet]
    public IActionResult ConfirmDetails()
    {
        // Redirect back to start if accessed directly
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Step 4: Create organization and send notifications
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientOnboardingVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View("ConfirmDetails", vm);
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(vm.ContactFirstName) || string.IsNullOrWhiteSpace(vm.ContactLastName))
        {
            ModelState.AddModelError(string.Empty, "Contact first name and last name are required.");
            return View("ConfirmDetails", vm);
        }

        // Check for duplicate ABN
        if (!string.IsNullOrWhiteSpace(vm.Abn))
        {
            var existingAbn = await _orgRepo.GetByAbnAsync(vm.Abn, ct);
            if (existingAbn != null)
            {
                ModelState.AddModelError("Abn", "An organization with this ABN already exists.");
                return View("Index", vm);
            }
        }

        // Check for duplicate subdomain
        if (!string.IsNullOrWhiteSpace(vm.Subdomain))
        {
            var existingSubdomain = await _orgRepo.GetBySubdomainAsync(vm.Subdomain, ct);
            if (existingSubdomain != null)
            {
                ModelState.AddModelError("Subdomain", "This subdomain is already in use.");
                return View("ConfirmDetails", vm);
            }
        }

        // Create organization (pending approval)
        var org = Organization.CreatePending(
            name: vm.ExtractedBusinessName ?? vm.Name,
            legalName: vm.ExtractedLegalName ?? vm.Name,
            abn: vm.Abn!,
            defaultCurrency: "AUD",
            primaryColorHex: "#1e3a8a",
            secondaryColorHex: "#3b82f6",
            supportEmail: vm.SupportEmail ?? vm.ContactEmail ?? "support@debtmanager.local",
            supportPhone: vm.SupportPhone ?? "1300000000",
            timezone: vm.Timezone ?? "Australia/Sydney",
            subdomain: vm.Subdomain,
            tradingName: vm.ExtractedTradingName ?? vm.TradingName
        );

        await _orgRepo.AddAsync(org, ct);
        await _orgRepo.SaveChangesAsync(ct);

        // Attach organization to current user's profile
        var externalId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _db.Users.FirstAsync(u => u.ExternalAuthId == externalId, ct);
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        
        if (profile == null)
        {
            profile = new UserProfile 
            { 
                Id = Guid.NewGuid(), 
                UserId = user.Id,
                FirstName = vm.ContactFirstName,
                LastName = vm.ContactLastName
            };
            _db.UserProfiles.Add(profile);
        }
        else
        {
            profile.FirstName = vm.ContactFirstName;
            profile.LastName = vm.ContactLastName;
        }

        profile.OrganizationId = org.Id;
        await _db.SaveChangesAsync(ct);

        // Queue welcome email to user (using Handlebars template)
        BackgroundJob.Enqueue(() => SendWelcomeEmailAsync(org.Id, user.Id, vm.ContactFirstName, vm.ContactLastName, vm.ContactEmail));

        // Queue internal notification to admins
        BackgroundJob.Enqueue(() => SendAdminNotificationAsync(org.Id, user.Id, vm.ContactFirstName, vm.ContactLastName, vm.ContactEmail));

        // Redirect to "What's Next" page
        return RedirectToAction(nameof(WhatsNext), new { orgId = org.Id });
    }

    /// <summary>
    /// Step 5: What's Next - Pending approval message
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> WhatsNext(Guid orgId, CancellationToken ct)
    {
        var org = await _orgRepo.GetAsync(orgId, ct);
        if (org == null)
        {
            return NotFound();
        }

        var vm = new WhatsNextVm
        {
            OrganizationName = org.Name,
            TradingName = org.TradingName,
            SupportEmail = org.SupportEmail,
            SupportPhone = org.SupportPhone,
            IsApproved = org.IsApproved
        };

        return View(vm);
    }

    /// <summary>
    /// Background job: Send welcome email to client using Handlebars template
    /// </summary>
    public async Task SendWelcomeEmailAsync(Guid orgId, Guid userId, string firstName, string lastName, string? email)
    {
        try
        {
            var org = await _db.Organizations.FindAsync(orgId);
            if (org == null)
            {
                _logger.LogWarning("Organization {OrgId} not found for welcome email", orgId);
                return;
            }

            // Get template
            var template = await _db.Set<MessageTemplate>()
                .FirstOrDefaultAsync(t => t.Code == "client-onboarding-welcome");

            if (template == null)
            {
                _logger.LogWarning("Template 'client-onboarding-welcome' not found");
                return;
            }

            // Prepare template data
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

            // Compile and render template
            var subjectTemplate = Handlebars.Compile(template.Subject);
            var bodyTemplate = Handlebars.Compile(template.BodyTemplate);

            var subject = subjectTemplate(data);
            var body = bodyTemplate(data);

            // Create queued message
            var queuedMessage = new QueuedMessage(
                recipientEmail: email ?? org.SupportEmail,
                subject: subject,
                body: body,
                channel: MessageChannel.Email,
                relatedEntityType: "Organization",
                relatedEntityId: org.Id
            );

            _db.Set<QueuedMessage>().Add(queuedMessage);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Welcome email queued for organization {OrgId}", orgId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue welcome email for organization {OrgId}", orgId);
        }
    }

    /// <summary>
    /// Background job: Send admin notification using Handlebars template
    /// </summary>
    public async Task SendAdminNotificationAsync(Guid orgId, Guid userId, string firstName, string lastName, string? email)
    {
        try
        {
            var org = await _db.Organizations.FindAsync(orgId);
            if (org == null)
            {
                _logger.LogWarning("Organization {OrgId} not found for admin notification", orgId);
                return;
            }

            // Get template
            var template = await _db.Set<MessageTemplate>()
                .FirstOrDefaultAsync(t => t.Code == "client-onboarding-admin-notification");

            if (template == null)
            {
                _logger.LogWarning("Template 'client-onboarding-admin-notification' not found");
                return;
            }

            // Get all admin users
            var adminRoleId = await _db.Roles
                .Where(r => r.Name == "Admin")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (adminRoleId == Guid.Empty)
            {
                _logger.LogWarning("Admin role not found");
                return;
            }

            var adminUsers = await _db.UserRoles
                .Where(ur => ur.RoleId == adminRoleId)
                .Join(_db.Users, ur => ur.UserId, u => u.Id, (ur, u) => u)
                .ToListAsync();

            if (!adminUsers.Any())
            {
                _logger.LogWarning("No admin users found to notify");
                return;
            }

            // Prepare template data
            var baseUrl = "https://localhost:5001"; // TODO: Get from configuration
            var data = new Dictionary<string, object>
            {
                ["OrganizationName"] = org.Name,
                ["LegalName"] = org.LegalName,
                ["TradingName"] = org.TradingName ?? string.Empty,
                ["Abn"] = org.Abn,
                ["Acn"] = string.Empty, // TODO: Add ACN to org domain model
                ["Subdomain"] = org.Subdomain ?? string.Empty,
                ["ContactFirstName"] = firstName,
                ["ContactLastName"] = lastName,
                ["ContactEmail"] = email ?? string.Empty,
                ["RegisteredAt"] = org.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                ["AdminPortalUrl"] = baseUrl
            };

            // Compile and render template
            var subjectTemplate = Handlebars.Compile(template.Subject);
            var bodyTemplate = Handlebars.Compile(template.BodyTemplate);

            var subject = subjectTemplate(data);
            var body = bodyTemplate(data);

            // Queue notification for each admin
            foreach (var admin in adminUsers)
            {
                var queuedMessage = new QueuedMessage(
                    recipientEmail: admin.Email ?? "admin@debtmanager.local",
                    subject: subject,
                    body: body,
                    channel: MessageChannel.Email,
                    relatedEntityType: "Organization",
                    relatedEntityId: org.Id
                );

                _db.Set<QueuedMessage>().Add(queuedMessage);
            }

            // Also create internal message for admins
            var internalMessage = new InternalMessage(
                title: $"New Client Registration: {org.Name}",
                content: $"A new organization '{org.Name}' (ABN: {org.Abn}) has registered and requires approval. Contact: {firstName} {lastName} ({email})",
                priority: MessagePriority.High,
                category: "Client Onboarding"
            );

            // Send to all admins
            foreach (var admin in adminUsers)
            {
                internalMessage.AddRecipient(admin.Id);
            }

            _db.Set<InternalMessage>().Add(internalMessage);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Admin notifications queued for organization {OrgId} to {Count} admins", orgId, adminUsers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue admin notifications for organization {OrgId}", orgId);
        }
    }
}

public class ClientOnboardingVm
{
    // Step 1: ABN/ACN Input
    [Required(ErrorMessage = "ABN is required")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "ABN must be 11 digits")]
    public string Abn { get; set; } = string.Empty;

    [RegularExpression(@"^\d{9}$", ErrorMessage = "ACN must be 9 digits")]
    public string? Acn { get; set; }

    // Step 1: Validation Results
    public bool? IsValidAbn { get; set; }
    public bool? IsValidAcn { get; set; }

    // Step 2: Extracted Business Info
    public string? ExtractedBusinessName { get; set; }
    public string? ExtractedLegalName { get; set; }
    public string? ExtractedTradingName { get; set; }
    public string? ExtractedAbn { get; set; }
    public string? ExtractedAcn { get; set; }

    // Step 2: User Contact Details
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public string ContactFirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public string ContactLastName { get; set; } = string.Empty;

    [EmailAddress]
    public string? ContactEmail { get; set; }

    // Step 3: Additional Organization Details
    [StringLength(200)]
    public string? Name { get; set; }

    [StringLength(200)]
    public string? TradingName { get; set; }

    [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Lowercase letters, numbers and hyphen only")]
    [StringLength(63)]
    public string? Subdomain { get; set; }

    [EmailAddress]
    public string? SupportEmail { get; set; }

    [Phone]
    public string? SupportPhone { get; set; }

    public string? Timezone { get; set; }
}

public class WhatsNextVm
{
    public string OrganizationName { get; set; } = string.Empty;
    public string? TradingName { get; set; }
    public string SupportEmail { get; set; } = string.Empty;
    public string SupportPhone { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
}

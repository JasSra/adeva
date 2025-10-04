using DebtManager.Domain.Debtors;
using DebtManager.Domain.Organizations;

namespace DebtManager.Infrastructure.Identity;

public class UserProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // Common profile fields
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    // Role-specific links
    public Guid? OrganizationId { get; set; } // for Client role
    public Guid? DebtorId { get; set; } // for User role (debtor portal)

    public ApplicationUser? User { get; set; }
    public Organization? Organization { get; set; }
    public Debtor? Debtor { get; set; }
}

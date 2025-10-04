using DebtManager.Domain.Common;

namespace DebtManager.Domain.AdminUsers;

/// <summary>
/// Represents an administrator user in the system.
/// Admin signup is restricted - only existing admins can assign admin roles.
/// </summary>
public class AdminUser : Entity
{
    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string ExternalAuthId { get; private set; } = string.Empty; // Azure AD B2C Object ID
    public AdminRole Role { get; private set; }
    public DateTime? LastLoginUtc { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? AssignedByAdminId { get; private set; } // Which admin assigned this role

    private AdminUser() { } // EF Core

    public AdminUser(
        string email,
        string name,
        string externalAuthId,
        AdminRole role,
        Guid? assignedByAdminId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(externalAuthId))
            throw new ArgumentException("External auth ID is required", nameof(externalAuthId));

        Id = Guid.NewGuid();
        Email = email;
        Name = name;
        ExternalAuthId = externalAuthId;
        Role = role;
        IsActive = true;
        AssignedByAdminId = assignedByAdminId;
    }

    public void UpdateRole(AdminRole newRole, Guid assignedByAdminId)
    {
        Role = newRole;
        AssignedByAdminId = assignedByAdminId;
    }

    public void RecordLogin()
    {
        LastLoginUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}

public enum AdminRole
{
    Administrator = 1,
    ReadOnly = 2
}

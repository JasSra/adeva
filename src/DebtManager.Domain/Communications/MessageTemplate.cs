using DebtManager.Domain.Common;

namespace DebtManager.Domain.Communications;

/// <summary>
/// Represents a reusable message template with Handlebars syntax support
/// </summary>
public class MessageTemplate : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string BodyTemplate { get; private set; } = string.Empty;
    public MessageChannel Channel { get; private set; }
    public bool IsActive { get; private set; }
    public string? Description { get; private set; }

    private MessageTemplate() { }

    public MessageTemplate(
        string code,
        string name,
        string subject,
        string bodyTemplate,
        MessageChannel channel,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(bodyTemplate))
            throw new ArgumentException("Body template is required", nameof(bodyTemplate));

        Code = code;
        Name = name;
        Subject = subject;
        BodyTemplate = bodyTemplate;
        Channel = channel;
        Description = description;
        IsActive = true;
    }

    public void Update(string name, string subject, string bodyTemplate, string? description = null)
    {
        Name = name;
        Subject = subject;
        BodyTemplate = bodyTemplate;
        Description = description;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public enum MessageChannel
{
    Email = 1,
    Sms = 2,
    InApp = 3,
    Push = 4
}

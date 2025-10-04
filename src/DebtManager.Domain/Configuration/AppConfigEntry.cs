using DebtManager.Domain.Common;

namespace DebtManager.Domain.Configuration;

public class AppConfigEntry : Entity
{
    public string Key { get; private set; } = string.Empty;
    public string? Value { get; private set; }
    public bool IsSecret { get; private set; }

    private AppConfigEntry() { }

    public AppConfigEntry(string key, string? value, bool isSecret)
    {
        Key = key;
        Value = value;
        IsSecret = isSecret;
    }

    public void Update(string? value, bool? isSecret = null)
    {
        Value = value;
        if (isSecret.HasValue) IsSecret = isSecret.Value;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

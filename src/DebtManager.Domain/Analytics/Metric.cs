using DebtManager.Domain.Common;

namespace DebtManager.Domain.Analytics;

public enum MetricType
{
    Counter = 0,
    Gauge = 1,
    Histogram = 2
}

public class Metric : Entity
{
    public string Key { get; private set; } = string.Empty;
    public MetricType Type { get; private set; }
    public decimal Value { get; private set; }
    public string? Tags { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
    public Guid? OrganizationId { get; private set; }

    private Metric()
    {
    }

    public static Metric Record(string key, MetricType type, decimal value, string? tags = null, Guid? organizationId = null)
    {
        return new Metric
        {
            Key = key,
            Type = type,
            Value = value,
            Tags = tags,
            RecordedAtUtc = DateTime.UtcNow,
            OrganizationId = organizationId
        };
    }

    public void UpdateValue(decimal newValue)
    {
        Value = newValue;
        RecordedAtUtc = DateTime.UtcNow;
    }
}

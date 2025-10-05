using System.Threading;

namespace DebtManager.Web.Services;

public sealed class MaintenanceState : IMaintenanceState
{
    private int _maintenance; // 0 = off, 1 = on
    public Exception? StartupException { get; private set; }
    public DateTimeOffset? EnabledAt { get; private set; }

    public bool IsMaintenance => Volatile.Read(ref _maintenance) == 1;

    public void Enable(Exception? ex = null)
    {
        StartupException = ex;
        EnabledAt = DateTimeOffset.UtcNow;
        Interlocked.Exchange(ref _maintenance, 1);
    }

    public void Disable()
    {
        StartupException = null;
        EnabledAt = null;
        Interlocked.Exchange(ref _maintenance, 0);
    }
}

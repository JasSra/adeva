namespace DebtManager.Web.Services;

public interface IMaintenanceState
{
    bool IsMaintenance { get; }
    Exception? StartupException { get; }
    DateTimeOffset? EnabledAt { get; }

    void Enable(Exception? ex = null);
    void Disable();
}

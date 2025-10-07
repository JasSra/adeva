using Hangfire;
using Hangfire.Common;
using Hangfire.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DebtManager.Infrastructure.Persistence;
using DebtManager.Domain.Communications;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "RequireAdminScope")]
public class JobsController : Controller
{
    private readonly IRecurringJobManager _recurring;
    private readonly AppDbContext _db;

    public JobsController(IRecurringJobManager recurring, AppDbContext db)
    {
        _recurring = recurring;
        _db = db;
    }

    private static IMonitoringApi Monitoring => JobStorage.Current.GetMonitoringApi();

    [HttpGet]
    public IActionResult Index()
    {
        using var conn = JobStorage.Current.GetConnection();
        var rec = conn.GetRecurringJobs();
        var vms = rec.Select(r => new RecurringJobVm
        {
            Id = r.Id,
            Cron = r.Cron,
            LastExecution = r.LastExecution,
            NextExecution = r.NextExecution,
            LastJobId = r.LastJobId,
            TimeZoneId = r.TimeZoneId,
            Queue = r.Queue,
            Removed = r.Removed,
            Error = r.Error,
            CreatedAt = r.CreatedAt
        }).OrderBy(r => r.Id).ToList();

        var stats = new JobStatsVm
        {
            Enqueued = Monitoring.Queues().Sum(q => (int)q.Length),
            Processing = (int)Monitoring.ProcessingCount(),
            Scheduled = (int)Monitoring.ScheduledCount(),
            Succeeded = (int)Monitoring.SucceededListCount(),
            Failed = (int)Monitoring.FailedCount(),
            Recurring = vms.Count
        };

        var vm = new JobsIndexVm { Jobs = vms, Stats = stats };
        ViewBag.Title = "Background Jobs";
        return View(vm);
    }

    [HttpGet]
    public IActionResult Details(string id)
    {
        using var conn = JobStorage.Current.GetConnection();
        var dto = conn.GetRecurringJobs().FirstOrDefault(j => j.Id == id);
        if (dto == null) return NotFound();

        var history = GetRecentHistoryForRecurring(id, take: 20);

        var vm = new RecurringJobDetailVm
        {
            Id = dto.Id,
            Cron = dto.Cron,
            LastExecution = dto.LastExecution,
            NextExecution = dto.NextExecution,
            LastJobId = dto.LastJobId,
            TimeZoneId = dto.TimeZoneId,
            Queue = dto.Queue,
            Removed = dto.Removed,
            Error = dto.Error,
            CreatedAt = dto.CreatedAt,
            Method = dto.Job?.ToString() ?? "(unknown)",
            RecentRuns = history
        };
        ViewBag.Title = $"Job: {id}";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Trigger(string id)
    {
        RecurringJob.Trigger(id);
        TempData["Message"] = "Job triggered.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateCron(string id, string cron)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(cron))
        {
            TempData["Error"] = "Job id and CRON are required.";
            return RedirectToAction(nameof(Details), new { id });
        }

        using var conn = JobStorage.Current.GetConnection();
        var dto = conn.GetRecurringJobs().FirstOrDefault(j => j.Id == id);
        if (dto == null)
        {
            TempData["Error"] = "Job not found.";
            return RedirectToAction(nameof(Index));
        }

        // Re-register with same Job invocation and new CRON
        _recurring.AddOrUpdate(id, dto.Job!, cron, TimeZoneInfo.FindSystemTimeZoneById(dto.TimeZoneId ?? TimeZoneInfo.Utc.Id), dto.Queue);
        TempData["Message"] = "Schedule updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequeueFailedDispatches()
    {
        var failed = await _db.QueuedMessages
            .Where(m => m.Channel == MessageChannel.Email || m.Channel == MessageChannel.Sms)
            .Where(m => m.Status == QueuedMessageStatus.Failed)
            .ToListAsync();

        foreach (var m in failed)
        {
            m.MarkAsCancelled(); // clear to a safe state first
        }
        await _db.SaveChangesAsync();

        // Re-enqueue by creating new Pending entries with same payload
        foreach (var m in failed)
        {
            var clone = new QueuedMessage(
                recipientEmail: m.RecipientEmail,
                subject: m.Subject,
                body: m.Body,
                channel: m.Channel,
                relatedEntityType: m.RelatedEntityType,
                relatedEntityId: m.RelatedEntityId,
                recipientPhone: m.RecipientPhone
            );
            _db.QueuedMessages.Add(clone);
        }
        await _db.SaveChangesAsync();

        TempData["Message"] = $"Requeued {failed.Count} failed dispatch(es).";
        return RedirectToAction(nameof(Index));
    }

    private static List<JobHistoryItemVm> GetRecentHistoryForRecurring(string recurringId, int take)
    {
        var result = new List<JobHistoryItemVm>();

        // Collect succeeded and failed lists and filter by RecurringJobId parameter
        var succeeded = Monitoring.SucceededJobs(0, 100).ToList();
        var failed = Monitoring.FailedJobs(0, 100).ToList();

        foreach (var pair in succeeded)
        {
            var jobId = pair.Key;
            var details = Monitoring.JobDetails(jobId);
            if (details?.Properties != null && details.Properties.TryGetValue("RecurringJobId", out var rid) && rid == recurringId)
            {
                string? resultText = null;
                TimeSpan? duration = null;
                try { resultText = pair.Value?.GetType().GetProperty("Result")?.GetValue(pair.Value)?.ToString(); } catch {}
                try
                {
                    var durVal = pair.Value?.GetType().GetProperty("TotalDuration")?.GetValue(pair.Value);
                    if (durVal is long l) duration = TimeSpan.FromMilliseconds(l);
                    else if (durVal is int i) duration = TimeSpan.FromMilliseconds(i);
                }
                catch {}

                result.Add(new JobHistoryItemVm
                {
                    JobId = jobId,
                    State = "Succeeded",
                    CreatedAt = details.CreatedAt ?? DateTime.MinValue,
                    Result = resultText,
                    Duration = duration
                });
            }
        }

        foreach (var pair in failed)
        {
            var jobId = pair.Key;
            var details = Monitoring.JobDetails(jobId);
            if (details?.Properties != null && details.Properties.TryGetValue("RecurringJobId", out var rid) && rid == recurringId)
            {
                string? ex = null;
                try { ex = pair.Value?.GetType().GetProperty("ExceptionMessage")?.GetValue(pair.Value)?.ToString(); } catch {}
                result.Add(new JobHistoryItemVm
                {
                    JobId = jobId,
                    State = "Failed",
                    CreatedAt = details.CreatedAt ?? DateTime.MinValue,
                    ExceptionMessage = ex
                });
            }
        }

        return result
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .ToList();
    }
}

public class JobsIndexVm
{
    public List<RecurringJobVm> Jobs { get; set; } = new();
    public JobStatsVm Stats { get; set; } = new();
}

public class JobStatsVm
{
    public int Enqueued { get; set; }
    public int Processing { get; set; }
    public int Scheduled { get; set; }
    public int Failed { get; set; }
    public int Succeeded { get; set; }
    public int Recurring { get; set; }
}

public class RecurringJobVm
{
    public string Id { get; set; } = string.Empty;
    public string Cron { get; set; } = string.Empty;
    public DateTime? LastExecution { get; set; }
    public DateTime? NextExecution { get; set; }
    public string? LastJobId { get; set; }
    public string? TimeZoneId { get; set; }
    public string? Queue { get; set; }
    public bool Removed { get; set; }
    public string? Error { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class RecurringJobDetailVm : RecurringJobVm
{
    public string Method { get; set; } = string.Empty;
    public List<JobHistoryItemVm> RecentRuns { get; set; } = new();
}

public class JobHistoryItemVm
{
    public string JobId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ExceptionMessage { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? Result { get; set; }
}

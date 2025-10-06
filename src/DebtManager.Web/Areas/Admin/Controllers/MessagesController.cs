using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DebtManager.Infrastructure.Persistence;
using DebtManager.Domain.Communications;
using System.Security.Claims;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "RequireAdminScope")]
public class MessagesController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(AppDbContext db, ILogger<MessagesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Admin inbox - shows internal messages for current admin
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Inbox(
        string? status = null,
        string? priority = null,
        string? category = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var query = _db.Set<InternalMessageRecipient>()
            .Include(r => r.InternalMessage)
            .Where(r => r.UserId == userGuid);

        // Filter by status
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<InternalMessageStatus>(status, true, out var statusEnum))
        {
            query = query.Where(r => r.Status == statusEnum);
        }

        // Filter by priority
        if (!string.IsNullOrEmpty(priority) && Enum.TryParse<MessagePriority>(priority, true, out var priorityEnum))
        {
            query = query.Where(r => r.InternalMessage!.Priority == priorityEnum);
        }

        // Filter by category
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(r => r.InternalMessage!.Category == category);
        }

        var total = await query.CountAsync(ct);
        
        var messages = await query
            .OrderByDescending(r => r.InternalMessage!.SentAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new InboxMessageVm
            {
                Id = r.InternalMessage!.Id,
                Title = r.InternalMessage.Title,
                Content = r.InternalMessage.Content,
                Priority = r.InternalMessage.Priority,
                Category = r.InternalMessage.Category,
                SentAtUtc = r.InternalMessage.SentAtUtc,
                Status = r.Status,
                ReadAtUtc = r.ReadAtUtc,
                RelatedEntityType = r.InternalMessage.RelatedEntityType,
                RelatedEntityId = r.InternalMessage.RelatedEntityId
            })
            .ToListAsync(ct);

        var unreadCount = await _db.Set<InternalMessageRecipient>()
            .Where(r => r.UserId == userGuid && r.Status == InternalMessageStatus.Unread)
            .CountAsync(ct);

        var vm = new InboxVm
        {
            Messages = messages,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            StatusFilter = status,
            PriorityFilter = priority,
            CategoryFilter = category,
            UnreadCount = unreadCount
        };

        ViewBag.Title = "Inbox";
        return View(vm);
    }

    /// <summary>
    /// View message details and mark as read
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> View(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var recipient = await _db.Set<InternalMessageRecipient>()
            .Include(r => r.InternalMessage)
            .FirstOrDefaultAsync(r => r.InternalMessageId == id && r.UserId == userGuid, ct);

        if (recipient == null)
        {
            return NotFound();
        }

        // Mark as read if unread
        if (recipient.Status == InternalMessageStatus.Unread)
        {
            recipient.MarkAsRead();
            await _db.SaveChangesAsync(ct);
        }

        var vm = new MessageDetailVm
        {
            Id = recipient.InternalMessage!.Id,
            Title = recipient.InternalMessage.Title,
            Content = recipient.InternalMessage.Content,
            Priority = recipient.InternalMessage.Priority,
            Category = recipient.InternalMessage.Category,
            SentAtUtc = recipient.InternalMessage.SentAtUtc,
            ReadAtUtc = recipient.ReadAtUtc,
            Status = recipient.Status,
            RelatedEntityType = recipient.InternalMessage.RelatedEntityType,
            RelatedEntityId = recipient.InternalMessage.RelatedEntityId,
            IsSystemGenerated = recipient.InternalMessage.IsSystemGenerated
        };

        ViewBag.Title = "Message Details";
        return View(vm);
    }

    /// <summary>
    /// Mark message as read/unread
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRead(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var recipient = await _db.Set<InternalMessageRecipient>()
            .FirstOrDefaultAsync(r => r.InternalMessageId == id && r.UserId == userGuid, ct);

        if (recipient == null)
        {
            return NotFound();
        }

        if (recipient.Status == InternalMessageStatus.Unread)
        {
            recipient.MarkAsRead();
        }
        else
        {
            recipient.MarkAsUnread();
        }

        await _db.SaveChangesAsync(ct);

        return RedirectToAction(nameof(Inbox));
    }

    /// <summary>
    /// Archive message
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var recipient = await _db.Set<InternalMessageRecipient>()
            .FirstOrDefaultAsync(r => r.InternalMessageId == id && r.UserId == userGuid, ct);

        if (recipient == null)
        {
            return NotFound();
        }

        recipient.MarkAsArchived();
        await _db.SaveChangesAsync(ct);

        return RedirectToAction(nameof(Inbox));
    }

    /// <summary>
    /// View all queued/sent messages (admin overview)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> AllMessages(
        string? channel = null,
        string? status = null,
        string? search = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = _db.Set<QueuedMessage>().AsQueryable();

        // Filter by channel
        if (!string.IsNullOrEmpty(channel) && Enum.TryParse<MessageChannel>(channel, true, out var channelEnum))
        {
            query = query.Where(m => m.Channel == channelEnum);
        }

        // Filter by status
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<QueuedMessageStatus>(status, true, out var statusEnum))
        {
            query = query.Where(m => m.Status == statusEnum);
        }

        // Search
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(m => 
                m.RecipientEmail.Contains(search) ||
                m.Subject.Contains(search) ||
                (m.RecipientPhone != null && m.RecipientPhone.Contains(search)));
        }

        // Date range
        if (fromDate.HasValue)
        {
            query = query.Where(m => m.QueuedAtUtc >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            query = query.Where(m => m.QueuedAtUtc <= toDate.Value.AddDays(1));
        }

        var total = await query.CountAsync(ct);

        var messages = await query
            .OrderByDescending(m => m.QueuedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new QueuedMessageVm
            {
                Id = m.Id,
                RecipientEmail = m.RecipientEmail,
                RecipientPhone = m.RecipientPhone,
                Subject = m.Subject,
                Channel = m.Channel,
                Status = m.Status,
                QueuedAtUtc = m.QueuedAtUtc,
                SentAtUtc = m.SentAtUtc,
                FailedAtUtc = m.FailedAtUtc,
                ErrorMessage = m.ErrorMessage,
                RetryCount = m.RetryCount,
                RelatedEntityType = m.RelatedEntityType,
                RelatedEntityId = m.RelatedEntityId
            })
            .ToListAsync(ct);

        // Get statistics
        var stats = new MessageStatisticsVm
        {
            TotalQueued = await _db.Set<QueuedMessage>().CountAsync(m => m.Status == QueuedMessageStatus.Pending, ct),
            TotalSent = await _db.Set<QueuedMessage>().CountAsync(m => m.Status == QueuedMessageStatus.Sent, ct),
            TotalFailed = await _db.Set<QueuedMessage>().CountAsync(m => m.Status == QueuedMessageStatus.Failed, ct),
            EmailCount = await _db.Set<QueuedMessage>().CountAsync(m => m.Channel == MessageChannel.Email, ct),
            SmsCount = await _db.Set<QueuedMessage>().CountAsync(m => m.Channel == MessageChannel.Sms, ct)
        };

        var vm = new AllMessagesVm
        {
            Messages = messages,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            ChannelFilter = channel,
            StatusFilter = status,
            SearchQuery = search,
            FromDate = fromDate,
            ToDate = toDate,
            Statistics = stats
        };

        ViewBag.Title = "All Messages";
        return View(vm);
    }

    /// <summary>
    /// View queued message details
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ViewQueued(Guid id, CancellationToken ct)
    {
        var message = await _db.Set<QueuedMessage>()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (message == null)
        {
            return NotFound();
        }

        var vm = new QueuedMessageDetailVm
        {
            Id = message.Id,
            RecipientEmail = message.RecipientEmail,
            RecipientPhone = message.RecipientPhone,
            Subject = message.Subject,
            Body = message.Body,
            Channel = message.Channel,
            Status = message.Status,
            QueuedAtUtc = message.QueuedAtUtc,
            SentAtUtc = message.SentAtUtc,
            FailedAtUtc = message.FailedAtUtc,
            ErrorMessage = message.ErrorMessage,
            RetryCount = message.RetryCount,
            RelatedEntityType = message.RelatedEntityType,
            RelatedEntityId = message.RelatedEntityId,
            ProviderMessageId = message.ProviderMessageId
        };

        ViewBag.Title = "Message Details";
        return View(vm);
    }
}

#region View Models

public class InboxVm
{
    public List<InboxMessageVm> Messages { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public string? StatusFilter { get; set; }
    public string? PriorityFilter { get; set; }
    public string? CategoryFilter { get; set; }
    public int UnreadCount { get; set; }
}

public class InboxMessageVm
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessagePriority Priority { get; set; }
    public string? Category { get; set; }
    public DateTime SentAtUtc { get; set; }
    public InternalMessageStatus Status { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
}

public class MessageDetailVm
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessagePriority Priority { get; set; }
    public string? Category { get; set; }
    public DateTime SentAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public InternalMessageStatus Status { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public bool IsSystemGenerated { get; set; }
}

public class AllMessagesVm
{
    public List<QueuedMessageVm> Messages { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public string? ChannelFilter { get; set; }
    public string? StatusFilter { get; set; }
    public string? SearchQuery { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public MessageStatisticsVm Statistics { get; set; } = new();
}

public class QueuedMessageVm
{
    public Guid Id { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string? RecipientPhone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public MessageChannel Channel { get; set; }
    public QueuedMessageStatus Status { get; set; }
    public DateTime QueuedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? FailedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
}

public class QueuedMessageDetailVm
{
    public Guid Id { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string? RecipientPhone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public MessageChannel Channel { get; set; }
    public QueuedMessageStatus Status { get; set; }
    public DateTime QueuedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? FailedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? ProviderMessageId { get; set; }
}

public class MessageStatisticsVm
{
    public int TotalQueued { get; set; }
    public int TotalSent { get; set; }
    public int TotalFailed { get; set; }
    public int EmailCount { get; set; }
    public int SmsCount { get; set; }
}

#endregion

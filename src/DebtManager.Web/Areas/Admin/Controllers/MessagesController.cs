using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DebtManager.Infrastructure.Persistence;
using DebtManager.Domain.Communications;
using System.Security.Claims;
using DebtManager.Web.Services;
using System.ComponentModel.DataAnnotations;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "RequireAdminScope")]
public class MessagesController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<MessagesController> _logger;
    private readonly IMessageQueueService _mq;

    public MessagesController(AppDbContext db, ILogger<MessagesController> logger, IMessageQueueService mq)
    {
        _db = db;
        _logger = logger;
        _mq = mq;
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

    /// <summary>
    /// Compose a new message
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Compose(CancellationToken ct)
    {
        var vm = new ComposeMessageVm();
        ViewBag.Title = "Compose Message";
        return View(vm);
    }

    /// <summary>
    /// Send a composed message
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Compose(ComposeMessageVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Title = "Compose Message";
            return View(vm);
        }

        var recipientUserIds = new List<Guid>();

        // Fan-out: by role
        if (vm.SendToAdmins)
        {
            var adminRoleId = await _db.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstOrDefaultAsync(ct);
            if (adminRoleId != Guid.Empty)
            {
                var ids = await _db.UserRoles.Where(ur => ur.RoleId == adminRoleId).Select(ur => ur.UserId).ToListAsync(ct);
                recipientUserIds.AddRange(ids);
            }
        }
        if (vm.SendToClients)
        {
            var roleId = await _db.Roles.Where(r => r.Name == "Client").Select(r => r.Id).FirstOrDefaultAsync(ct);
            if (roleId != Guid.Empty)
            {
                var ids = await _db.UserRoles.Where(ur => ur.RoleId == roleId).Select(ur => ur.UserId).ToListAsync(ct);
                recipientUserIds.AddRange(ids);
            }
        }
        if (vm.SendToUsers)
        {
            var roleId = await _db.Roles.Where(r => r.Name == "User").Select(r => r.Id).FirstOrDefaultAsync(ct);
            if (roleId != Guid.Empty)
            {
                var ids = await _db.UserRoles.Where(ur => ur.RoleId == roleId).Select(ur => ur.UserId).ToListAsync(ct);
                recipientUserIds.AddRange(ids);
            }
        }

        // Explicit user IDs
        if (!string.IsNullOrWhiteSpace(vm.RecipientUserIdsCsv))
        {
            foreach (var token in vm.RecipientUserIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Guid.TryParse(token, out var id)) recipientUserIds.Add(id);
            }
        }

        recipientUserIds = recipientUserIds.Distinct().ToList();
        if (recipientUserIds.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Please choose at least one recipient.");
            ViewBag.Title = "Compose Message";
            return View(vm);
        }

        // Queue internal message
        await _mq.QueueInternalAsync(
            title: vm.Title!,
            content: vm.Content!,
            recipientUserIds: recipientUserIds,
            priority: vm.Priority,
            category: vm.Category,
            relatedEntityType: vm.RelatedEntityType,
            relatedEntityId: vm.RelatedEntityId,
            ct: ct
        );

        // Optionally also send email/SMS
        if (vm.SendEmail)
        {
            var emails = await _db.Users.Where(u => recipientUserIds.Contains(u.Id)).Select(u => u.Email!).ToListAsync(ct);
            await _mq.QueueEmailAsync(subject: vm.Title!, body: vm.Content!, recipientEmails: emails, relatedEntityType: vm.RelatedEntityType, relatedEntityId: vm.RelatedEntityId, ct: ct);
        }
        if (vm.SendSms)
        {
            var phones = await _db.Users.Where(u => recipientUserIds.Contains(u.Id)).Select(u => u.PhoneNumber!).ToListAsync(ct);
            await _mq.QueueSmsAsync(body: vm.Content!, recipientPhones: phones, relatedEntityType: vm.RelatedEntityType, relatedEntityId: vm.RelatedEntityId, ct: ct);
        }

        TempData["Message"] = "Message queued.";
        return RedirectToAction(nameof(Inbox));
    }

    /// <summary>
    /// Reply to a message
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply([FromForm] ReplyMessageVm vm, CancellationToken ct)
    {
        if (vm == null || vm.Id == Guid.Empty || string.IsNullOrWhiteSpace(vm.Content))
        {
            TempData["Error"] = "Reply content is required.";
            return RedirectToAction(nameof(View), new { id = vm?.Id });
        }

        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserIdStr) || !Guid.TryParse(currentUserIdStr, out var currentUserId))
        {
            return Unauthorized();
        }

        var original = await _db.InternalMessages
            .Include(m => m.Recipients)
            .FirstOrDefaultAsync(m => m.Id == vm.Id, ct);

        if (original == null)
        {
            return NotFound();
        }

        // Participant-only enforcement
        var isParticipant = (original.SenderId.HasValue && original.SenderId.Value == currentUserId)
            || original.Recipients.Any(r => r.UserId == currentUserId);
        if (!isParticipant)
        {
            return Forbid();
        }

        // Determine recipients
        var isAdmin = User.IsInRole("Admin");
        var recipients = new HashSet<Guid>();
        if (isAdmin)
        {
            if (original.SenderId.HasValue && original.SenderId.Value != currentUserId)
                recipients.Add(original.SenderId.Value);
            foreach (var r in original.Recipients)
            {
                if (r.UserId != currentUserId) recipients.Add(r.UserId);
            }
        }
        else
        {
            // Non-admin: reply only to originator (admin)
            if (original.SenderId.HasValue && original.SenderId.Value != currentUserId)
            {
                recipients.Add(original.SenderId.Value);
            }
            else
            {
                TempData["Error"] = "You can only reply to the message originator.";
                return RedirectToAction(nameof(View), new { id = vm.Id });
            }
        }

        if (recipients.Count == 0)
        {
            TempData["Error"] = "No valid recipients for reply.";
            return RedirectToAction(nameof(View), new { id = vm.Id });
        }

        await _mq.QueueInternalAsync(
            title: $"RE: {original.Title}",
            content: vm.Content!,
            recipientUserIds: recipients,
            priority: original.Priority,
            category: original.Category,
            relatedEntityType: original.RelatedEntityType,
            relatedEntityId: original.RelatedEntityId,
            senderId: currentUserId,
            systemGenerated: false,
            ct: ct);

        TempData["Message"] = "Reply sent.";
        return RedirectToAction(nameof(View), new { id = vm.Id });
    }

    /// <summary>
    /// Mark message as read/unread (AJAX)
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MarkReadAjax([FromBody] Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }
        var rec = await _db.InternalMessageRecipients.FirstOrDefaultAsync(r => r.InternalMessageId == id && r.UserId == userGuid, ct);
        if (rec == null) return NotFound();
        if (rec.Status == InternalMessageStatus.Unread) rec.MarkAsRead();
        await _db.SaveChangesAsync(ct);
        var unread = await _db.InternalMessageRecipients.CountAsync(r => r.UserId == userGuid && r.Status == InternalMessageStatus.Unread, ct);
        return Json(new { ok = true, unread });
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

public class ComposeMessageVm
{
    [Required]
    [StringLength(200)]
    public string? Title { get; set; }

    [Required]
    [StringLength(10000)]
    public string? Content { get; set; }

    public MessagePriority Priority { get; set; } = MessagePriority.Normal;
    public string? Category { get; set; }

    // Targeting
    public bool SendToAdmins { get; set; }
    public bool SendToClients { get; set; }
    public bool SendToUsers { get; set; }
    public string? RecipientUserIdsCsv { get; set; }

    // Delivery channels
    public bool SendEmail { get; set; }
    public bool SendSms { get; set; }

    // Optional linking
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
}

public class ReplyMessageVm
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [StringLength(10000)]
    public string? Content { get; set; }
}

#endregion

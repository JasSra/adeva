using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DebtManager.Infrastructure.Persistence;
using DebtManager.Domain.Communications;
using System.Security.Claims;
using DebtManager.Web.Services;
using System.ComponentModel.DataAnnotations;

namespace DebtManager.Web.Areas.User.Controllers;

[Area("User")]
[Authorize(Policy = "RequireUserScope")]
public class MessagesController : Controller
{
    private readonly AppDbContext _db;
    private readonly IMessageQueueService _mq;

    public MessagesController(AppDbContext db, IMessageQueueService mq)
    {
        _db = db;
        _mq = mq;
    }

    [HttpGet]
    public async Task<IActionResult> Inbox(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();
        var query = _db.InternalMessageRecipients.Include(r => r.InternalMessage).Where(r => r.UserId == userId);
        var items = await query.OrderByDescending(r => r.InternalMessage!.SentAtUtc)
            .Skip((page-1)*pageSize).Take(pageSize)
            .Select(r => new UserInboxItemVm
            {
                Id = r.InternalMessageId,
                Title = r.InternalMessage!.Title,
                Preview = r.InternalMessage.Content.Length > 140 ? r.InternalMessage.Content.Substring(0,140)+"…" : r.InternalMessage.Content,
                SentAtUtc = r.InternalMessage.SentAtUtc,
                Status = r.Status
            }).ToListAsync(ct);
        ViewBag.Title = "Inbox";
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> View(Guid id, CancellationToken ct)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();
        var rec = await _db.InternalMessageRecipients.Include(r => r.InternalMessage)
            .FirstOrDefaultAsync(r => r.InternalMessageId == id && r.UserId == userId, ct);
        if (rec == null) return NotFound();
        if (rec.Status == InternalMessageStatus.Unread) { rec.MarkAsRead(); await _db.SaveChangesAsync(ct); }
        var vm = new UserMessageDetailVm
        {
            Id = id,
            Title = rec.InternalMessage!.Title,
            Content = rec.InternalMessage.Content,
            SentAtUtc = rec.InternalMessage.SentAtUtc,
            ReadAtUtc = rec.ReadAtUtc,
            Status = rec.Status
        };
        ViewBag.Title = "Message";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply([FromForm] UserReplyVm vm, CancellationToken ct)
    {
        if (vm == null || vm.Id == Guid.Empty || string.IsNullOrWhiteSpace(vm.Content))
        {
            TempData["Error"] = "Reply content is required.";
            return RedirectToAction(nameof(View), new { id = vm?.Id });
        }
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();
        var original = await _db.InternalMessages.Include(m => m.Recipients).FirstOrDefaultAsync(m => m.Id == vm.Id, ct);
        if (original == null) return NotFound();
        var isParticipant = (original.SenderId.HasValue && original.SenderId.Value == userId) || original.Recipients.Any(r => r.UserId == userId);
        if (!isParticipant) return Forbid();
        var recipients = new List<Guid>();
        if (original.SenderId.HasValue && original.SenderId.Value != userId) recipients.Add(original.SenderId.Value);
        if (recipients.Count == 0)
        {
            TempData["Error"] = "You can only reply to the message originator.";
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
            senderId: userId,
            systemGenerated: false,
            ct: ct);
        TempData["Message"] = "Reply sent.";
        return RedirectToAction(nameof(View), new { id = vm.Id });
    }
}

public class UserInboxItemVm
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Preview { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
    public InternalMessageStatus Status { get; set; }
}

public class UserMessageDetailVm
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public InternalMessageStatus Status { get; set; }
}

public class UserReplyVm
{
    [Required]
    public Guid Id { get; set; }
    [Required, StringLength(10000)]
    public string? Content { get; set; }
}

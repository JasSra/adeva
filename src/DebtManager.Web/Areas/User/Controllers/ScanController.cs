using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DebtManager.Contracts.Documents;
using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Documents;
using DebtManager.Infrastructure.Identity;
using DebtManager.Infrastructure.Persistence;
using DebtManager.Web.Filters;
using DebtManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Areas.User.Controllers;

[Area("User")]
[Authorize(Policy = "RequireUserScope")]
[RequireDebtorOnboarded]
public class ScanController : Controller
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/jpeg", "image/png", "image/jpg", "image/heic", "image/heif"
    };

    private const long MaxFileBytes = 15 * 1024 * 1024; // 15 MB

    private readonly IDocumentRepository _documents;
    private readonly IInvoiceProcessingService _invoiceProcessing;
    private readonly IInvoiceDataRepository _invoiceDataRepo;
    private readonly AppDbContext _db;

    public ScanController(
        IDocumentRepository documents,
        IInvoiceProcessingService invoiceProcessing,
        IInvoiceDataRepository invoiceDataRepo,
        AppDbContext db)
    {
        _documents = documents;
        _invoiceProcessing = invoiceProcessing;
        _invoiceDataRepo = invoiceDataRepo;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Upload Documents";

        var debtorId = await GetCurrentDebtorIdAsync(ct);
        if (debtorId == null)
        {
            // Safety net, RequireDebtorOnboarded should already ensure this
            return Redirect("/User/Onboarding");
        }

        var docs = await _documents.GetByDebtorAsync(debtorId.Value, ct);
        var items = new List<ScanItemVm>();
        foreach (var d in docs.Take(50))
        {
            var inv = await _invoiceDataRepo.GetByDocumentIdAsync(d.Id, ct);
            items.Add(new ScanItemVm
            {
                DocumentId = d.Id,
                FileName = d.FileName,
                Type = d.Type,
                SizeBytes = d.SizeBytes,
                CreatedAtUtc = d.CreatedAtUtc,
                StorageUrl = d.StoragePath,
                InvoiceStatus = inv?.Status.ToString(),
                InvoiceNumber = inv?.InvoiceNumber,
                Confidence = inv?.ConfidenceScore
            });
        }

        var vm = new ScanIndexVm
        {
            Items = items
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(ScanUploadVm vm, IFormFile? file, CancellationToken ct)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Upload Documents";

        if (vm.DocumentType == DocumentType.Invoice)
        {
            ModelState.AddModelError(nameof(vm.DocumentType), "Debtors cannot upload invoices. Please choose Identity, Evidence, or Receipt.");
        }

        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("file", "Please choose a file to upload.");
        }
        else
        {
            if (file.Length > MaxFileBytes)
                ModelState.AddModelError("file", $"File is too large. Maximum allowed size is {MaxFileBytes / (1024 * 1024)} MB.");

            if (!AllowedContentTypes.Contains(file.ContentType))
                ModelState.AddModelError("file", "Unsupported file type. Allowed: PDF, JPEG, PNG, HEIC/HEIF.");
        }

        if (!ModelState.IsValid)
        {
            // redisplay index with errors
            var debtorIdErr = await GetCurrentDebtorIdAsync(ct);
            var docsErr = debtorIdErr != null ? await _documents.GetByDebtorAsync(debtorIdErr.Value, ct) : Array.Empty<Document>();
            var itemsErr = new List<ScanItemVm>();
            foreach (var d in docsErr.Take(50))
            {
                var inv = await _invoiceDataRepo.GetByDocumentIdAsync(d.Id, ct);
                itemsErr.Add(new ScanItemVm
                {
                    DocumentId = d.Id,
                    FileName = d.FileName,
                    Type = d.Type,
                    SizeBytes = d.SizeBytes,
                    CreatedAtUtc = d.CreatedAtUtc,
                    StorageUrl = d.StoragePath,
                    InvoiceStatus = inv?.Status.ToString(),
                    InvoiceNumber = inv?.InvoiceNumber,
                    Confidence = inv?.ConfidenceScore
                });
            }
            return View("Index", new ScanIndexVm { Items = itemsErr });
        }

        var debtorId = await GetCurrentDebtorIdAsync(ct);
        if (debtorId == null)
        {
            return Redirect("/User/Onboarding");
        }

        try
        {
            // Ensure uploads folder exists
            var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsRoot);

            var safeFileName = Path.GetFileName(file!.FileName);
            var uniqueName = $"{Guid.NewGuid():N}_{safeFileName}";
            var diskPath = Path.Combine(uploadsRoot, uniqueName);
            await using (var stream = System.IO.File.Create(diskPath))
            {
                await file.CopyToAsync(stream, ct);
            }

            // Public URL (dev-friendly)
            var publicUrl = $"{Request.Scheme}://{Request.Host}/uploads/{uniqueName}";

            // Create document entity
            var doc = new Document(
                fileName: safeFileName,
                contentType: file.ContentType,
                sizeBytes: file.Length,
                type: vm.DocumentType,
                storagePath: publicUrl,
                sha256: null,
                organizationId: null,
                debtorId: debtorId
            );

            await _documents.AddAsync(doc, ct);
            await _documents.SaveChangesAsync(ct);

            // For receipts we may add further processing later; invoices are disallowed for users
            TempData["Message"] = "File uploaded successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Failed to upload file. Please try again.";
            ModelState.AddModelError(string.Empty, ex.Message);
            // Fall through to redisplay index with existing docs
            var docsErr = await _documents.GetByDebtorAsync(debtorId.Value, ct);
            var itemsErr = new List<ScanItemVm>();
            foreach (var d in docsErr.Take(50))
            {
                var inv = await _invoiceDataRepo.GetByDocumentIdAsync(d.Id, ct);
                itemsErr.Add(new ScanItemVm
                {
                    DocumentId = d.Id,
                    FileName = d.FileName,
                    Type = d.Type,
                    SizeBytes = d.SizeBytes,
                    CreatedAtUtc = d.CreatedAtUtc,
                    StorageUrl = d.StoragePath,
                    InvoiceStatus = inv?.Status.ToString(),
                    InvoiceNumber = inv?.InvoiceNumber,
                    Confidence = inv?.ConfidenceScore
                });
            }
            return View("Index", new ScanIndexVm { Items = itemsErr });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Status(Guid documentId, CancellationToken ct)
    {
        var inv = await _invoiceDataRepo.GetByDocumentIdAsync(documentId, ct);
        if (inv == null) return Json(new { status = "stored" });
        return Json(new
        {
            status = inv.Status.ToString(),
            invoiceNumber = inv.InvoiceNumber,
            confidence = inv.ConfidenceScore,
            processedAt = inv.ProcessedAtUtc
        });
    }

    private async Task<Guid?> GetCurrentDebtorIdAsync(CancellationToken ct)
    {
        var externalId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(externalId)) return null;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.ExternalAuthId == externalId, ct);
        if (user == null) return null;
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        return profile?.DebtorId;
    }
}

public class ScanIndexVm
{
    public List<ScanItemVm> Items { get; set; } = new();
}

public class ScanItemVm
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DocumentType Type { get; set; }
    public long SizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string StorageUrl { get; set; } = string.Empty;
    public string? InvoiceStatus { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal? Confidence { get; set; }
}

public class ScanUploadVm
{
    [Display(Name = "Document Type")]
    [Required]
    public DocumentType DocumentType { get; set; } = DocumentType.Evidence;
}

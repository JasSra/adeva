using DebtManager.Contracts.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "RequireAdminScope")]
public class DocumentsController : Controller
{
    private readonly IDocumentGenerationService _documentService;

    public DocumentsController(IDocumentGenerationService documentService)
    {
        _documentService = documentService;
    }

    [HttpPost]
    public async Task<IActionResult> GenerateRemittanceInvoice([FromBody] RemittanceInvoiceRequest request)
    {
        var invoiceData = new InvoiceGenerationData
        {
            InvoiceNumber = request.InvoiceNumber ?? $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            IssuedDate = request.IssuedDate ?? DateTime.UtcNow,
            DueDate = request.DueDate,
            OrganizationName = request.OrganizationName,
            OrganizationAbn = request.OrganizationAbn,
            OrganizationAddress = request.OrganizationAddress,
            OrganizationLogo = request.OrganizationLogo,
            Subtotal = request.Subtotal,
            TaxAmount = request.TaxAmount,
            Total = request.Subtotal + request.TaxAmount,
            Currency = request.Currency ?? "AUD",
            Description = request.Description ?? "Remittance for debt collection services",
            Notes = request.Notes,
            TermsAndConditions = request.TermsAndConditions ?? "Payment is due within 30 days of invoice date. All amounts are in Australian Dollars (AUD). Platform fees are calculated as per the service agreement.",
            LineItems = request.LineItems ?? new List<InvoiceLineItem>(),
            PrimaryColor = request.PrimaryColor ?? "#0066cc",
            PaymentInstructions = request.PaymentInstructions
        };

        // Send email if requested
        if (!string.IsNullOrEmpty(request.SendToEmail))
        {
            await _documentService.SendInvoiceEmailAsync(invoiceData, request.SendToEmail);
        }

        if (request.Format?.ToLower() == "pdf")
        {
            var pdf = await _documentService.GenerateInvoicePdfAsync(invoiceData);
            return File(pdf, "application/pdf", $"{invoiceData.InvoiceNumber}.pdf");
        }

        var html = await _documentService.GenerateInvoiceHtmlAsync(invoiceData);
        return Content(html, "text/html");
    }

    [HttpPost]
    public async Task<IActionResult> GeneratePaymentReceipt([FromBody] PaymentReceiptRequest request)
    {
        var receiptData = new ReceiptGenerationData
        {
            ReceiptNumber = request.ReceiptNumber ?? $"RCP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            IssuedDate = request.IssuedDate ?? DateTime.UtcNow,
            Amount = request.Amount,
            Currency = request.Currency ?? "AUD",
            PaymentMethod = request.PaymentMethod,
            ReferenceNumber = request.ReferenceNumber,
            DebtorName = request.DebtorName,
            DebtorEmail = request.DebtorEmail,
            DebtorAddress = request.DebtorAddress,
            DebtReference = request.DebtReference,
            OrganizationName = request.OrganizationName,
            OrganizationLogo = request.OrganizationLogo,
            OrganizationAddress = request.OrganizationAddress,
            OrganizationPhone = request.OrganizationPhone,
            OrganizationEmail = request.OrganizationEmail,
            Notes = request.Notes,
            PrimaryColor = request.PrimaryColor ?? "#0066cc",
            IsPartialPayment = request.IsPartialPayment,
            RemainingBalance = request.RemainingBalance
        };

        // Send email if requested
        if (!string.IsNullOrEmpty(request.SendToEmail))
        {
            await _documentService.SendReceiptEmailAsync(receiptData, request.SendToEmail);
        }

        if (request.Format?.ToLower() == "pdf")
        {
            var pdf = await _documentService.GenerateReceiptPdfAsync(receiptData);
            return File(pdf, "application/pdf", $"{receiptData.ReceiptNumber}.pdf");
        }

        var html = await _documentService.GenerateReceiptHtmlAsync(receiptData);
        return Content(html, "text/html");
    }

    [HttpPost]
    public async Task<IActionResult> SendBatchReceipts([FromBody] BatchReceiptRequest request)
    {
        var receipts = request.Recipients.Select(r => (
            new ReceiptGenerationData
            {
                ReceiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                IssuedDate = DateTime.UtcNow,
                Amount = r.Amount,
                Currency = "AUD",
                PaymentMethod = r.PaymentMethod,
                DebtorName = r.DebtorName,
                DebtorEmail = r.Email,
                DebtReference = r.DebtReference,
                OrganizationName = request.OrganizationName,
                PrimaryColor = request.PrimaryColor ?? "#0066cc",
                IsPartialPayment = r.IsPartialPayment,
                RemainingBalance = r.RemainingBalance
            },
            r.Email
        )).ToList();

        await _documentService.SendBatchReceiptEmailsAsync(receipts);

        return Ok(new { message = $"Sent {receipts.Count} receipts successfully" });
    }

    public IActionResult Index()
    {
        ViewBag.Title = "Document Generation";
        return View();
    }

    public IActionResult ViewReceipts()
    {
        ViewBag.Title = "View Receipts";
        return View();
    }

    public IActionResult ViewInvoices()
    {
        ViewBag.Title = "View Invoices";
        return View();
    }
}

public class RemittanceInvoiceRequest
{
    public string? InvoiceNumber { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public string? Currency { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string? OrganizationAbn { get; set; }
    public string? OrganizationAddress { get; set; }
    public string? OrganizationLogo { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? TermsAndConditions { get; set; }
    public List<InvoiceLineItem>? LineItems { get; set; }
    public string? PrimaryColor { get; set; }
    public string? PaymentInstructions { get; set; }
    public string? Format { get; set; }
    public string? SendToEmail { get; set; }
}

public class PaymentReceiptRequest
{
    public string? ReceiptNumber { get; set; }
    public DateTime? IssuedDate { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string DebtorName { get; set; } = string.Empty;
    public string? DebtorEmail { get; set; }
    public string? DebtorAddress { get; set; }
    public string? DebtReference { get; set; }
    public string? OrganizationName { get; set; }
    public string? OrganizationLogo { get; set; }
    public string? OrganizationAddress { get; set; }
    public string? OrganizationPhone { get; set; }
    public string? OrganizationEmail { get; set; }
    public string? Notes { get; set; }
    public string? PrimaryColor { get; set; }
    public bool IsPartialPayment { get; set; }
    public decimal? RemainingBalance { get; set; }
    public string? Format { get; set; }
    public string? SendToEmail { get; set; }
}

public class BatchReceiptRequest
{
    public string OrganizationName { get; set; } = string.Empty;
    public string? PrimaryColor { get; set; }
    public List<BatchReceiptRecipient> Recipients { get; set; } = new();
}

public class BatchReceiptRecipient
{
    public string DebtorName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? DebtReference { get; set; }
    public bool IsPartialPayment { get; set; }
    public decimal? RemainingBalance { get; set; }
}

using Microsoft.AspNetCore.Mvc;
using DebtManager.Contracts.Persistence;
using DebtManager.Contracts.Documents;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class InvoiceProcessingController(
    IInvoiceDataRepository invoiceDataRepository,
    IDocumentRepository documentRepository,
    IInvoiceProcessingService invoiceProcessingService) : Controller
{
    public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
    {
        // For simplicity, get pending invoices
        var pendingInvoices = await invoiceDataRepository.GetPendingAsync();
        
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        
        return View(pendingInvoices);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var invoiceData = await invoiceDataRepository.GetAsync(id);
        if (invoiceData == null)
        {
            return NotFound();
        }

        return View(invoiceData);
    }

    [HttpPost]
    public async Task<IActionResult> Retry(Guid id)
    {
        var invoiceData = await invoiceDataRepository.GetAsync(id);
        if (invoiceData == null)
        {
            return NotFound();
        }

        // Queue for reprocessing
        await invoiceProcessingService.QueueInvoiceProcessingAsync(invoiceData.DocumentId);

        TempData["Message"] = "Invoice queued for reprocessing";
        return RedirectToAction(nameof(Details), new { id });
    }
}

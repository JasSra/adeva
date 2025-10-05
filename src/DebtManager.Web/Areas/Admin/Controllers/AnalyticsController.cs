using Microsoft.AspNetCore.Mvc;
using DebtManager.Contracts.Persistence;
using DebtManager.Contracts.Analytics;
using DebtManager.Domain.Analytics;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class AnalyticsController(IMetricService metricService, IMetricRepository metricRepository) : Controller
{
    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, Guid? organizationId)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var metrics = await metricService.GetAggregatedMetricsAsync(from, to, organizationId);

        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.OrganizationId = organizationId;
        ViewBag.Metrics = metrics;

        return View(metrics);
    }

    public async Task<IActionResult> InvoiceMetrics(DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var successMetrics = await metricService.GetMetricsByKeyAsync("invoice.extraction.success", from, to);
        var failureMetrics = await metricService.GetMetricsByKeyAsync("invoice.extraction.failure", from, to);
        var processingMetrics = await metricService.GetMetricsByKeyAsync("invoice.processing.completed", from, to);

        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.SuccessCount = successMetrics.Sum(m => m.Value);
        ViewBag.FailureCount = failureMetrics.Sum(m => m.Value);
        ViewBag.ProcessingCount = processingMetrics.Sum(m => m.Value);

        return View();
    }
}

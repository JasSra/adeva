# Invoice Processing Integration Examples

## Example 1: Upload and Queue Invoice Processing

```csharp
// In a Client/Debtor controller or service
public async Task<IActionResult> UploadInvoice(IFormFile file, Guid? debtorId, Guid? organizationId)
{
    // 1. Save document to storage
    var storagePath = await SaveToStorageAsync(file);
    
    // 2. Create Document entity
    var document = new Document(
        fileName: file.FileName,
        contentType: file.ContentType,
        sizeBytes: file.Length,
        type: DocumentType.Invoice,
        storagePath: storagePath,
        sha256: ComputeSha256(file),
        organizationId: organizationId,
        debtorId: debtorId
    );
    
    await _documentRepository.AddAsync(document);
    await _documentRepository.SaveChangesAsync();
    
    // 3. Queue for processing (if it's an invoice)
    if (document.Type == DocumentType.Invoice)
    {
        await _invoiceProcessingService.QueueInvoiceProcessingAsync(document.Id);
        
        TempData["Message"] = "Invoice uploaded and queued for processing";
    }
    
    return RedirectToAction("Index");
}
```

## Example 2: Immediate Synchronous Processing

```csharp
// For scenarios where you need immediate results
public async Task<IActionResult> ProcessInvoiceNow(Guid documentId)
{
    var result = await _invoiceProcessingService.ExtractInvoiceDataAsync(documentId);
    
    if (result.Success)
    {
        // Create InvoiceData record with results
        var invoiceData = InvoiceData.Create(documentId);
        invoiceData.MarkAsCompleted(
            result.InvoiceNumber,
            result.InvoiceDate,
            result.DueDate,
            result.TotalAmount,
            result.Currency,
            result.VendorName,
            result.VendorAddress,
            result.VendorAbn,
            result.CustomerName,
            result.CustomerAddress,
            JsonSerializer.Serialize(result.AdditionalFields),
            result.ConfidenceScore
        );
        
        await _invoiceDataRepository.AddAsync(invoiceData);
        await _invoiceDataRepository.SaveChangesAsync();
        
        return Json(new { success = true, data = result });
    }
    else
    {
        return Json(new { success = false, error = result.ErrorMessage });
    }
}
```

## Example 3: Auto-Create Debt from Invoice

```csharp
// Automatically create a debt when invoice processing completes
public async Task ProcessCompletedInvoice(Guid invoiceDataId)
{
    var invoiceData = await _invoiceDataRepository.GetAsync(invoiceDataId);
    
    if (invoiceData?.Status == InvoiceProcessingStatus.Completed)
    {
        var document = invoiceData.Document;
        
        // Find or create debtor
        var debtor = document.DebtorId.HasValue 
            ? await _debtorRepository.GetAsync(document.DebtorId.Value)
            : await FindOrCreateDebtorAsync(invoiceData.CustomerName, invoiceData.CustomerAddress);
        
        // Create debt from invoice data
        var debt = new Debt(
            debtorId: debtor.Id,
            organizationId: document.OrganizationId!.Value,
            originalPrincipal: invoiceData.TotalAmount ?? 0,
            description: $"Invoice {invoiceData.InvoiceNumber}",
            clientReferenceNumber: invoiceData.InvoiceNumber,
            dueDate: invoiceData.DueDate
        );
        
        await _debtRepository.AddAsync(debt);
        await _debtRepository.SaveChangesAsync();
        
        // Record metric
        await _metricService.RecordMetricAsync(
            "debt.auto_created_from_invoice",
            MetricType.Counter,
            1,
            $"invoice:{invoiceData.InvoiceNumber}",
            document.OrganizationId
        );
    }
}
```

## Example 4: Webhook Handler for Processing Completion

```csharp
// Hangfire job that runs after processing
[AutomaticRetry(Attempts = 0)]
public async Task OnInvoiceProcessingComplete(Guid invoiceDataId)
{
    var invoiceData = await _invoiceDataRepository.GetAsync(invoiceDataId);
    
    if (invoiceData == null) return;
    
    // Send notification
    if (invoiceData.Status == InvoiceProcessingStatus.Completed)
    {
        await _emailSender.SendEmailAsync(
            to: "admin@example.com",
            subject: $"Invoice {invoiceData.InvoiceNumber} processed",
            body: $"Invoice processing completed with {invoiceData.ConfidenceScore:P} confidence."
        );
    }
    else if (invoiceData.Status == InvoiceProcessingStatus.ManualReviewRequired)
    {
        await _emailSender.SendEmailAsync(
            to: "admin@example.com",
            subject: "Invoice requires manual review",
            body: $"Invoice processing needs review: {invoiceData.ErrorMessage}"
        );
    }
}
```

## Example 5: Bulk Invoice Processing

```csharp
// Process multiple invoices in batch
public async Task<IActionResult> BulkUploadInvoices(List<IFormFile> files, Guid organizationId)
{
    var results = new List<(string fileName, Guid invoiceDataId)>();
    
    foreach (var file in files)
    {
        // Save document
        var storagePath = await SaveToStorageAsync(file);
        var document = new Document(
            file.FileName,
            file.ContentType,
            file.Length,
            DocumentType.Invoice,
            storagePath,
            null,
            organizationId,
            null
        );
        
        await _documentRepository.AddAsync(document);
        await _documentRepository.SaveChangesAsync();
        
        // Queue for processing
        var invoiceDataId = await _invoiceProcessingService.QueueInvoiceProcessingAsync(document.Id);
        results.Add((file.FileName, invoiceDataId));
    }
    
    // Record bulk upload metric
    await _metricService.RecordMetricAsync(
        "invoice.bulk_upload",
        MetricType.Counter,
        files.Count,
        $"organization:{organizationId}",
        organizationId
    );
    
    return Json(new { 
        success = true, 
        message = $"Uploaded {files.Count} invoices for processing",
        results 
    });
}
```

## Example 6: Custom Metrics Recording

```csharp
// Record custom business metrics
public async Task RecordBusinessMetrics(Guid organizationId)
{
    var fromDate = DateTime.UtcNow.AddDays(-30);
    var toDate = DateTime.UtcNow;
    
    // Get processed invoices count
    var processedCount = await _invoiceDataRepository
        .GetByOrganizationAsync(organizationId, fromDate, toDate)
        .ContinueWith(t => t.Result.Count(i => i.Status == InvoiceProcessingStatus.Completed));
    
    // Record as gauge metric
    await _metricService.RecordMetricAsync(
        "invoice.monthly_processed",
        MetricType.Gauge,
        processedCount,
        "period:30days",
        organizationId
    );
    
    // Calculate average confidence score
    var avgConfidence = await _invoiceDataRepository
        .GetByOrganizationAsync(organizationId, fromDate, toDate)
        .ContinueWith(t => t.Result
            .Where(i => i.ConfidenceScore.HasValue)
            .Average(i => i.ConfidenceScore!.Value));
    
    await _metricService.RecordMetricAsync(
        "invoice.avg_confidence",
        MetricType.Gauge,
        avgConfidence,
        "period:30days",
        organizationId
    );
}
```

## Example 7: API Endpoint for External Systems

```csharp
// RESTful API for external systems to submit invoices
[ApiController]
[Route("api/invoices")]
public class InvoicesApiController : ControllerBase
{
    private readonly IInvoiceProcessingService _invoiceProcessingService;
    private readonly IDocumentRepository _documentRepository;
    
    [HttpPost("process")]
    public async Task<IActionResult> ProcessInvoice(
        [FromForm] IFormFile file,
        [FromForm] Guid organizationId,
        [FromForm] Guid? debtorId)
    {
        // Validate file
        if (file == null || file.Length == 0)
            return BadRequest("File is required");
        
        if (!IsValidInvoiceFormat(file.ContentType))
            return BadRequest("Invalid file format. Supported: PDF, JPEG, PNG");
        
        try
        {
            // Save and process
            var storagePath = await SaveToStorageAsync(file);
            var document = new Document(
                file.FileName,
                file.ContentType,
                file.Length,
                DocumentType.Invoice,
                storagePath,
                null,
                organizationId,
                debtorId
            );
            
            await _documentRepository.AddAsync(document);
            await _documentRepository.SaveChangesAsync();
            
            var invoiceDataId = await _invoiceProcessingService
                .QueueInvoiceProcessingAsync(document.Id);
            
            return Ok(new
            {
                documentId = document.Id,
                invoiceDataId,
                status = "queued",
                message = "Invoice queued for processing"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    [HttpGet("status/{invoiceDataId}")]
    public async Task<IActionResult> GetStatus(Guid invoiceDataId)
    {
        var invoiceData = await _invoiceDataRepository.GetAsync(invoiceDataId);
        
        if (invoiceData == null)
            return NotFound();
        
        return Ok(new
        {
            id = invoiceData.Id,
            documentId = invoiceData.DocumentId,
            status = invoiceData.Status.ToString(),
            confidenceScore = invoiceData.ConfidenceScore,
            processedAt = invoiceData.ProcessedAtUtc,
            data = invoiceData.Status == InvoiceProcessingStatus.Completed 
                ? new
                {
                    invoiceData.InvoiceNumber,
                    invoiceData.InvoiceDate,
                    invoiceData.TotalAmount,
                    invoiceData.Currency,
                    invoiceData.VendorName,
                    invoiceData.CustomerName
                }
                : null,
            error = invoiceData.ErrorMessage
        });
    }
}
```

## Helper Methods

```csharp
private bool IsValidInvoiceFormat(string contentType)
{
    var validTypes = new[] { 
        "application/pdf", 
        "image/jpeg", 
        "image/png" 
    };
    return validTypes.Contains(contentType);
}

private string ComputeSha256(IFormFile file)
{
    using var sha256 = SHA256.Create();
    using var stream = file.OpenReadStream();
    var hash = sha256.ComputeHash(stream);
    return Convert.ToHexString(hash);
}

private async Task<string> SaveToStorageAsync(IFormFile file)
{
    // Implementation depends on storage provider
    // Example for Azure Blob Storage:
    var blobName = $"{Guid.NewGuid()}/{file.FileName}";
    var containerClient = _blobServiceClient.GetBlobContainerClient("invoices");
    await containerClient.CreateIfNotExistsAsync();
    var blobClient = containerClient.GetBlobClient(blobName);
    await blobClient.UploadAsync(file.OpenReadStream());
    return blobClient.Uri.ToString();
}
```

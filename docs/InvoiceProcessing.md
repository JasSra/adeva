# Invoice Processing Feature

## Overview

This feature provides automated invoice data extraction using AI/OCR (Azure Form Recognizer) with comprehensive fault tolerance, analytics, and admin management capabilities.

## Architecture

### Domain Entities

- **InvoiceData**: Stores extracted invoice information and processing status
- **Metric**: Records analytics data for monitoring and dashboards

### Processing Flow

1. **Upload**: User uploads invoice document (Document entity)
2. **Queue**: System creates InvoiceData record and queues Hangfire job
3. **Process**: Background worker extracts data using Azure Form Recognizer
4. **Retry**: Failed jobs automatically retry (max 3 attempts)
5. **Review**: Low confidence scores flagged for manual review

### Status Workflow

```
Pending → Processing → Completed
                    ↓ (on failure)
                   Failed → (retry) → Processing
                    ↓ (low confidence)
            ManualReviewRequired
```

## Configuration

### Azure Form Recognizer Setup

Add to database configuration (via Admin UI or database):

```
Key: AzureFormRecognizer:Endpoint
Value: https://your-resource.cognitiveservices.azure.com/

Key: AzureFormRecognizer:ApiKey
Value: your-api-key-here
```

### Stub Fallback

If Azure Form Recognizer is not configured, the system uses a stub implementation that returns placeholder data for testing.

## Hangfire Jobs

Three recurring jobs are configured:

1. **process-pending-invoices** (Hourly): Processes any pending invoices
2. **retry-failed-invoices** (Daily): Retries failed invoices that haven't exceeded max retries
3. **nightly-reminders** (Daily): Existing reminder job

## Admin Interface

### Analytics Dashboard

**Route**: `/Admin/Analytics/Index`

View aggregated metrics with date filtering:
- Invoice extraction success/failure rates
- Processing completion counts
- Custom metrics from other services

### Invoice Processing Metrics

**Route**: `/Admin/Analytics/InvoiceMetrics`

Dedicated dashboard for invoice-specific metrics:
- Successful extractions
- Failed extractions
- Completed processing jobs

### Invoice Queue

**Route**: `/Admin/InvoiceProcessing/Index`

Monitor all invoice processing jobs:
- View pending, processing, failed, and completed invoices
- See retry counts and timestamps
- Access individual invoice details

### Invoice Details

**Route**: `/Admin/InvoiceProcessing/Details/{id}`

View detailed information about a specific invoice:
- Processing status and confidence score
- Extracted data (invoice number, amounts, dates, vendor/customer details)
- Error messages for failed jobs
- Retry button for failed/manual review items

## API Usage

### Queue Invoice Processing

```csharp
var invoiceDataId = await _invoiceProcessingService.QueueInvoiceProcessingAsync(documentId);
```

### Direct Extraction (Synchronous)

```csharp
var result = await _invoiceProcessingService.ExtractInvoiceDataAsync(documentId);

if (result.Success)
{
    var invoiceNumber = result.InvoiceNumber;
    var totalAmount = result.TotalAmount;
    var vendorName = result.VendorName;
    // ... use extracted data
}
```

### Record Custom Metrics

```csharp
await _metricService.RecordMetricAsync(
    "custom.metric.key",
    MetricType.Counter,
    value: 1,
    tags: "source:api,type:upload",
    organizationId: orgId
);
```

### Query Metrics

```csharp
var metrics = await _metricService.GetAggregatedMetricsAsync(
    fromUtc: DateTime.UtcNow.AddDays(-7),
    toUtc: DateTime.UtcNow,
    organizationId: optionalOrgId
);
```

## Extracted Data Fields

### Standard Fields
- **InvoiceNumber**: Invoice/reference number
- **InvoiceDate**: Invoice issue date
- **DueDate**: Payment due date
- **TotalAmount**: Total invoice amount
- **Currency**: Currency code (default: AUD)
- **VendorName**: Supplier/vendor name
- **VendorAddress**: Vendor address
- **VendorAbn**: Vendor ABN (Australian Business Number)
- **CustomerName**: Customer/debtor name
- **CustomerAddress**: Customer address

### Additional Fields
Any other fields extracted by Azure Form Recognizer are stored as JSON in `ExtractedDataJson`.

## Error Handling & Fault Tolerance

### Automatic Retries
- Max 3 retry attempts per invoice
- Exponential backoff via Hangfire
- Failed jobs moved to dead-letter queue after max retries

### Low Confidence Detection
- Confidence scores below 70% trigger manual review
- Admins can review and retry these invoices

### Logging
All processing steps logged with structured logging:
- Document ID
- Processing status changes
- Error messages
- Metrics recorded

## Metrics Collected

### Invoice Processing Metrics
- `invoice.extraction.success` (Counter): Successful extractions
- `invoice.extraction.failure` (Counter): Failed extractions
- `invoice.processing.completed` (Counter): Completed processing jobs
- `invoice.processing.failed` (Counter): Failed processing jobs
- `invoice.processing.error` (Counter): Processing errors with error type

All metrics include:
- Timestamp
- Organization ID (when applicable)
- Tags (confidence scores, error types, etc.)

## Testing

### Unit Tests

Run invoice processing tests:
```bash
dotnet test --filter "FullyQualifiedName~InvoiceProcessingTests"
```

### Integration Testing

1. Upload a test invoice document
2. Verify InvoiceData record created
3. Check Hangfire dashboard for queued job
4. Monitor processing in Admin UI
5. Review extracted data in details page

## Database Migration

The feature includes a migration that creates:
- `InvoiceData` table
- `Metrics` table
- Required indexes for performance

Apply migration:
```bash
dotnet ef database update --project src/DebtManager.Infrastructure --startup-project src/DebtManager.Web
```

## Future Enhancements

- [ ] Support for multiple invoice formats
- [ ] Line item extraction
- [ ] Automatic debt creation from invoices
- [ ] Email-based invoice submission
- [ ] Webhook notifications for processing completion
- [ ] Enhanced analytics with time-series charts
- [ ] Export metrics to CSV/Excel
- [ ] Integration with Power BI

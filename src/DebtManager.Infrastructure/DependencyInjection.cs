using DebtManager.Infrastructure.External;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using DebtManager.Contracts.Persistence;
using DebtManager.Infrastructure.Persistence.Repositories;
using DebtManager.Contracts.External;
using DebtManager.Contracts.Notifications;
using DebtManager.Infrastructure.Notifications;
using DebtManager.Contracts.Payments;
using DebtManager.Infrastructure.Payments;
using DebtManager.Contracts.Configuration;
using DebtManager.Infrastructure.Configuration;
using DebtManager.Contracts.Documents;
using DebtManager.Infrastructure.Documents;
using DebtManager.Contracts.Analytics;
using DebtManager.Infrastructure.Analytics;
using DebtManager.Contracts.Audit;
using DebtManager.Infrastructure.Audit;

namespace DebtManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration? configuration = null)
    {
       
        // Repositories
        services.AddScoped<IDebtorRepository, DebtorRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IDebtRepository, DebtRepository>();
        services.AddScoped<IPaymentPlanRepository, PaymentPlanRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IInvoiceDataRepository, InvoiceDataRepository>();
        services.AddScoped<IMetricRepository, MetricRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // Config service
        services.AddScoped<IAppConfigService, AppConfigService>();

        // Audit service
        services.AddScoped<IAuditService, AuditService>();

        // Notifications
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<ISmsSender, SmsSender>();

        // Payment Services
        services.AddScoped<IPaymentService, StripePaymentService>();
        services.AddScoped<IWebhookProcessor, StripeWebhookProcessor>();
        services.AddScoped<IReceiptService, ReceiptService>();
        
        // Payment Jobs
        services.AddScoped<ReceiptGenerationJob>();
        services.AddScoped<RetryFailedPaymentsJob>();
        services.AddScoped<PaymentNotificationJob>();

        // Document Generation
        services.AddScoped<IDocumentGenerationService, DocumentGenerationService>();
        
        // Document Processing Services
        services.AddScoped<IInvoiceProcessingService, AzureFormRecognizerInvoiceService>();
        
        // Analytics Services
        services.AddScoped<IMetricService, MetricService>();
        
        return services;
    }
}

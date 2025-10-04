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

namespace DebtManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // ABR Http Client (no config here; validator will use DB-backed config)
        services.AddHttpClient<AbrHttpValidator>();

        // Choose implementation based on presence of BaseUrl; fall back to stub
        services.AddScoped<IAbrValidator>(sp =>
        {
            // Decide dynamically at runtime in validator using config values
            var handler = sp.GetRequiredService<AbrHttpValidator>();
            return handler;
        });

        // Repositories
        services.AddScoped<IDebtorRepository, DebtorRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IDebtRepository, DebtRepository>();
        services.AddScoped<IPaymentPlanRepository, PaymentPlanRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IArticleRepository, ArticleRepository>();

        // Config service
        services.AddScoped<IAppConfigService, AppConfigService>();

        // Notifications
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<ISmsSender, SmsSender>();

        // Payment Services
        services.AddScoped<IPaymentService, StripePaymentService>();
        services.AddScoped<IWebhookProcessor, StripeWebhookProcessor>();
        
        return services;
    }
}

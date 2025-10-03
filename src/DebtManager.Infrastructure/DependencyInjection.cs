using DebtManager.Infrastructure.External;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using DebtManager.Contracts.Persistence;
using DebtManager.Infrastructure.Persistence.Repositories;
using DebtManager.Contracts.External;
using DebtManager.Contracts.Notifications;
using DebtManager.Infrastructure.Notifications;

namespace DebtManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // ABR Http Client
        services.AddHttpClient<AbrHttpValidator>((sp, http) =>
        {
            var config = configuration ?? sp.GetRequiredService<IConfiguration>();
            var baseUrl = config["AbrApi:BaseUrl"] ?? "";
            var apiKey = config["AbrApi:ApiKey"] ?? "";
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                http.BaseAddress = new Uri(baseUrl);
            }
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                http.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
            }
        });

        // Choose implementation based on presence of BaseUrl; fall back to stub
        services.AddScoped<IAbrValidator>(sp =>
        {
            var config = (configuration ?? sp.GetRequiredService<IConfiguration>());
            var baseUrl = config["AbrApi:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                return sp.GetRequiredService<AbrHttpValidator>();
            }
            return new AbrValidatorStub();
        });

        // Repositories
        services.AddScoped<IDebtorRepository, DebtorRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IDebtRepository, DebtRepository>();
        services.AddScoped<IPaymentPlanRepository, PaymentPlanRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        // Notifications
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<ISmsSender, SmsSender>();
        return services;
    }
}

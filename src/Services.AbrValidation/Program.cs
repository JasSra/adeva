using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// GET /validate?abn=...  -> uses IBusinessLookupService if available
app.MapGet("/validate", async (string abn, HttpRequest request, IServiceProvider sp, ILoggerFactory loggerFactory, IConfiguration config, CancellationToken ct) =>
{
    var apiKey = config["AbrService:ApiKey"];
    var providedKey = request.Headers["X-API-KEY"].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(apiKey) && !string.Equals(apiKey, providedKey, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(abn))
        return Results.BadRequest(new { error = "abn is required" });

    var lookupInterfaceName = "BusinessLookupService.Services.IBusinessLookupService";
    var interfaceType = AppDomain.CurrentDomain.GetAssemblies()
        .Select(a => a.GetType(lookupInterfaceName, throwOnError: false, ignoreCase: false))
        .FirstOrDefault(t => t != null);

    if (interfaceType == null)
    {
        var log = loggerFactory.CreateLogger("AbrValidation");
        log.LogWarning("IBusinessLookupService not found. Ensure the BusinessLookupService package is referenced and registered in DI.");
        return Results.Ok(new { isValid = false, warning = "lookup-service-missing" });
    }

    // Try to get from DI
    var lookup = sp.GetService(interfaceType);

    // If not registered, try to construct a default implementation if available
    if (lookup == null)
    {
        var implType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("BusinessLookupService.Services.BusinessLookupService", throwOnError: false, ignoreCase: false))
            .FirstOrDefault(t => t != null);
        if (implType != null)
        {
            var authGuid = config["AbrService:AuthGuid"] ?? string.Empty;
            var implLogger = loggerFactory.CreateLogger(implType);

            // Find ctor(string, ILogger<BusinessLookupService>)
            var ctor = implType.GetConstructors()
                .FirstOrDefault(c =>
                {
                    var ps = c.GetParameters();
                    return ps.Length == 2 && ps[0].ParameterType == typeof(string) && ps[1].ParameterType.IsGenericType && ps[1].ParameterType.GetGenericTypeDefinition() == typeof(ILogger<>);
                });

            if (ctor != null)
            {
                // Create ILogger<T> for the implType via LoggerFactory
                var loggerInstance = loggerFactory.CreateLogger(implType);
                lookup = ctor.Invoke(new object?[] { authGuid, loggerInstance });
            }
        }
    }

    if (lookup == null)
    {
        var log = loggerFactory.CreateLogger("AbrValidation");
        log.LogWarning("IBusinessLookupService not registered and no default implementation found.");
        return Results.Ok(new { isValid = false, warning = "lookup-service-not-available" });
    }

    // Invoke IsIdentifierActiveAsync via reflection
    var method = interfaceType.GetMethod("IsIdentifierActiveAsync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
    if (method == null)
    {
        return Results.Ok(new { isValid = false, warning = "method-missing" });
    }

    var taskObj = method.Invoke(lookup, new object?[] { abn });
    if (taskObj is Task task)
    {
        await task.ConfigureAwait(false);
        var resultObj = GetTaskResult(task);
        var isValid = resultObj is bool b && b;
        return Results.Ok(new { isValid });
    }

    return Results.Ok(new { isValid = false, warning = "unexpected-return" });
})
.WithOpenApi();

app.Run();

static object? GetTaskResult(Task task)
{
    var t = task.GetType();
    if (t.IsGenericType)
    {
        var prop = t.GetProperty("Result", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        return prop?.GetValue(task);
    }
    return null;
}


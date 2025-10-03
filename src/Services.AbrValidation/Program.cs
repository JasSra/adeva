using DebtManager.Contracts.External;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IAbrValidator, LocalAbrValidator>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/validate", (string abn, HttpRequest request, IAbrValidator validator) =>
{
    var apiKey = builder.Configuration["AbrService:ApiKey"];
    var providedKey = request.Headers["X-API-KEY"].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        if (!string.Equals(apiKey, providedKey, StringComparison.Ordinal))
        {
            return Results.Unauthorized();
        }
    }
    return Results.Ok(new { isValid = abn != null && abn.Length == 11 });
})
.WithOpenApi();

app.Run();

sealed class LocalAbrValidator : IAbrValidator
{
    public Task<bool> ValidateAsync(string abn, CancellationToken ct = default) => Task.FromResult(!string.IsNullOrWhiteSpace(abn) && abn.Length == 11);
}

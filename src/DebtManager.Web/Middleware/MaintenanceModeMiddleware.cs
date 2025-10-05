using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using DebtManager.Web.Services;
using Serilog;

namespace DebtManager.Web.Middleware;

public class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;

    public MaintenanceModeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IMaintenanceState state, IHostEnvironment env)
    {
        // Allow health endpoints and brand assets to pass through
        var path = context.Request.Path.Value ?? string.Empty;
        if (!state.IsMaintenance ||
            path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/brand", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(path, "/favicon.ico", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        var fwdFor = context.Request.Headers["X-Forwarded-For"].ToString();

        // Log a structured maintenance hit
        Log.ForContext("TraceId", traceId)
           .ForContext("ClientIP", remoteIp)
           .ForContext("ForwardedFor", fwdFor)
           .ForContext("Path", path)
           .Information("Maintenance page served");

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers["Retry-After"] = "120";
        context.Response.Headers["X-Trace-Id"] = traceId;
        context.Response.ContentType = "text/html; charset=utf-8";

        var since = state.EnabledAt;
        var duration = since.HasValue ? DateTimeOffset.UtcNow - since.Value : (TimeSpan?)null;
        var durationText = duration.HasValue ? FormatDuration(duration.Value) : "just now";
        var sinceText = since?.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt zzz");

        var sb = new StringBuilder();
        sb.Append("<!doctype html><html lang=\"en\"><head>");
        sb.Append("<meta charset=\"utf-8\">");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.Append("<title>Adeva — We'll be right back</title>");
        sb.Append("<link rel=\"preconnect\" href=\"https://fonts.googleapis.com\">");
        sb.Append("<link rel=\"preconnect\" href=\"https://fonts.gstatic.com\" crossorigin>");
        sb.Append("<link href=\"https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600&display=swap\" rel=\"stylesheet\">");
        
        sb.Append("<style>");
        sb.Append("*{margin:0;padding:0;box-sizing:border-box}");
        sb.Append("html{font-size:16px}");
        sb.Append("body{font-family:'Inter',-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;");
        sb.Append("background:linear-gradient(135deg,#0f1419 0%,#1a1f2e 50%,#0f1419 100%);");
        sb.Append("color:#ffffff;min-height:100vh;display:flex;align-items:center;justify-content:center;");
        sb.Append("line-height:1.6;letter-spacing:-0.011em;-webkit-font-smoothing:antialiased;");
        sb.Append("background-attachment:fixed}");
        
        sb.Append(".container{max-width:540px;margin:0 auto;padding:2rem;text-align:center}");
        
        sb.Append(".card{background:rgba(255,255,255,0.03);border:1px solid rgba(255,255,255,0.08);");
        sb.Append("border-radius:20px;padding:3rem 2.5rem;backdrop-filter:blur(20px);");
        sb.Append("box-shadow:0 20px 40px rgba(0,0,0,0.15),0 0 0 1px rgba(255,255,255,0.05) inset;");
        sb.Append("position:relative;overflow:hidden}");
        
        sb.Append(".card::before{content:'';position:absolute;top:0;left:0;right:0;height:1px;");
        sb.Append("background:linear-gradient(90deg,transparent,rgba(255,255,255,0.1),transparent)}");
        
        sb.Append(".logo-container{margin-bottom:2rem;position:relative}");
        sb.Append(".logo{height:40px;opacity:0.95;filter:brightness(1.1)}");
        sb.Append(".status-badge{position:absolute;top:-8px;right:-12px;");
        sb.Append("background:linear-gradient(135deg,#ff6b6b,#ee5a24);color:#fff;");
        sb.Append("padding:4px 12px;border-radius:12px;font-size:11px;font-weight:600;");
        sb.Append("letter-spacing:0.5px;text-transform:uppercase;");
        sb.Append("box-shadow:0 4px 12px rgba(238,90,36,0.3)}");
        
        sb.Append("h1{font-size:2rem;font-weight:600;margin-bottom:1rem;");
        sb.Append("background:linear-gradient(135deg,#ffffff,#e2e8f0);");
        sb.Append("-webkit-background-clip:text;-webkit-text-fill-color:transparent;");
        sb.Append("background-clip:text}");
        
        sb.Append(".subtitle{color:rgba(255,255,255,0.7);font-size:1.125rem;margin-bottom:2.5rem;font-weight:400}");
        
        sb.Append(".info-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(160px,1fr));");
        sb.Append("gap:1rem;margin:2rem 0}");
        
        sb.Append(".info-card{background:rgba(255,255,255,0.04);border:1px solid rgba(255,255,255,0.06);");
        sb.Append("border-radius:12px;padding:1.25rem;text-align:left;");
        sb.Append("transition:all 0.2s ease}");
        
        sb.Append(".info-card:hover{background:rgba(255,255,255,0.06);border-color:rgba(255,255,255,0.1);");
        sb.Append("transform:translateY(-1px)}");
        
        sb.Append(".info-label{color:rgba(139,174,255,0.9);font-size:0.75rem;font-weight:600;");
        sb.Append("text-transform:uppercase;letter-spacing:0.5px;margin-bottom:0.5rem}");
        
        sb.Append(".info-value{color:#ffffff;font-weight:500;word-break:break-all;font-size:0.875rem}");
        
        sb.Append(".trace-id{font-family:'SF Mono',Monaco,Inconsolata,'Roboto Mono',Consolas,'Courier New',monospace;");
        sb.Append("background:rgba(139,174,255,0.1);color:#8baaff;padding:0.5rem;border-radius:6px;");
        sb.Append("font-size:0.75rem;border:1px solid rgba(139,174,255,0.2)}");
        
        sb.Append(".footer{margin-top:2.5rem;color:rgba(255,255,255,0.5);font-size:0.875rem}");
        
        sb.Append(".pulse{animation:pulse 2s infinite}");
        sb.Append("@keyframes pulse{0%,100%{opacity:1}50%{opacity:0.7}}");
        
        sb.Append(".dev-section{margin-top:2rem;padding-top:2rem;");
        sb.Append("border-top:1px solid rgba(255,255,255,0.1)}");
        
        sb.Append(".dev-title{color:rgba(255,179,71,0.9);font-size:1rem;font-weight:600;margin-bottom:1rem}");
        
        sb.Append(".error-details{background:rgba(255,179,71,0.05);border:1px solid rgba(255,179,71,0.2);");
        sb.Append("border-radius:8px;padding:1rem;font-family:'SF Mono',Monaco,Inconsolata,'Roboto Mono',Consolas,'Courier New',monospace;");
        sb.Append("font-size:0.75rem;color:rgba(255,179,71,0.9);text-align:left;");
        sb.Append("white-space:pre-wrap;overflow:auto;max-height:300px}");
        
        sb.Append("@media (max-width:640px){");
        sb.Append(".container{padding:1rem}");
        sb.Append(".card{padding:2rem 1.5rem}");
        sb.Append("h1{font-size:1.75rem}");
        sb.Append(".subtitle{font-size:1rem}");
        sb.Append(".info-grid{grid-template-columns:1fr;gap:0.75rem}");
        sb.Append("}");
        sb.Append("</style>");
        
        sb.Append("</head><body>");
        sb.Append("<div class=\"container\">");
        sb.Append("<div class=\"card\">");
        
        sb.Append("<div class=\"logo-container\">");
        sb.Append("<img class=\"logo\" src=\"/brand/adeva-wordmark.svg\" alt=\"Adeva\">");
        sb.Append("<div class=\"status-badge pulse\">503</div>");
        sb.Append("</div>");
        
        sb.Append("<h1>We'll be right back</h1>");
        sb.Append("<p class=\"subtitle\">We're experiencing technical difficulties and are working to restore service as quickly as possible.</p>");
        
        sb.Append("<div class=\"info-grid\">");
        sb.Append($"<div class=\"info-card\"><div class=\"info-label\">Reference ID</div><div class=\"trace-id\">{System.Net.WebUtility.HtmlEncode(traceId)}</div></div>");
        
        if (since.HasValue)
        {
            sb.Append($"<div class=\"info-card\"><div class=\"info-label\">Started</div><div class=\"info-value\">{System.Net.WebUtility.HtmlEncode(sinceText!)}</div></div>");
            sb.Append($"<div class=\"info-card\"><div class=\"info-label\">Duration</div><div class=\"info-value\">{System.Net.WebUtility.HtmlEncode(durationText)}</div></div>");
        }
        sb.Append("</div>");

        if (env.IsDevelopment())
        {
            var ex = state.StartupException;
            if (ex != null)
            {
                sb.Append("<div class=\"dev-section\">");
                sb.Append("<div class=\"dev-title\">🔧 Development Information</div>");
                sb.Append("<div class=\"error-details\">");
                sb.Append(System.Net.WebUtility.HtmlEncode(ex.ToString()));
                sb.Append("</div>");
                sb.Append("</div>");
            }
        }
        
        sb.Append("<div class=\"footer\">");
        sb.Append("Please reference the ID above when contacting support. We apologize for any inconvenience.");
        sb.Append("</div>");
        
        sb.Append("</div>");
        sb.Append("</div>");
        sb.Append("</body></html>");

        await context.Response.WriteAsync(sb.ToString());
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalDays >= 1)
        {
            var days = (int)ts.TotalDays;
            ts -= TimeSpan.FromDays(days);
            return $"{days}d {ts.Hours}h {ts.Minutes}m";
        }
        if (ts.TotalHours >= 1)
        {
            return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
        }
        if (ts.TotalMinutes >= 1)
        {
            return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
        }
        return $"{(int)ts.TotalSeconds}s";
    }
}

public static class MaintenanceModeMiddlewareExtensions
{
    public static IApplicationBuilder UseMaintenanceMode(this IApplicationBuilder app)
    {
        return app.UseMiddleware<MaintenanceModeMiddleware>();
    }
}

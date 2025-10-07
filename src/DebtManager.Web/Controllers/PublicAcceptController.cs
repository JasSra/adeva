using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace DebtManager.Web.Controllers;

[AllowAnonymous]
public class PublicAcceptController : Controller
{
    private readonly IConfiguration _config;

    public PublicAcceptController(IConfiguration config)
    {
        _config = config;
    }

    // Public entry: /Accept?t=...  token encodes debtId and expiry
    [HttpGet("/Accept")]
    public IActionResult Accept(string? t)
    {
        if (string.IsNullOrWhiteSpace(t))
        {
            return BadRequest("Missing token");
        }

        if (!TryValidateToken(t, out var debtId, out var error))
        {
            TempData["Error"] = "Your link is invalid or has expired.";
            return Redirect("/Account/SignInUser");
        }

        // Redirect into secured flow; auth middleware will prompt sign-in if needed
        return Redirect($"/User/Accept/{debtId}");
    }

    private bool TryValidateToken(string token, out Guid debtId, out string? error)
    {
        debtId = Guid.Empty;
        error = null;
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 2)
            {
                error = "Invalid token format";
                return false;
            }
            var payloadBytes = Base64UrlDecode(parts[0]);
            var sigBytes = Base64UrlDecode(parts[1]);
            var payload = Encoding.UTF8.GetString(payloadBytes);
            var fields = payload.Split('|');
            if (fields.Length < 2)
            {
                error = "Invalid token payload";
                return false;
            }
            if (!Guid.TryParse(fields[0], out debtId))
            {
                error = "Invalid debt id";
                return false;
            }
            if (!long.TryParse(fields[1], out var expUnix))
            {
                error = "Invalid expiry";
                return false;
            }
            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (expUnix < nowUnix)
            {
                error = "Token expired";
                return false;
            }

            var secret = _config["AcceptLinks:SecretKey"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                error = "Missing secret";
                return false;
            }
            using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computed = h.ComputeHash(payloadBytes);
            var ok = CryptographicOperations.FixedTimeEquals(computed, sigBytes);
            if (!ok)
            {
                error = "Signature invalid";
                return false;
            }
            return true;
        }
        catch
        {
            error = "Token validation failed";
            return false;
        }
    }

    public static string CreateToken(Guid debtId, string secret, TimeSpan? lifetime = null)
    {
        var exp = DateTimeOffset.UtcNow.Add(lifetime ?? TimeSpan.FromDays(7)).ToUnixTimeSeconds();
        var payload = Encoding.UTF8.GetBytes($"{debtId}|{exp}");
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var sig = h.ComputeHash(payload);
        return Base64UrlEncode(payload) + "." + Base64UrlEncode(sig);
    }

    private static byte[] Base64UrlDecode(string input)
    {
        input = input.Replace('-', '+').Replace('_', '/');
        switch (input.Length % 4)
        {
            case 2: input += "=="; break;
            case 3: input += "="; break;
        }
        return Convert.FromBase64String(input);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

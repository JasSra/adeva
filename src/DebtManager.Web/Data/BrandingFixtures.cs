using System;
using Bogus;
using DebtManager.Domain.Organizations;

namespace DebtManager.Web.Data;

internal static class BrandingFixtures
{
    private static readonly BrandingTemplate[] Templates =
    {
        new(
            Key: "northwave",
            PrimaryColorHex: "#1e3a8a",
            SecondaryColorHex: "#3b82f6",
            LogoUrl: "https://placehold.co/160x48/1E3A8A/ffffff?text=Northwave",
            FaviconUrl: "https://placehold.co/48/1E3A8A/ffffff?text=N",
            TaglineTemplate: "{name} • Recover with confidence.",
            StatementFooterTemplate: "Receipts issued by {legalName}. ABN {abn}."),
        new(
            Key: "aurora",
            PrimaryColorHex: "#0f766e",
            SecondaryColorHex: "#14b8a6",
            LogoUrl: "https://placehold.co/160x48/0F766E/ffffff?text=Aurora",
            FaviconUrl: "https://placehold.co/48/0F766E/ffffff?text=A",
            TaglineTemplate: "{name} • Compassionate collections.",
            StatementFooterTemplate: "Customer receipts issued by {legalName} trading as {name}."),
        new(
            Key: "ember",
            PrimaryColorHex: "#b91c1c",
            SecondaryColorHex: "#f87171",
            LogoUrl: "https://placehold.co/160x48/B91C1C/ffffff?text=Ember",
            FaviconUrl: "https://placehold.co/48/B91C1C/ffffff?text=E",
            TaglineTemplate: "{name} • Keep the balance steady.",
            StatementFooterTemplate: "All payments handled by {legalName}. Need help? Email {supportEmail}."),
        new(
            Key: "solstice",
            PrimaryColorHex: "#7c3aed",
            SecondaryColorHex: "#a855f7",
            LogoUrl: "https://placehold.co/160x48/7C3AED/ffffff?text=Solstice",
            FaviconUrl: "https://placehold.co/48/7C3AED/ffffff?text=S",
            TaglineTemplate: "{name} • Fair, transparent debt recovery.",
            StatementFooterTemplate: "Issued on behalf of {legalName}. Contact us at {supportEmail}."),
        new(
            Key: "copperline",
            PrimaryColorHex: "#c2410c",
            SecondaryColorHex: "#f97316",
            LogoUrl: "https://placehold.co/160x48/C2410C/ffffff?text=Copperline",
            FaviconUrl: "https://placehold.co/48/C2410C/ffffff?text=C",
            TaglineTemplate: "{name} • Partners in resolution.",
            StatementFooterTemplate: "{legalName} • ABN {abn} • {supportPhone}.")
    };

    public static BrandingTemplate Next() => Templates[Random.Shared.Next(Templates.Length)];

    public static BrandingTemplate Pick(Random random) => Templates[random.Next(Templates.Length)];

    public static BrandingTemplate Pick(Randomizer randomizer) => randomizer.ArrayElement(Templates);

    public static void ApplyTo(Organization org, BrandingTemplate? template = null)
    {
        if (org is null) throw new ArgumentNullException(nameof(org));
        var chosen = template ?? Next();

        org.RefreshBranding(
            chosen.PrimaryColorHex ?? org.PrimaryColorHex,
            chosen.SecondaryColorHex ?? org.SecondaryColorHex,
            chosen.LogoUrl,
            chosen.FaviconUrl,
            Format(chosen.TaglineTemplate, org));

        var footer = Format(chosen.StatementFooterTemplate, org);
        org.SetStatementFooter(footer);
    }

    private static string Format(string template, Organization org)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return $"Receipts issued by {org.LegalName}.";
        }

        var trading = string.IsNullOrWhiteSpace(org.TradingName) ? org.Name : org.TradingName!;

        return template
            .Replace("{name}", trading, StringComparison.OrdinalIgnoreCase)
            .Replace("{legalName}", org.LegalName, StringComparison.OrdinalIgnoreCase)
            .Replace("{abn}", org.Abn, StringComparison.OrdinalIgnoreCase)
            .Replace("{supportEmail}", org.SupportEmail, StringComparison.OrdinalIgnoreCase)
            .Replace("{supportPhone}", org.SupportPhone, StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed record BrandingTemplate(
    string Key,
    string PrimaryColorHex,
    string SecondaryColorHex,
    string LogoUrl,
    string? FaviconUrl,
    string TaglineTemplate,
    string StatementFooterTemplate);

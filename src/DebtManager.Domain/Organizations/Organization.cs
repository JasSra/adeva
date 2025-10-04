using System.Collections.Generic;
using System.Linq;
using DebtManager.Domain.Common;
using DebtManager.Domain.Debtors;
using DebtManager.Domain.Debts;
using DebtManager.Domain.Documents;

namespace DebtManager.Domain.Organizations;

public class Organization : Entity
{
    private readonly List<Debtor> _debtors;
    private readonly List<Debt> _debts;
    private readonly List<Document> _documents;

    public string Name { get; private set; }
    public string LegalName { get; private set; }
    public string? TradingName { get; private set; }
    public string? Subdomain { get; private set; }
    public string Abn { get; private set; }
    public string DefaultCurrency { get; private set; }
    public string PrimaryColorHex { get; private set; }
    public string SecondaryColorHex { get; private set; }
    public string SupportEmail { get; private set; }
    public string SupportPhone { get; private set; }
    public string? BillingContactName { get; private set; }
    public string? BillingContactEmail { get; private set; }
    public string? BillingContactPhone { get; private set; }
    public string Timezone { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? FaviconUrl { get; private set; }
    public string? StatementFooter { get; private set; }
    public string? BrandTagline { get; private set; }
    public bool IsApproved { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public DateTime? OnboardedAtUtc { get; private set; }
    public DateTime? NextReconciliationAtUtc { get; private set; }
    public DateTime? LastBrandRefreshAtUtc { get; private set; }

    public IReadOnlyCollection<Debtor> Debtors => _debtors.AsReadOnly();
    public IReadOnlyCollection<Debt> Debts => _debts.AsReadOnly();
    public IReadOnlyCollection<Document> Documents => _documents.AsReadOnly();

    private Organization()
    {
        _debtors = new List<Debtor>();
        _debts = new List<Debt>();
        _documents = new List<Document>();
        Name = LegalName = Abn = DefaultCurrency = PrimaryColorHex = SecondaryColorHex = SupportEmail = SupportPhone = Timezone = string.Empty;
    }

    public Organization(
        string name,
        string legalName,
        string abn,
        string defaultCurrency,
        string primaryColorHex,
        string secondaryColorHex,
        string supportEmail,
        string supportPhone,
        string timezone,
        string? subdomain = null,
        string? tradingName = null)
        : this()
    {
        Name = name;
        LegalName = legalName;
        Abn = abn;
        DefaultCurrency = defaultCurrency;
        PrimaryColorHex = primaryColorHex;
        SecondaryColorHex = secondaryColorHex;
        SupportEmail = supportEmail;
        SupportPhone = supportPhone;
        Timezone = timezone;
        Subdomain = subdomain;
        TradingName = tradingName;
    }

    public static Organization CreatePending(
        string name,
        string legalName,
        string abn,
        string defaultCurrency,
        string primaryColorHex,
        string secondaryColorHex,
        string supportEmail,
        string supportPhone,
        string timezone,
        string? subdomain = null,
        string? tradingName = null)
    {
        var organization = new Organization(name, legalName, abn, defaultCurrency, primaryColorHex, secondaryColorHex, supportEmail, supportPhone, timezone, subdomain, tradingName)
        {
            IsApproved = false
        };
        return organization;
    }

    public void UpdateTradingName(string? tradingName)
    {
        TradingName = tradingName;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetSubdomain(string? subdomain)
    {
        Subdomain = subdomain;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Approve(DateTime? approvedAtUtc = null)
    {
        if (IsApproved)
        {
            return;
        }

        IsApproved = true;
        ApprovedAtUtc = approvedAtUtc ?? DateTime.UtcNow;
        UpdatedAtUtc = ApprovedAtUtc;
    }

    public void MarkOnboarded(DateTime? onboardedAtUtc = null)
    {
        OnboardedAtUtc = onboardedAtUtc ?? DateTime.UtcNow;
        UpdatedAtUtc = OnboardedAtUtc;
    }

    public void ScheduleNextReconciliation(DateTime nextReconciliationUtc)
    {
        NextReconciliationAtUtc = nextReconciliationUtc;
        UpdatedAtUtc = nextReconciliationUtc;
    }

    public void RefreshBranding(string primaryColorHex, string secondaryColorHex, string? logoUrl, string? faviconUrl, string? brandTagline)
    {
        PrimaryColorHex = primaryColorHex;
        SecondaryColorHex = secondaryColorHex;
        LogoUrl = logoUrl;
        FaviconUrl = faviconUrl;
        BrandTagline = brandTagline;
        LastBrandRefreshAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = LastBrandRefreshAtUtc;
    }

    public void UpdateSupportContacts(string email, string phone)
    {
        SupportEmail = email;
        SupportPhone = phone;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateBillingContact(string? name, string? email, string? phone)
    {
        BillingContactName = name;
        BillingContactEmail = email;
        BillingContactPhone = phone;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetStatementFooter(string? statementFooter)
    {
        StatementFooter = statementFooter;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ChangeDefaultCurrency(string currency)
    {
        DefaultCurrency = currency;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AttachDebtor(Debtor debtor)
    {
        if (debtor is null)
        {
            throw new ArgumentNullException(nameof(debtor));
        }

        if (_debtors.Any(x => x.Id == debtor.Id))
        {
            return;
        }

        _debtors.Add(debtor);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AttachDebt(Debt debt)
    {
        if (debt is null)
        {
            throw new ArgumentNullException(nameof(debt));
        }

        if (_debts.Any(x => x.Id == debt.Id))
        {
            return;
        }

        _debts.Add(debt);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AttachDocument(Document doc)
    {
        if (doc is null) throw new ArgumentNullException(nameof(doc));
        if (_documents.Any(d => d.Id == doc.Id)) return;
        _documents.Add(doc);
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

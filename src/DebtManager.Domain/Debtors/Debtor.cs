using System.Collections.Generic;
using System.Linq;
using DebtManager.Domain.Common;
using DebtManager.Domain.Debts;
using DebtManager.Domain.Organizations;
using DebtManager.Domain.Payments;
using DebtManager.Domain.Documents;

namespace DebtManager.Domain.Debtors;

public enum DebtorStatus
{
    New,
    Invited,
    Verified,
    Active,
    Delinquent,
    Settled,
    Archived
}

public enum ContactMethod
{
    Email,
    Sms,
    PhoneCall,
    PostalMail,
    Portal
}

public class Debtor : Entity
{
    private readonly List<Debt> _debts;
    private readonly List<Transaction> _transactions;
    private readonly List<Document> _documents;

    public Guid OrganizationId { get; private set; }
    public string ReferenceId { get; private set; }
    public string Email { get; private set; }
    public string Phone { get; private set; }
    public string AlternatePhone { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string PreferredName { get; private set; }
    public DateTime? DateOfBirth { get; private set; }
    public DebtorStatus Status { get; private set; }
    public ContactMethod PreferredContactMethod { get; private set; }
    public string AddressLine1 { get; private set; }
    public string AddressLine2 { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string PostalCode { get; private set; }
    public string CountryCode { get; private set; }
    public string GovernmentId { get; private set; }
    public string EmployerName { get; private set; }
    public string IncomeBracket { get; private set; }
    public bool PortalAccessEnabled { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }
    public DateTime? LastContactedAtUtc { get; private set; }
    public string TagsCsv { get; private set; }
    public string Notes { get; private set; }

    public Organization? Organization { get; private set; }
    public IReadOnlyCollection<Debt> Debts => _debts.AsReadOnly();
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();
    public IReadOnlyCollection<Document> Documents => _documents.AsReadOnly();

    private Debtor()
    {
        _debts = new List<Debt>();
        _transactions = new List<Transaction>();
        _documents = new List<Document>();
        ReferenceId = Email = Phone = AlternatePhone = string.Empty;
        FirstName = LastName = PreferredName = string.Empty;
        AddressLine1 = AddressLine2 = City = State = PostalCode = string.Empty;
        CountryCode = "AU";
        GovernmentId = EmployerName = IncomeBracket = string.Empty;
        TagsCsv = Notes = string.Empty;
        PreferredContactMethod = ContactMethod.Email;
        Status = DebtorStatus.New;
    }

    public Debtor(Guid organizationId, string referenceId, string email, string phone, string firstName, string lastName)
        : this()
    {
        OrganizationId = organizationId;
        ReferenceId = referenceId;
        Email = email;
        Phone = phone;
        FirstName = firstName;
        LastName = lastName;
    }

    public Debtor(Guid organizationId, string email, string phone, string referenceId)
        : this(organizationId, referenceId, email, phone, string.Empty, string.Empty)
    {
    }

    public void UpdatePersonalDetails(string firstName, string lastName, string? preferredName = null, DateTime? dateOfBirth = null, string? governmentId = null)
    {
        FirstName = firstName;
        LastName = lastName;
        PreferredName = preferredName ?? PreferredName;
        DateOfBirth = dateOfBirth;
        if (!string.IsNullOrWhiteSpace(governmentId))
        {
            GovernmentId = governmentId;
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateContactDetails(string email, string phone, string? alternatePhone = null, ContactMethod? preferredMethod = null)
    {
        Email = email;
        Phone = phone;
        if (!string.IsNullOrWhiteSpace(alternatePhone))
        {
            AlternatePhone = alternatePhone;
        }

        if (preferredMethod.HasValue)
        {
            PreferredContactMethod = preferredMethod.Value;
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateAddress(string? line1, string? line2, string? city, string? state, string? postalCode, string? countryCode = null)
    {
        AddressLine1 = line1 ?? string.Empty;
        AddressLine2 = line2 ?? string.Empty;
        City = city ?? string.Empty;
        State = state ?? string.Empty;
        PostalCode = postalCode ?? string.Empty;
        CountryCode = string.IsNullOrWhiteSpace(countryCode) ? CountryCode : countryCode!;

        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateEmployment(string? employerName, string? incomeBracket)
    {
        EmployerName = employerName ?? string.Empty;
        IncomeBracket = incomeBracket ?? string.Empty;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetStatus(DebtorStatus status)
    {
        if (Status == status)
        {
            return;
        }

        Status = status;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void EnablePortalAccess()
    {
        if (PortalAccessEnabled)
        {
            return;
        }

        PortalAccessEnabled = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void DisablePortalAccess()
    {
        if (!PortalAccessEnabled)
        {
            return;
        }

        PortalAccessEnabled = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordLogin(DateTime loginAtUtc)
    {
        LastLoginAtUtc = loginAtUtc;
        UpdatedAtUtc = loginAtUtc;
    }

    public void RecordContact(DateTime contactAtUtc, string? notes = null)
    {
        LastContactedAtUtc = contactAtUtc;
        AppendNote(notes);
        UpdatedAtUtc = contactAtUtc;
    }

    public void SetTags(IEnumerable<string> tags)
    {
        TagsCsv = string.Join(',', tags.Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)));
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AppendNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return;
        }

        Notes = string.IsNullOrWhiteSpace(Notes)
            ? note.Trim()
            : $"{Notes}\n{note.Trim()}";

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

    public void AttachTransaction(Transaction transaction)
    {
        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (_transactions.Any(x => x.Id == transaction.Id))
        {
            return;
        }

        _transactions.Add(transaction);
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

using DebtManager.Domain.Common;
using DebtManager.Domain.Debtors;
using DebtManager.Domain.Organizations;

namespace DebtManager.Domain.Documents;

public enum DocumentType
{
    Unknown = 0,
    Invoice = 1,
    Receipt = 2,
    Statement = 3,
    Contract = 4,
    Identity = 5,
    Evidence = 6
}

public class Document : Entity
{
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public long SizeBytes { get; private set; }

    // Storage provider fields (e.g., Azure Blob URL or key)
    public string StoragePath { get; private set; } = string.Empty;
    public string? Sha256 { get; private set; }

    public DocumentType Type { get; private set; }

    // Optional relationships - a document can belong to an Organization or a Debtor (or both)
    public Guid? OrganizationId { get; private set; }
    public Guid? DebtorId { get; private set; }

    public Organization? Organization { get; private set; }
    public Debtor? Debtor { get; private set; }

    private Document()
    {
        FileName = ContentType = string.Empty;
    }

    public Document(string fileName, string contentType, long sizeBytes, DocumentType type, string storagePath, string? sha256 = null, Guid? organizationId = null, Guid? debtorId = null)
    {
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        Type = type;
        StoragePath = storagePath;
        Sha256 = sha256;
        OrganizationId = organizationId;
        DebtorId = debtorId;
    }
}

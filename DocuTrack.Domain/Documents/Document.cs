using DocuTrack.Domain.Documents.Exceptions;

namespace DocuTrack.Domain.Documents;
public sealed class Document
{
    private Document()
    {
    }
    public Guid Id { get; private set; }
    public string DocumentNumber { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DocumentType Type { get; private set; }
    public Department Department { get; private set; }
    public string Owner { get; private set; } = string.Empty;
    public DocumentStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastUpdatedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? LastModifiedByUserId { get; private set; }
    public int Version { get; private set; }

    public static Document Create(
        string documentNumber,
        string title,
        string? description,
        DocumentType type,
        Department department,
        string owner,
        Guid createdByUserId,
        DateTimeOffset createdAt)
    {
        ValidateDetails(title, type, department, owner);

        if (string.IsNullOrWhiteSpace(documentNumber))
        {
            throw new ArgumentException(
                "Document number is required.",
                nameof(documentNumber));
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Creator user ID is required.",
                nameof(createdByUserId));
        }

        return new Document
        {
            Id = Guid.NewGuid(),
            DocumentNumber = documentNumber.Trim(),
            Title = title.Trim(),
            Description = NormalizeDescription(description),
            Type = type,
            Department = department,
            Owner = owner.Trim(),
            Status = DocumentStatus.Draft,
            CreatedAt = createdAt,
            LastUpdatedAt = createdAt,
            CreatedByUserId = createdByUserId,
            LastModifiedByUserId = createdByUserId,
            Version = 1
        };
    }

    public void UpdateDetails(
        string title,
        string? description,
        DocumentType type,
        Department department,
        string owner,
        Guid modifiedByUserId,
        DateTimeOffset modifiedAt)
    {
        ValidateDetails(title, type, department, owner);

        if (modifiedByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Modifier user ID is required.",
                nameof(modifiedByUserId));
        }

        Title = title.Trim();
        Description = NormalizeDescription(description);
        Type = type;
        Department = department;
        Owner = owner.Trim();
        LastModifiedByUserId = modifiedByUserId;
        LastUpdatedAt = modifiedAt;
        Version++;
    }

    public void ChangeStatus(
        DocumentStatus newStatus,
        Guid modifiedByUserId,
        DateTimeOffset modifiedAt)
    {
        if (!CanTransitionTo(newStatus))
        {
            throw new InvalidDocumentStatusTransitionException(Status, newStatus);
        }

        Status = newStatus;
        LastModifiedByUserId = modifiedByUserId;
        LastUpdatedAt = modifiedAt;
        Version++;
    }

    public void EnsureCanDelete()
    {
        if (Status is not DocumentStatus.Draft and not DocumentStatus.Rejected)
        {
            throw new DocumentDeletionNotAllowedException(Id, Status);
        }
    }

    private bool CanTransitionTo(
        DocumentStatus newStatus)
    {
        return (Status, newStatus) switch
        {
            (DocumentStatus.Draft,
             DocumentStatus.Uploaded) => true,

            (DocumentStatus.Uploaded,
             DocumentStatus.UnderReview) => true,

            (DocumentStatus.UnderReview,
             DocumentStatus.PendingApproval) => true,

            (DocumentStatus.PendingApproval,
             DocumentStatus.Approved) => true,

            (DocumentStatus.PendingApproval,
             DocumentStatus.Rejected) => true,

            (DocumentStatus.Rejected,
             DocumentStatus.Draft) => true,

            (DocumentStatus.Approved,
             DocumentStatus.Archived) => true,

            _ => false
        };
    }

    private static void ValidateDetails(
        string title,
        DocumentType type,
        Department department,
        string owner)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Document title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(owner))
        {
            throw new ArgumentException("Document owner is required.", nameof(owner));
        }

        if (type == DocumentType.Unknown)
        {
            throw new ArgumentException("Document type is required.", nameof(type));
        }

        if (department == Department.Unknown)
        {
            throw new ArgumentException("Department is required.", nameof(department));
        }
    }

    private static string? NormalizeDescription(
        string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }
}
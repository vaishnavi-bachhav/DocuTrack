using System.ComponentModel.DataAnnotations;
using DocuTrack.Domain.Documents;

namespace DocuTrack.Api.Contracts.Requests;

public sealed class UpdateDocumentApiRequest
    : IValidatableObject
{
    [Required]
    [StringLength(150, MinimumLength = 3)]
    public required string Title { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    [EnumDataType(typeof(DocumentType))]
    public DocumentType DocumentType { get; init; }

    [EnumDataType(typeof(Department))]
    public Department Department { get; init; }

    [Required]
    [StringLength(100)]
    public required string Owner { get; init; }

    [Range(1, int.MaxValue)]
    public int Version { get; init; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            yield return new ValidationResult(
                "Title cannot contain only whitespace.",
                [nameof(Title)]);
        }

        if (string.IsNullOrWhiteSpace(Owner))
        {
            yield return new ValidationResult(
                "Owner cannot contain only whitespace.",
                [nameof(Owner)]);
        }

        if (DocumentType == DocumentType.Unknown)
        {
            yield return new ValidationResult(
                "A valid document type is required.",
                [nameof(DocumentType)]);
        }

        if (Department == Department.Unknown)
        {
            yield return new ValidationResult(
                "A valid department is required.",
                [nameof(Department)]);
        }
    }
}
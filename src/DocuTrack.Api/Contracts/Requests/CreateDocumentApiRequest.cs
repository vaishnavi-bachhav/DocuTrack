using DocuTrack.Domain.Documents;
using System.ComponentModel.DataAnnotations;

namespace DocuTrack.Api.Contracts.Requests
{
    public class CreateDocumentApiRequest
    {
        [Required(ErrorMessage = "Title is required.")]
        [MinLength(3, ErrorMessage = "Title must be at least 3 characters long.")]
        [MaxLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
        public required string Title { get; init; }

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; init; }

        [EnumDataType(typeof(DocumentType), ErrorMessage = "Invalid document type.")]
        public DocumentType DocumentType { get; init; }

        [EnumDataType(typeof(Department), ErrorMessage = "Invalid department.")]
        public Department Department { get; init; }

        [Required(ErrorMessage = "Owner is required.")]
        [MaxLength(100, ErrorMessage = "Owner cannot exceed 100 characters.")]
        public required string Owner { get; init; }
    }
}

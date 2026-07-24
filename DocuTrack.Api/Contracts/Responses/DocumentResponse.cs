using DocuTrack.Core.Enums;

namespace DocuTrack.Api.Contracts.Responses
{
    public class DocumentResponse
    {
        public Guid Id { get; init; }
        public required string DocumentNumber { get; init; }
        public required string Title { get; init; }
        public string? Description { get; init; }
        public DocumentType Type { get; init; }
        public Department Department { get; init; }
        public required string Owner { get; init; }
        public DocumentStatus Status { get; init; }
        public DateTimeOffset CreatedDate { get; init; }
        public DateTimeOffset LastUpdatedDate { get; init; }
        public int Version { get; init; }
    }
}

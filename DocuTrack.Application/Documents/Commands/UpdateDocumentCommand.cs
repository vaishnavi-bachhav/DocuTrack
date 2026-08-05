using DocuTrack.Domain.Documents;

namespace DocuTrack.Application.Documents.Commands
{
    public class UpdateDocumentCommand
    {
        public required string Title { get; init; }
        public string? Description { get; init; }
        public DocumentType DocumentType { get; init; }
        public Department Department { get; init; }
        public required string Owner { get; init; }
        public int Version { get; init; }
    }
}

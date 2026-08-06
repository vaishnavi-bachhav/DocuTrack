using DocuTrack.Domain.Documents;

namespace DocuTrack.Application.Documents.Commands
{
    public sealed class ChangeDocumentStatusCommand
    {
        public Guid DocumentId { get; init; }
        public DocuTrack.Domain.Documents.DocumentStatus NewStatus { get; init; }
        public int Version { get; init; }
    }
}

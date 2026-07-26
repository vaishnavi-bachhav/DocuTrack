using DocuTrack.Core.Enums;

namespace DocuTrack.Core.Requests
{
    public sealed class ChangeDocumentStatusRequest
    {
        public Guid DocumentId { get; init; }
        public DocumentStatus NewStatus { get; init; }
    }
}

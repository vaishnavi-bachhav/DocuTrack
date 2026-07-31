using DocuTrack.Core.Enums;

namespace DocuTrack.Api.Contracts.Requests
{
    public sealed class ChangeDocumentStatusApiRequest
    {
        public DocumentStatus NewStatus { get; init; }
        public int Version { get; init; }
    }
}

using DocuTrack.Core.Enums;

namespace DocuTrack.Api.Contracts.Requests
{
    public class ChangeDocumentStatusApiRequest
    {
        public DocumentStatus NewStatus { get; set; }
    }
}

using DocuTrack.Domain.Documents;
using System.ComponentModel.DataAnnotations;

namespace DocuTrack.Api.Contracts.Requests
{
    public sealed class ChangeDocumentStatusApiRequest
    {
        [EnumDataType(typeof(DocumentStatus))]
        public DocumentStatus NewStatus { get; init; }

        [Range(1, int.MaxValue)]
        public int Version { get; init; }
    }
}

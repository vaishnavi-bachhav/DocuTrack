using DocuTrack.Core.Enums;

namespace DocuTrack.Core.Exceptions
{
    public sealed class DocumentDeletionNotAllowedException : Exception
    {
        public DocumentDeletionNotAllowedException(Guid documentId, DocumentStatus documentStatus) : base($"Document '{documentId}' cannot be deleted while its status is '{documentStatus}'.")
        {
        }
    }
}

namespace DocuTrack.Application.Common.Exceptions
{
    public sealed class DocumentConcurrencyException : Exception
    {
        public DocumentConcurrencyException(Guid documentId) : base($"Document '{documentId}' was modified or deleted by another request.")
        {
            DocumentId = documentId;
        }

        public DocumentConcurrencyException(Guid documentId, Exception innerException) : base($"Document '{documentId}' was modified or deleted by another request.", innerException)
        {
            DocumentId = documentId;
        }

        public Guid DocumentId { get; }

    }
}

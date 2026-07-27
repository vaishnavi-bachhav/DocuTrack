namespace DocuTrack.Core.Exceptions
{
    public sealed class DocumentNotFoundException : Exception
    {
        public DocumentNotFoundException(Guid documentId) : base($"No document was found with ID '{documentId}'.")
        {
            this.DocumentId = documentId;
        }
        public Guid DocumentId { get; }
    }
}

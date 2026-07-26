using DocuTrack.Core.Enums;

namespace DocuTrack.Core.Exceptions
{
    public sealed class InvalidDocumentStatusTransitionException : Exception
    {
        public InvalidDocumentStatusTransitionException(DocumentStatus currentStatus, DocumentStatus newStatus) : base($"Invalid status transition from {currentStatus} to {newStatus}.")
        {
            CurrentStatus = currentStatus;
            NewStatus = newStatus;
        }

        public DocumentStatus CurrentStatus { get; }
        public DocumentStatus NewStatus { get; }
    }
}

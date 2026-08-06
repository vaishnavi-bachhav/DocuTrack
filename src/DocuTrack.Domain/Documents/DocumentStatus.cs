namespace DocuTrack.Domain.Documents
{
    public enum DocumentStatus
    {
        Unknown,
        Draft,
        Uploaded,
        UnderReview,
        PendingApproval,
        Approved,
        Rejected,
        Archived
    }
}

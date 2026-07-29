using DocuTrack.Core.Enums;

namespace DocuTrack.Core.Models
{
    public class Document
    {
        public Guid Id { get; set; }
        public required string DocumentNumber { get; set; }
        public  required string Title { get; set; }
        public string? Description { get; set; }
        public DocumentType Type { get; set; }
        public Department Department { get; set; }
        public required string Owner { get; set; }
        public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
        public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.Now;
        public int Version { get; set; } = 1;
        public Guid CreatedByUserId { get; set; }
        public Guid? LastModifiedByUserId { get; set; }
    }
}

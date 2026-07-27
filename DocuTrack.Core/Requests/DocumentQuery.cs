using DocuTrack.Core.Enums;

namespace DocuTrack.Core.Requests
{
    public sealed class DocumentQuery
    {
        public string? Search { get; init; }
        public DocumentStatus? Status { get; init; }
        public Department? Department { get; init; }
        public string? Owner { get; init; }
        public DateTimeOffset? CreatedFrom { get; init; }
        public DateTimeOffset? CreatedTo { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public DocumentSortField SortBy { get; init; }
        public SortDirection SortDirection { get; init; }
    }
}

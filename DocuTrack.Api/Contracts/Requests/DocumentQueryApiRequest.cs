using DocuTrack.Core.Enums;

namespace DocuTrack.Api.Contracts.Requests
{
    public class DocumentQueryApiRequest
    {
        public string? Search { get; init; }
        public DocumentStatus? Status { get; init; }
        public Department? Department { get; init; }
        public string? Owner { get; init; }
        public DateTimeOffset? CreatedFrom { get; init; }
        public DateTimeOffset? CreatedTo { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public DocumentSortField SortBy { get; init; } = DocumentSortField.CreatedAt;
        public SortDirection SortDirection { get; init; } = SortDirection.Descending;
    }
}

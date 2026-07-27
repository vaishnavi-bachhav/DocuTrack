namespace DocuTrack.Core.Models
{
    public sealed class PagedResult<T>
    {
        public required IReadOnlyList<T> Items { get; init; }
        public required int PageNumber { get; init; }
        public required int PageSize { get; init; }
        public required int TotalCount { get; init; }
        public required int TotalPages { get; init; }
        public required bool HasPreviousPage { get; init; }
        public required bool HasNextPage { get; init; }
    }
}

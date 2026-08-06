using DocuTrack.Api.Contracts.Responses;
using DocuTrack.Application.Common;
using DocuTrack.Domain.Documents;

namespace DocuTrack.Api.Mappings
{
    public static class DocumentResponseMappings
    {
        public static DocumentResponse ToResponse(
       this Document document)
        {
            ArgumentNullException.ThrowIfNull(document);

            return new DocumentResponse
            {
                Id = document.Id,
                DocumentNumber = document.DocumentNumber,
                Title = document.Title,
                Description = document.Description,
                Type = document.Type,
                Department = document.Department,
                Owner = document.Owner,
                Status = document.Status,
                CreatedDate = document.CreatedAt,
                LastUpdatedDate = document.LastUpdatedAt,
                CreatedByUserId = document.CreatedByUserId,
                LastModifiedByUserId =
                    document.LastModifiedByUserId,
                Version = document.Version
            };
        }

        public static PagedResult<DocumentResponse> ToResponse(
        this PagedResult<Document> result)
        {
            ArgumentNullException.ThrowIfNull(result);

            return new PagedResult<DocumentResponse>
            {
                Items = result.Items
                    .Select(document => document.ToResponse())
                    .ToList(),

                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
                HasNextPage = result.HasNextPage,
                HasPreviousPage = result.HasPreviousPage
            };
        }
    }
}

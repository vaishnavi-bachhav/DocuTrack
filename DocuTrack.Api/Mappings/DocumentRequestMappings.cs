using DocuTrack.Api.Contracts.Requests;
using DocuTrack.Application.Documents.Commands;
using DocuTrack.Application.Documents.Queries;

namespace DocuTrack.Api.Mappings
{
    public static class DocumentRequestMappings
    {
        public static CreateDocumentCommand ToCommand(
       this CreateDocumentApiRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new CreateDocumentCommand
            {
                Title = request.Title.Trim(),
                Description = Normalize(request.Description),
                DocumentType = request.DocumentType,
                Department = request.Department,
                Owner = request.Owner.Trim()
            };
        }

        public static UpdateDocumentCommand ToCommand(
        this UpdateDocumentApiRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new UpdateDocumentCommand
            {
                Title = request.Title.Trim(),
                Description = Normalize(request.Description),
                DocumentType = request.DocumentType,
                Department = request.Department,
                Owner = request.Owner.Trim(),
                Version = request.Version
            };
        }

        public static ChangeDocumentStatusCommand ToCommand(
       this ChangeDocumentStatusApiRequest request,
       Guid documentId)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new ChangeDocumentStatusCommand
            {
                DocumentId = documentId,
                NewStatus = request.NewStatus,
                Version = request.Version
            };
        }

        public static DocumentQuery ToQuery(
       this DocumentQueryApiRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new DocumentQuery
            {
                Search = Normalize(request.Search),
                Status = request.Status,
                Department = request.Department,
                Owner = Normalize(request.Owner),
                CreatedFrom = request.CreatedFrom,
                CreatedTo = request.CreatedTo,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                SortDirection = request.SortDirection
            };
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

    }
}

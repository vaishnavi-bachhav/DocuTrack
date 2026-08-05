using DocuTrack.Application.Common;
using DocuTrack.Application.Documents.Commands;
using DocuTrack.Application.Documents.Queries;
using DocuTrack.Domain.Documents;

namespace DocuTrack.Application.Documents
{
    public interface IDocumentService
    {
        Task<Document> ChangeDocumentStatusAsync(ChangeDocumentStatusCommand command, CancellationToken cancellationToken = default);
        Task<Document> CreateDocumentAsync(CreateDocumentCommand command, CancellationToken cancellationToken = default);
        Task DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Document> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<PagedResult<Document>> SearchDocumentsAsync(DocumentQuery query, CancellationToken cancellationToken = default);
        Task<Document> UpdateDocumentAsync(Guid id, UpdateDocumentCommand command, CancellationToken cancellationToken = default);
    }
}
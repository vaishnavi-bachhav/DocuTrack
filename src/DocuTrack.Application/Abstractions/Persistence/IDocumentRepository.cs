using DocuTrack.Domain.Documents;
using DocuTrack.Application.Documents.Queries;
using DocuTrack.Application.Common;

namespace DocuTrack.Application.Abstractions.Persistence
{
    public interface IDocumentRepository
    {
        Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default);
        Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Document?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Document> UpdateAsync(Document document, int expectedVersion, CancellationToken cancellationToken = default);
        Task DeleteAsync(Document document, CancellationToken cancellationToken = default);
        Task<PagedResult<Document>> SearchAsync(DocumentQuery documentQuery, CancellationToken cancellationToken = default);
        void SetOriginalVersion(Document document, int originalVersion);
    }
}
using DocuTrack.Core.Models;
using DocuTrack.Core.Requests;

namespace DocuTrack.Core.Repositories
{
    public interface IDocumentRepository
    {
        Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default);
        Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Document?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
        Task<long> GetNextDocumentNumberAsync(CancellationToken cancellationToken = default);
        Task<Document> UpdateAsync(Document document, int expectedVersion, CancellationToken cancellationToken = default);
        Task DeleteAsync(Document document, CancellationToken cancellationToken = default);
        Task<PagedResult<Document>> SearchAsync(DocumentQuery documentQuery, CancellationToken cancellationToken = default);
        void SetOriginalVersion(Document document, int originalVersion);
    }
}

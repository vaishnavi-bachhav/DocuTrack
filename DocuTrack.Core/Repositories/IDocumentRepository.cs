using DocuTrack.Core.Models;

namespace DocuTrack.Core.Repositories
{
    public interface IDocumentRepository
    {
        Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<long> GetNextDocumentNumberAsync(CancellationToken cancellationToken = default);
    }
}

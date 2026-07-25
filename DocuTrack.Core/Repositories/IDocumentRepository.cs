using DocuTrack.Core.Models;

namespace DocuTrack.Core.Repositories
{
    public interface IDocumentRepository
    {
        public Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default);
        public Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default);
        public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    }
}

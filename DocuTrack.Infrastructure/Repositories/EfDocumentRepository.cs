using DocuTrack.Core.Models;
using DocuTrack.Core.Repositories;
using DocuTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocuTrack.Infrastructure.Repositories
{
    public sealed class EfDocumentRepository : IDocumentRepository
    {
        private readonly DocuTrackDbContext _context;
        public EfDocumentRepository(DocuTrackDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(document);
            await _context.Documents.AddAsync(document, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return document;
        }

        public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Documents.AsNoTracking().OrderByDescending(o => o.CreatedAt).ToListAsync(cancellationToken);
        }

        public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }
    }
}

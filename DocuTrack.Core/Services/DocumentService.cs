using DocuTrack.Core.Models;
using DocuTrack.Core.Repositories;
using DocuTrack.Core.Requests;

namespace DocuTrack.Core.Services
{
    public sealed class DocumentService
    {
        private readonly IDocumentRepository _documentRepository;

        public DocumentService(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        }

        public async Task<Document> CreateDocumentAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            long nextNumber = await _documentRepository.GetNextDocumentNumberAsync(cancellationToken);
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var document = new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = $"DOC-{nextNumber:D6}",
                Title = request.Title,
                Description = request.Description,
                Type = request.DocumentType,
                Department = request.Department,
                Owner = request.Owner,
                Status = Enums.DocumentStatus.Draft,
                CreatedAt = now,
                LastUpdatedAt = now,
                Version = 1
            };
            return await _documentRepository.AddAsync(document, cancellationToken);
        }

        public async Task<IReadOnlyList<Document>> GetAllDocumentsAsync(CancellationToken cancellationToken = default)
        {
            return await _documentRepository.GetAllAsync(cancellationToken);
        }

        public async Task<Document?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _documentRepository.GetByIdAsync(id, cancellationToken);
        }

        public async Task<Document?> UpdateDocumentAsync(Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            Document? existingDocument = await _documentRepository.GetByIdAsync(id, cancellationToken);
            if (existingDocument is null)
            {
                return null;
            }
            existingDocument.Title = request.Title;
            existingDocument.Description = request.Description;
            existingDocument.Type = request.DocumentType;
            existingDocument.Department = request.Department;
            existingDocument.Owner = request.Owner;
            existingDocument.LastUpdatedAt = DateTimeOffset.UtcNow;
            existingDocument.Version++;
            return await _documentRepository.UpdateAsync(existingDocument, cancellationToken);
        }
    }
}

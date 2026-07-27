using DocuTrack.Core.Models;
using DocuTrack.Core.Repositories;
using DocuTrack.Core.Requests;
using DocuTrack.Core.Enums;
using DocuTrack.Core.Exceptions;

namespace DocuTrack.Core.Services
{
    public sealed class DocumentService
    {
        private readonly IDocumentRepository _documentRepository;

        public DocumentService(IDocumentRepository documentRepository)
        {
            ArgumentNullException.ThrowIfNull(documentRepository);
            _documentRepository = documentRepository;
        }

        public async Task<Document> CreateDocumentAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

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
            ArgumentNullException.ThrowIfNull(request);
            Document? existingDocument = await _documentRepository.GetByIdForUpdateAsync(id, cancellationToken);
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

        public async Task<Document?> ChangeDocumentStatusAsync(ChangeDocumentStatusRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            Document? existingDocument = await _documentRepository.GetByIdForUpdateAsync(request.DocumentId, cancellationToken);
            if (existingDocument is null)
            {
                return null;
            }

            if (!IsValidStatusTransition(existingDocument.Status, request.NewStatus))
            {
                throw new InvalidDocumentStatusTransitionException(existingDocument.Status, request.NewStatus);
            }
            
            existingDocument.Status = request.NewStatus;
            existingDocument.LastUpdatedAt = DateTimeOffset.UtcNow;
            existingDocument.Version++;
            return await _documentRepository.UpdateAsync(existingDocument, cancellationToken);
        }

        public async Task<bool> DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Document? existingDocument = await _documentRepository.GetByIdForUpdateAsync(id, cancellationToken);
            if (existingDocument is null)
            {
                return false;
            }

            bool canDelete = existingDocument.Status is DocumentStatus.Draft or DocumentStatus.Rejected;
            if(!canDelete)
            {
                throw new DocumentDeletionNotAllowedException(id, existingDocument.Status);
            }
            await _documentRepository.DeleteAsync(existingDocument, cancellationToken);
            return true;
        }

        public async Task<PagedResult<Document>> SearchDocumentsAsync(DocumentQuery request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _documentRepository.SearchAsync(request, cancellationToken);
        }

        private static bool IsValidStatusTransition(DocumentStatus currentStatus, DocumentStatus newStatus)
        {
            return (currentStatus, newStatus) switch
            {
                (DocumentStatus.Draft, DocumentStatus.Uploaded) => true,
                (DocumentStatus.Uploaded, DocumentStatus.UnderReview) => true,
                (DocumentStatus.UnderReview, DocumentStatus.PendingApproval) => true,
                (DocumentStatus.PendingApproval, DocumentStatus.Approved) => true,
                (DocumentStatus.PendingApproval, DocumentStatus.Rejected) => true,
                (DocumentStatus.Rejected, DocumentStatus.Draft) => true,
                (DocumentStatus.Approved, DocumentStatus.Archived) => true,
                _ => false,
            };
        }
    }
}

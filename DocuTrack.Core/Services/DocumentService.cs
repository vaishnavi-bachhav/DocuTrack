using DocuTrack.Core.Models;
using DocuTrack.Core.Repositories;
using DocuTrack.Core.Requests;
using DocuTrack.Core.Enums;
using DocuTrack.Core.Exceptions;
using DocuTrack.Core.Identity;

namespace DocuTrack.Core.Services
{
    public sealed class DocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly ICurrentUser _currentUser;

        public DocumentService(IDocumentRepository documentRepository, ICurrentUser currentUser)
        {
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        public async Task<Document> CreateDocumentAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            ValidateDocumentDetails(request.Title, request.Owner, request.DocumentType, request.Department);

            if (!_currentUser.IsAuthenticated)
            {
                throw new InvalidOperationException("An authenticated user is required to create a document.");
            }

            long nextNumber = await _documentRepository.GetNextDocumentNumberAsync(cancellationToken);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Guid userId = _currentUser.UserId;

            var document = new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = $"DOC-{nextNumber:D6}",
                Title = request.Title,
                Description = request.Description,
                Type = request.DocumentType,
                Department = request.Department,
                Owner = request.Owner,
                Status = DocumentStatus.Draft,
                CreatedAt = now,
                LastUpdatedAt = now,
                CreatedByUserId = userId,
                LastModifiedByUserId = userId,
                Version = 1
            };
            return await _documentRepository.AddAsync(document, cancellationToken);
        }

        public async Task<Document> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Document? document = await _documentRepository.GetByIdAsync(id, cancellationToken);
            return document ?? throw new DocumentNotFoundException(id);
        }

        private async Task<Document> GetDocumentForUpdateAsync(
    Guid id,
    CancellationToken cancellationToken)
        {
            Document? document =
                await _documentRepository.GetByIdForUpdateAsync(
                    id,
                    cancellationToken);

            return document
                ?? throw new DocumentNotFoundException(id);
        }
        public async Task<Document> UpdateDocumentAsync(Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            ValidateDocumentDetails(request.Title, request.Owner, request.DocumentType, request.Department);

            if (request.Version < 1)
            {
                throw new DomainValidationException("Version must be greater than or equal to 1.");
            }

            Document existingDocument = await GetDocumentForUpdateAsync(id, cancellationToken);

            existingDocument.Title = request.Title;
            existingDocument.Description = request.Description;
            existingDocument.Type = request.DocumentType;
            existingDocument.Department = request.Department;
            existingDocument.Owner = request.Owner;
            existingDocument.LastModifiedByUserId = _currentUser.UserId;
            existingDocument.LastUpdatedAt = DateTimeOffset.UtcNow;
            existingDocument.Version = request.Version + 1;

            return await _documentRepository.UpdateAsync(existingDocument, request.Version, cancellationToken);
        }

        private static void ValidateDocumentDetails(string title, string owner, DocumentType documentType, Department department)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new DomainValidationException("Document title is required.");
            }
            if (string.IsNullOrWhiteSpace(owner))
            {
                throw new DomainValidationException("Document owner is required.");
            }
            if (documentType == DocumentType.Unknown)
            {
                throw new DomainValidationException("A valid document type is required.");
            }
            if (department == Department.Unknown)
            {
                throw new DomainValidationException("A valid department is required.");
            }
        }

        public async Task<Document> ChangeDocumentStatusAsync(ChangeDocumentStatusRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Version < 1)
            {
                throw new DomainValidationException("Version must be greater than or equal to 1.");
            }

            if (request.NewStatus == DocumentStatus.Unknown)
            {
                throw new DomainValidationException("A valid new document status is required.");
            }

            Document existingDocument = await GetDocumentForUpdateAsync(request.DocumentId, cancellationToken);

            if (!IsValidStatusTransition(existingDocument.Status, request.NewStatus))
            {
                throw new InvalidDocumentStatusTransitionException(existingDocument.Status, request.NewStatus);
            }
            
            existingDocument.Status = request.NewStatus;
            existingDocument.LastModifiedByUserId = _currentUser.UserId;
            existingDocument.LastUpdatedAt = DateTimeOffset.UtcNow;
            existingDocument.Version = request.Version + 1;

            return await _documentRepository.UpdateAsync(existingDocument, request.Version, cancellationToken);
        }

        public async Task DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Document existingDocument = await GetDocumentForUpdateAsync(id, cancellationToken);

            bool canDelete = existingDocument.Status is DocumentStatus.Draft or DocumentStatus.Rejected;
            if(!canDelete)
            {
                throw new DocumentDeletionNotAllowedException(id, existingDocument.Status);
            }
            await _documentRepository.DeleteAsync(existingDocument, cancellationToken);
        }

        public async Task<PagedResult<Document>> SearchDocumentsAsync(DocumentQuery request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if(request.PageNumber < 1)
            {
                throw new DomainValidationException("Page number must be greater than or equal to 1.");
            }
            if(request.PageSize < 1 || request.PageSize > 100)
            {
                throw new DomainValidationException("Page size must be between 1 and 100.");
            }
            if(request.Status == DocumentStatus.Unknown)
            {
                throw new DomainValidationException("A valid document status is required.");
            }
            if(request.Department == Department.Unknown)
            {
                throw new DomainValidationException("A valid department is required.");
            }
            if (request.CreatedFrom.HasValue && request.CreatedTo.HasValue && request.CreatedFrom > request.CreatedTo)
            {
                throw new DomainValidationException("CreatedFrom date cannot be later than CreatedTo date.");
            }
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

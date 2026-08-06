using DocuTrack.Application.Abstractions.Authorization;
using DocuTrack.Application.Abstractions.Persistence;
using DocuTrack.Application.Abstractions.Time;
using DocuTrack.Application.Common;
using DocuTrack.Application.Common.Exceptions;
using DocuTrack.Application.Documents.Commands;
using DocuTrack.Application.Documents.Queries;
using DocuTrack.Domain.Documents;

namespace DocuTrack.Application.Documents
{
    public sealed class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentNumberGenerator _documentNumberGenerator;
        private readonly ICurrentUser _currentUser;
        private readonly IClock _clock;


        public DocumentService(IDocumentRepository documentRepository,
            IDocumentNumberGenerator documentNumberGenerator,
            ICurrentUser currentUser,
            IClock clock)
        {
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
            _documentNumberGenerator = documentNumberGenerator ?? throw new ArgumentNullException(nameof(documentNumberGenerator));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<Document> CreateDocumentAsync(CreateDocumentCommand command, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);

            EnsureAuthenticatedUser();

            string documentNumber = await _documentNumberGenerator.GenerateAsync(cancellationToken);

            Document document = Document.Create(
            documentNumber: documentNumber,
            title: command.Title,
            description: command.Description,
            type: command.DocumentType,
            department: command.Department,
            owner: command.Owner,
            createdByUserId: _currentUser.UserId,
            createdAt: _clock.UtcNow);

            return await _documentRepository.AddAsync(document, cancellationToken);
        }

        public async Task<Document> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            EnsureValidDocumentId(id);

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

        public async Task<Document> UpdateDocumentAsync(Guid id, UpdateDocumentCommand command, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);

            EnsureValidDocumentId(id);
            EnsureValidVersion(command.Version);
            EnsureAuthenticatedUser();

            Document document =
                await GetDocumentForUpdateAsync(
                    id,
                    cancellationToken);

            document.UpdateDetails(
                title: command.Title,
                description: command.Description,
                type: command.DocumentType,
                department: command.Department,
                owner: command.Owner,
                modifiedByUserId: _currentUser.UserId,
                modifiedAt: _clock.UtcNow);

            return await _documentRepository.UpdateAsync(
                document,
                expectedVersion: command.Version,
                cancellationToken);
        }

        public async Task<Document> ChangeDocumentStatusAsync(
        ChangeDocumentStatusCommand command,
        CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);

            EnsureValidDocumentId(command.DocumentId);
            EnsureValidVersion(command.Version);
            EnsureAuthenticatedUser();

            if (command.NewStatus == DocumentStatus.Unknown)
            {
                throw new UseCaseValidationException(
                    "A valid new document status is required.");
            }

            Document document =
                await GetDocumentForUpdateAsync(
                    command.DocumentId,
                    cancellationToken);

            document.ChangeStatus(
                newStatus: command.NewStatus,
                modifiedByUserId: _currentUser.UserId,
                modifiedAt: _clock.UtcNow);

            return await _documentRepository.UpdateAsync(
                document,
                expectedVersion: command.Version,
                cancellationToken);
        }
        public async Task DeleteDocumentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        {
            EnsureValidDocumentId(id);
            EnsureAuthenticatedUser();

            Document document =
                await GetDocumentForUpdateAsync(
                    id,
                    cancellationToken);

            document.EnsureCanDelete();

            await _documentRepository.DeleteAsync(
                document,
                cancellationToken);
        }


        public async Task<PagedResult<Document>> SearchDocumentsAsync(
       DocumentQuery query,
       CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            ValidateQuery(query);

            return await _documentRepository.SearchAsync(
                query,
                cancellationToken);
        }

        private void EnsureAuthenticatedUser()
        {
            if (!_currentUser.IsAuthenticated)
            {
                throw new UnauthenticatedUserException();
            }

            if (_currentUser.UserId == Guid.Empty)
            {
                throw new UnauthenticatedUserException();
            }
        }

        private static void EnsureValidDocumentId(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new UseCaseValidationException("Document ID is required.");
            }
        }

        private static void EnsureValidVersion(int version)
        {
            if (version < 1)
            {
                throw new UseCaseValidationException("Version must be greater than or equal to 1.");
            }
        }

        private static void ValidateQuery(DocumentQuery query)
        {
            if (query.PageNumber < 1)
            {
                throw new UseCaseValidationException(
                    "Page number must be greater than or equal to 1.");
            }

            if (query.PageSize is < 1 or > 100)
            {
                throw new UseCaseValidationException(
                    "Page size must be between 1 and 100.");
            }

            if (query.Status == DocumentStatus.Unknown)
            {
                throw new UseCaseValidationException(
                    "A valid document status is required.");
            }

            if (query.Department == Department.Unknown)
            {
                throw new UseCaseValidationException(
                    "A valid department is required.");
            }

            if (query.CreatedFrom.HasValue &&
                query.CreatedTo.HasValue &&
                query.CreatedFrom.Value >
                query.CreatedTo.Value)
            {
                throw new UseCaseValidationException(
                    "CreatedFrom date cannot be later than CreatedTo date.");
            }
        }

    }
}
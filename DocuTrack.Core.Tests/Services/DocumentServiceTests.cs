using DocuTrack.Core.Enums;
using DocuTrack.Core.Exceptions;
using DocuTrack.Core.Identity;
using DocuTrack.Core.Models;
using DocuTrack.Core.Repositories;
using DocuTrack.Core.Requests;
using DocuTrack.Core.Services;
using FluentAssertions;
using Moq;

namespace DocuTrack.Core.Tests.Services
{
    public sealed class DocumentServiceTests
    {
        private readonly Mock<IDocumentRepository> _repositoryMock;
        private readonly DocumentService _documentService;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private static readonly Guid TestUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public DocumentServiceTests()
        {
            _repositoryMock = new Mock<IDocumentRepository>();
            _currentUserMock = new Mock<ICurrentUser>();
            _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
            _currentUserMock.Setup(user => user.UserId).Returns(TestUserId);
            _documentService = new DocumentService(_repositoryMock.Object, _currentUserMock.Object);
        }

        private static Document CreateDocument(DocumentStatus status = DocumentStatus.Draft, int version = 1)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            return new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "DOC-000001",
                Title = "Supplier Agreement",
                Description = "Annual supplier agreement",
                Type = DocumentType.Contract,
                Department = Department.Purchasing,
                Owner = "Vaishnavi Bachhav",
                Status = status,
                CreatedAt = now,
                LastUpdatedAt = now,
                CreatedByUserId = TestUserId,
                LastModifiedByUserId = TestUserId,
                Version = version
            };
        }

        [Fact]
        public async Task CreateDocumentAsync_ValidRequest_ReturnsCreatedDocument()
        {
            // Arrange
            var request = new CreateDocumentRequest
            {
                Title = "Supplier Agreement",
                Description = "Annual supplier agreement",
                DocumentType = DocumentType.Contract,
                Department = Department.Purchasing,
                Owner = "Vaishnavi Bachhav"
            };
            
            _repositoryMock.Setup(r => r.GetNextDocumentNumberAsync(It.IsAny<CancellationToken>())).ReturnsAsync(25);

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Document doc, CancellationToken _) => doc);

            // Act
            var result = await _documentService.CreateDocumentAsync(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBe(Guid.Empty);
            result.DocumentNumber.Should().Be("DOC-000025");
            result.Title.Should().Be(request.Title);
            result.Description.Should().Be(request.Description);
            result.Type.Should().Be(DocumentType.Contract);
            result.Department.Should().Be(Department.Purchasing);
            result.Owner.Should().Be(request.Owner);
            result.Status.Should().Be(DocumentStatus.Draft);
            result.Version.Should().Be(1);
            result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
            result.CreatedByUserId.Should().Be(_currentUserMock.Object.UserId);
            result.LastModifiedByUserId.Should().Be(_currentUserMock.Object.UserId);

            _repositoryMock.Verify(r => r.AddAsync(It.Is<Document>(document => 
            document.DocumentNumber == "DOC-000025" && document.Status == DocumentStatus.Draft), 
            It.IsAny<CancellationToken>()), Times.Once);

            _repositoryMock.Verify(repository => repository.GetNextDocumentNumberAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // Validation test
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateDocumentAsync_InvalidTitle_ThrowsDomainValidationException(string title)
        {
            // Arrange
            var request = new CreateDocumentRequest
            {
                Title = title,
                Description = null,
                DocumentType = DocumentType.Contract,
                Department = Department.Purchasing,
                Owner = "Vaishnavi Bachhav"
            };
            
            // Act
            Func<Task> action = async () => await _documentService.CreateDocumentAsync(request, CancellationToken.None);
            
            // Assert
            await action.Should().ThrowAsync<DomainValidationException>()
                .WithMessage("*title*");

            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // Unknown document type test
        [Fact]
        public async Task CreateDocumentAsync_UnknownDocumentType_ThrowsDomainValidationException()
        {
            // Arrange
            var request = new CreateDocumentRequest
            {
                Title = "Supplier Agreement",
                Description = "Annual supplier agreement",
                DocumentType = DocumentType.Unknown, // Invalid document type
                Department = Department.Purchasing,
                Owner = "Vaishnavi Bachhav"
            };

            // Act
            Func<Task> action = async () => await _documentService.CreateDocumentAsync(request, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<DomainValidationException>()
                .WithMessage("*document type*");
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetDocumentByIdAsync_DocumentExists_ReturnsDocument()
        {
            // Arrange
            var document = CreateDocument();

            _repositoryMock.Setup(r => r.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(document);
            
            // Act
            var result = await _documentService.GetDocumentByIdAsync(document.Id, CancellationToken.None);

            // Assert
            result.Should().BeSameAs(document);
            result.Should().NotBeNull();
            result.Id.Should().Be(document.Id);
            result.DocumentNumber.Should().Be(document.DocumentNumber);
            result.Title.Should().Be(document.Title);
        }

        [Fact]
        public async Task GetDocumentByIdAsync_DocumentDoesNotExist_ThrowsDocumentNotFoundException()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Document?)null);
            
            // Act
            Func<Task> action = async () => await _documentService.GetDocumentByIdAsync(documentId, CancellationToken.None);
            
            // Assert
            DocumentNotFoundException exception = (await action.Should().ThrowAsync<DocumentNotFoundException>()).Which;
            exception.DocumentId.Should().Be(documentId);
        }

        [Fact]
        public async Task UpdateDocumentAsync_ValidRequest_UpdatesDocument()
        {
            // Arrange
            var existingDocument = CreateDocument(version: 1);
            DateTimeOffset originalUpdatedDate = existingDocument.LastUpdatedAt;
            var updateRequest = new UpdateDocumentRequest
            {
                Title = "Updated Title",
                Description = "Updated Description",
                DocumentType = DocumentType.ComplianceDocument,
                Department = Department.InformationTechnology,
                Version = 1,
                Owner = "John Doe"
            };
            _repositoryMock.Setup(r => r.GetByIdForUpdateAsync(existingDocument.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingDocument);
            _repositoryMock.Setup(r => r.UpdateAsync(existingDocument, updateRequest.Version, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingDocument);
            
            // Act
            var result = await _documentService.UpdateDocumentAsync(existingDocument.Id, updateRequest, CancellationToken.None);
            
            // Assert
            result.Title.Should().Be(updateRequest.Title);
            result.Description.Should().Be(updateRequest.Description);
            result.Type.Should().Be(updateRequest.DocumentType);
            result.Department.Should().Be(updateRequest.Department);
            result.Owner.Should().Be(updateRequest.Owner);
            result.Version.Should().Be(2);
            result.LastModifiedByUserId.Should().Be(TestUserId);

            result.LastUpdatedAt.Should().BeAfter(originalUpdatedDate);

            _repositoryMock.Verify(r => r.UpdateAsync(existingDocument, 1, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task UpdateDocumentAsync_DocumentDoesNotExist_ThrowsDocumentNotFoundException()
        {
            // Arrange
            Guid documentId = Guid.NewGuid();

            UpdateDocumentRequest request = new()
            {
                Title = "Updated title",
                Description = null,
                DocumentType = DocumentType.Contract,
                Department = Department.Purchasing,
                Version = 2,
                Owner = "John Doe"
            };

            _repositoryMock.Setup(r => r.GetByIdForUpdateAsync(documentId, It.IsAny<CancellationToken>())).ReturnsAsync((Document?)null);

            // Act
            Func<Task> action = () => _documentService.UpdateDocumentAsync(documentId, request);

            await action.Should().ThrowAsync<DocumentNotFoundException>();

            // Assert
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Document>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // Valid transition
        [Fact]
        public async Task ChangeDocumentStatusAsync_DraftToUploaded_UpdatesStatus()
        {
            // Arrange
            Document document = CreateDocument(DocumentStatus.Draft, version: 1);

            ChangeDocumentStatusRequest request = new()
            {
                DocumentId = document.Id,
                Version = 1,
                NewStatus = DocumentStatus.Uploaded
            };

            _repositoryMock.Setup(r => r.GetByIdForUpdateAsync(document.Id, It.IsAny<CancellationToken>())).ReturnsAsync(document);
            _repositoryMock.Setup(r => r.UpdateAsync(document, request.Version, It.IsAny<CancellationToken>())).ReturnsAsync(document);

            // Act
            Document result = await _documentService.ChangeDocumentStatusAsync(request);

            // Assert
            result.Status.Should().Be(DocumentStatus.Uploaded);
            result.Version.Should().Be(2);
            result.LastModifiedByUserId.Should().Be(TestUserId);

            _repositoryMock.Verify(r => r.UpdateAsync(document, 1, It.IsAny<CancellationToken>()), Times.Once);
        }

        // Invalid transition
        [Fact]
        public async Task ChangeDocumentStatusAsync_DraftToApproved_ThrowsInvalidTransitionException()
        {
            // Arrange
            Document document = CreateDocument(DocumentStatus.Draft, version: 1);

            ChangeDocumentStatusRequest request = new()
            {
                DocumentId = document.Id,
                Version = 1,
                NewStatus = DocumentStatus.Approved
            };

            _repositoryMock.Setup(r => r.GetByIdForUpdateAsync(document.Id, It.IsAny<CancellationToken>())).ReturnsAsync(document);

            // Act
            Func<Task> action = () => _documentService.ChangeDocumentStatusAsync(request);

            // Assert
            await action.Should().ThrowAsync<InvalidDocumentStatusTransitionException>();

            document.Status.Should().Be(DocumentStatus.Draft);
            document.Version.Should().Be(1);

            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Document>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // Test all valid transitions
        [Theory]
        [InlineData(DocumentStatus.Draft, DocumentStatus.Uploaded)]
        [InlineData(DocumentStatus.Uploaded, DocumentStatus.UnderReview)]
        [InlineData(DocumentStatus.UnderReview, DocumentStatus.PendingApproval)]
        [InlineData(DocumentStatus.PendingApproval, DocumentStatus.Approved)]
        [InlineData(DocumentStatus.PendingApproval, DocumentStatus.Rejected)]
        [InlineData(DocumentStatus.Rejected, DocumentStatus.Draft)]
        [InlineData(DocumentStatus.Approved, DocumentStatus.Archived)]
        public async Task ChangeDocumentStatusAsync_ValidTransition_UpdatesStatus(DocumentStatus currentStatus, DocumentStatus newStatus)
        {
            Document document = CreateDocument(currentStatus, version: 1);

            ChangeDocumentStatusRequest request = new()
            {
                DocumentId = document.Id,
                NewStatus = newStatus,
                Version = 1
            };

            _repositoryMock.Setup(r => r.GetByIdForUpdateAsync(document.Id, It.IsAny<CancellationToken>())).ReturnsAsync(document);
            _repositoryMock.Setup(r => r.UpdateAsync(document, request.Version, It.IsAny<CancellationToken>())).ReturnsAsync(document);

            Document result = await _documentService.ChangeDocumentStatusAsync(request);

            result.Status.Should().Be(newStatus);
            result.Version.Should().Be(2);

            _repositoryMock.Verify(r => r.UpdateAsync(document, 1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateDocumentAsync_InvalidVersion_ThrowsDomainValidationException()
        {
            UpdateDocumentRequest request = new()
            {
                Title = "Updated title",
                Description = null,
                DocumentType = DocumentType.Contract,
                Department = Department.Purchasing,
                Owner = "John Doe",
                Version = 0
            };

            Func<Task> action = () =>
                _documentService.UpdateDocumentAsync(
                    Guid.NewGuid(),
                    request);

            await action.Should()
                .ThrowAsync<DomainValidationException>()
                .WithMessage("*Version*");

            _repositoryMock.Verify(
                repository =>
                    repository.GetByIdForUpdateAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ChangeDocumentStatusAsync_InvalidVersion_ThrowsDomainValidationException()
        {
            ChangeDocumentStatusRequest request = new()
            {
                DocumentId = Guid.NewGuid(),
                NewStatus = DocumentStatus.Uploaded,
                Version = 0
            };

            Func<Task> action = () =>
                _documentService.ChangeDocumentStatusAsync(
                    request);

            await action.Should()
                .ThrowAsync<DomainValidationException>()
                .WithMessage("*Version*");

            _repositoryMock.Verify(
                repository =>
                    repository.GetByIdForUpdateAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateDocumentAsync_RepositoryConcurrencyConflict_PropagatesException()
        {
            // Arrange
            Document document =
                CreateDocument(version: 1);

            UpdateDocumentRequest request = new()
            {
                Title = "Updated title",
                Description = "Updated description",
                DocumentType = DocumentType.Contract,
                Department = Department.Purchasing,
                Owner = "John Doe",
                Version = 1
            };

            _repositoryMock
                .Setup(repository =>
                    repository.GetByIdForUpdateAsync(
                        document.Id,
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(document);

            _repositoryMock
                .Setup(repository =>
                    repository.UpdateAsync(
                        document,
                        request.Version,
                        It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new DocumentConcurrencyException(
                        document.Id));

            // Act
            Func<Task> action = () =>
                _documentService.UpdateDocumentAsync(
                    document.Id,
                    request);

            // Assert
            DocumentConcurrencyException exception =
                (await action.Should()
                    .ThrowAsync<DocumentConcurrencyException>())
                .Which;

            exception.DocumentId.Should().Be(document.Id);
        }

        [Fact]
        public async Task CreateDocumentAsync_UnauthenticatedUser_ThrowsException()
        {
            // Arrange
            _currentUserMock
                .Setup(user => user.IsAuthenticated)
                .Returns(false);

            CreateDocumentRequest request = new()
            {
                Title = "Supplier Agreement",
                Description = null,
                DocumentType = DocumentType.Contract,
                Department = Department.Purchasing,
                Owner = "Vaishnavi Bachhav"
            };

            // Act
            Func<Task> action = async () =>  await _documentService.CreateDocumentAsync(request);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>();

            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // Draft can be deleted
        [Fact]
        public async Task DeleteDocumentAsync_DraftDocument_DeletesDocument()
        {
            Document document = CreateDocument(DocumentStatus.Draft);

            _repositoryMock.Setup(r => r.GetByIdForUpdateAsync(document.Id, It.IsAny<CancellationToken>())).ReturnsAsync(document);

            await _documentService.DeleteDocumentAsync(document.Id);

            _repositoryMock.Verify(r => r.DeleteAsync(document, It.IsAny<CancellationToken>()), Times.Once);
        }

        // Rejected can be deleted
        [Fact]
        public async Task DeleteDocumentAsync_RejectedDocument_DeletesDocument()
        {
            Document document = CreateDocument(DocumentStatus.Rejected);

            _repositoryMock.Setup(r => r.GetByIdForUpdateAsync(document.Id, It.IsAny<CancellationToken>())).ReturnsAsync(document);

            await _documentService.DeleteDocumentAsync(document.Id);

            _repositoryMock.Verify(r => r.DeleteAsync(document, It.IsAny<CancellationToken>()), Times.Once);
        }

        // Approved cannot be deleted
        [Theory]
        [InlineData(DocumentStatus.Uploaded)]
        [InlineData(DocumentStatus.UnderReview)]
        [InlineData(DocumentStatus.PendingApproval)]
        [InlineData(DocumentStatus.Approved)]
        [InlineData(DocumentStatus.Archived)]
        public async Task DeleteDocumentAsync_NonDeletableStatus_ThrowsDeletionNotAllowedException(DocumentStatus status)
        {
            Document document = CreateDocument(status);

            _repositoryMock.Setup(r => r.GetByIdForUpdateAsync(document.Id, It.IsAny<CancellationToken>())).ReturnsAsync(document);

            Func<Task> action = () => _documentService.DeleteDocumentAsync(document.Id);

            await action.Should().ThrowAsync<DocumentDeletionNotAllowedException>();

            _repositoryMock.Verify(repository => repository.DeleteAsync(It.IsAny<Document>(),It.IsAny<CancellationToken>()), Times.Never);
        }

        // Test search validation
        [Fact]
        public async Task SearchDocumentsAsync_PageNumberBelowOne_ThrowsDomainValidationException()
        {
            DocumentQuery query = new()
            {
                PageNumber = 0,
                PageSize = 20,
                SortBy = DocumentSortField.CreatedAt,
                SortDirection = SortDirection.Descending
            };

            Func<Task> action = () => _documentService.SearchDocumentsAsync(query);

            await action.Should().ThrowAsync<DomainValidationException>();

            _repositoryMock.Verify(r => r.SearchAsync(It.IsAny<DocumentQuery>(), It.IsAny<CancellationToken>()),Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public async Task SearchDocumentsAsync_InvalidPageSize_ThrowsDomainValidationException(int pageSize)
        {
            DocumentQuery query = new()
            {
                PageNumber = 1,
                PageSize = pageSize,
                SortBy = DocumentSortField.CreatedAt,
                SortDirection = SortDirection.Descending
            };

            Func<Task> action = () => _documentService.SearchDocumentsAsync(query);

            await action.Should().ThrowAsync<DomainValidationException>();
        }

        [Fact]
        public async Task SearchDocumentsAsync_ValidQuery_ReturnsRepositoryResult()
        {
            DocumentQuery query = new()
            {
                Search = "supplier",
                PageNumber = 1,
                PageSize = 20,
                SortBy = DocumentSortField.CreatedAt,
                SortDirection = SortDirection.Descending
            };

            PagedResult<Document> expected = new()
            {
                Items = new List<Document>
                {
                    CreateDocument()
                },
                PageNumber = 1,
                PageSize = 20,
                TotalCount = 1,
                TotalPages = 1,
                HasPreviousPage = false,
                HasNextPage = false
            };

            _repositoryMock.Setup(r => r.SearchAsync(query, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            PagedResult<Document> result = await _documentService.SearchDocumentsAsync(query);

            result.Should().BeSameAs(expected);
        }
    }
}
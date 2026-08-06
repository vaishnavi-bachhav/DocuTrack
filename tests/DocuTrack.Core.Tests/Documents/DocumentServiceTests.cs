using DocuTrack.Application.Abstractions.Authorization;
using DocuTrack.Application.Abstractions.Persistence;
using DocuTrack.Application.Abstractions.Time;
using DocuTrack.Application.Common;
using DocuTrack.Application.Common.Exceptions;
using DocuTrack.Application.Documents;
using DocuTrack.Application.Documents.Commands;
using DocuTrack.Application.Documents.Queries;
using DocuTrack.Domain.Documents;
using DocuTrack.Domain.Documents.Exceptions;
using FluentAssertions;
using Moq;

namespace DocuTrack.Application.Tests.Documents;

public sealed class DocumentServiceTests
{
    private static readonly Guid CurrentUserId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly DateTimeOffset CurrentTime =
        new(
            2026,
            8,
            5,
            12,
            0,
            0,
            TimeSpan.Zero);

    private readonly Mock<IDocumentRepository> _repositoryMock;
    private readonly Mock<IDocumentNumberGenerator> _numberGeneratorMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IClock> _clockMock;

    private readonly DocumentService _service;

    public DocumentServiceTests()
    {
        _repositoryMock = new Mock<IDocumentRepository>();
        _numberGeneratorMock = new Mock<IDocumentNumberGenerator>();
        _currentUserMock = new Mock<ICurrentUser>();
        _clockMock = new Mock<IClock>();

        _currentUserMock
            .Setup(user => user.IsAuthenticated)
            .Returns(true);

        _currentUserMock
            .Setup(user => user.UserId)
            .Returns(CurrentUserId);

        _clockMock
            .Setup(clock => clock.UtcNow)
            .Returns(CurrentTime);

        _service = new DocumentService(
            _repositoryMock.Object,
            _numberGeneratorMock.Object,
            _currentUserMock.Object,
            _clockMock.Object);
    }

    [Fact]
    public async Task CreateDocumentAsync_ValidCommand_CreatesDocument()
    {
        CreateDocumentCommand command = CreateCommand();

        _numberGeneratorMock
            .Setup(generator =>
                generator.GenerateAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync("DOC-000001");

        _repositoryMock
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<Document>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (Document document, CancellationToken _) =>
                    document);

        Document result =
            await _service.CreateDocumentAsync(command);

        result.DocumentNumber.Should().Be("DOC-000001");
        result.Title.Should().Be(command.Title);
        result.Description.Should().Be(command.Description);
        result.Type.Should().Be(command.DocumentType);
        result.Department.Should().Be(command.Department);
        result.Owner.Should().Be(command.Owner);
        result.Status.Should().Be(DocumentStatus.Draft);
        result.Version.Should().Be(1);
        result.CreatedByUserId.Should().Be(CurrentUserId);
        result.LastModifiedByUserId.Should().Be(CurrentUserId);
        result.CreatedAt.Should().Be(CurrentTime);
        result.LastUpdatedAt.Should().Be(CurrentTime);

        _numberGeneratorMock.Verify(
            generator =>
                generator.GenerateAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _repositoryMock.Verify(
            repository =>
                repository.AddAsync(
                    It.Is<Document>(document =>
                        document.DocumentNumber == "DOC-000001" &&
                        document.CreatedByUserId == CurrentUserId),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateDocumentAsync_UnauthenticatedUser_ThrowsException()
    {
        _currentUserMock
            .Setup(user => user.IsAuthenticated)
            .Returns(false);

        Func<Task> action = () =>
            _service.CreateDocumentAsync(CreateCommand());

        await action.Should()
            .ThrowAsync<UnauthenticatedUserException>();

        _numberGeneratorMock.Verify(
            generator =>
                generator.GenerateAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _repositoryMock.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<Document>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetDocumentByIdAsync_ExistingDocument_ReturnsDocument()
    {
        Document document = CreateDocument();

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(
                    document.Id,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        Document result =
            await _service.GetDocumentByIdAsync(document.Id);

        result.Should().BeSameAs(document);
    }

    [Fact]
    public async Task GetDocumentByIdAsync_MissingDocument_ThrowsNotFound()
    {
        Guid id = Guid.NewGuid();

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(
                    id,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        Func<Task> action = () =>
            _service.GetDocumentByIdAsync(id);

        DocumentNotFoundException exception =
            (await action.Should()
                .ThrowAsync<DocumentNotFoundException>())
            .Which;

        exception.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public async Task GetDocumentByIdAsync_EmptyId_ThrowsValidationException()
    {
        Func<Task> action = () =>
            _service.GetDocumentByIdAsync(Guid.Empty);

        await action.Should()
            .ThrowAsync<UseCaseValidationException>();

        _repositoryMock.Verify(
            repository =>
                repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateDocumentAsync_ValidCommand_UpdatesDocument()
    {
        Document document = CreateDocument();

        UpdateDocumentCommand command = new()
        {
            Title = "Updated title",
            Description = "Updated description",
            DocumentType = DocumentType.Invoice,
            Department = Department.Legal,
            Owner = "Updated Owner",
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
                    command.Version,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        Document result =
            await _service.UpdateDocumentAsync(
                document.Id,
                command);

        result.Title.Should().Be(command.Title);
        result.Description.Should().Be(command.Description);
        result.Type.Should().Be(command.DocumentType);
        result.Department.Should().Be(command.Department);
        result.Owner.Should().Be(command.Owner);
        result.Version.Should().Be(2);
        result.LastModifiedByUserId.Should().Be(CurrentUserId);
        result.LastUpdatedAt.Should().Be(CurrentTime);

        _repositoryMock.Verify(
            repository =>
                repository.UpdateAsync(
                    document,
                    expectedVersion: 1,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateDocumentAsync_MissingDocument_ThrowsNotFound()
    {
        Guid id = Guid.NewGuid();

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdForUpdateAsync(
                    id,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        Func<Task> action = () =>
            _service.UpdateDocumentAsync(
                id,
                CreateUpdateCommand());

        await action.Should()
            .ThrowAsync<DocumentNotFoundException>();

        _repositoryMock.Verify(
            repository =>
                repository.UpdateAsync(
                    It.IsAny<Document>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateDocumentAsync_InvalidVersion_ThrowsValidationException(
        int invalidVersion)
    {
        UpdateDocumentCommand command =
            CreateUpdateCommand(invalidVersion);

        Func<Task> action = () =>
            _service.UpdateDocumentAsync(
                Guid.NewGuid(),
                command);

        await action.Should()
            .ThrowAsync<UseCaseValidationException>();

        _repositoryMock.Verify(
            repository =>
                repository.GetByIdForUpdateAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateDocumentAsync_RepositoryConcurrencyFailure_PropagatesException()
    {
        Document document = CreateDocument();
        UpdateDocumentCommand command = CreateUpdateCommand();

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
                    command.Version,
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new DocumentConcurrencyException(document.Id));

        Func<Task> action = () =>
            _service.UpdateDocumentAsync(
                document.Id,
                command);

        DocumentConcurrencyException exception =
            (await action.Should()
                .ThrowAsync<DocumentConcurrencyException>())
            .Which;

        exception.Message.Should()
            .Contain(document.Id.ToString());
    }

    [Fact]
    public async Task ChangeDocumentStatusAsync_ValidTransition_UpdatesDocument()
    {
        Document document = CreateDocument();

        ChangeDocumentStatusCommand command = new()
        {
            DocumentId = document.Id,
            NewStatus = DocumentStatus.Uploaded,
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
                    command.Version,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        Document result =
            await _service.ChangeDocumentStatusAsync(command);

        result.Status.Should().Be(DocumentStatus.Uploaded);
        result.Version.Should().Be(2);
        result.LastModifiedByUserId.Should().Be(CurrentUserId);
        result.LastUpdatedAt.Should().Be(CurrentTime);

        _repositoryMock.Verify(
            repository =>
                repository.UpdateAsync(
                    document,
                    expectedVersion: 1,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangeDocumentStatusAsync_InvalidTransition_DoesNotUpdateRepository()
    {
        Document document = CreateDocument();

        ChangeDocumentStatusCommand command = new()
        {
            DocumentId = document.Id,
            NewStatus = DocumentStatus.Approved,
            Version = 1
        };

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdForUpdateAsync(
                    document.Id,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        Func<Task> action = () =>
            _service.ChangeDocumentStatusAsync(command);

        await action.Should()
            .ThrowAsync<
                InvalidDocumentStatusTransitionException>();

        document.Status.Should().Be(DocumentStatus.Draft);
        document.Version.Should().Be(1);

        _repositoryMock.Verify(
            repository =>
                repository.UpdateAsync(
                    It.IsAny<Document>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeDocumentStatusAsync_UnknownStatus_ThrowsValidationException()
    {
        ChangeDocumentStatusCommand command = new()
        {
            DocumentId = Guid.NewGuid(),
            NewStatus = DocumentStatus.Unknown,
            Version = 1
        };

        Func<Task> action = () =>
            _service.ChangeDocumentStatusAsync(command);

        await action.Should()
            .ThrowAsync<UseCaseValidationException>();

        _repositoryMock.Verify(
            repository =>
                repository.GetByIdForUpdateAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteDocumentAsync_DraftDocument_DeletesDocument()
    {
        Document document = CreateDocument();

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdForUpdateAsync(
                    document.Id,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        await _service.DeleteDocumentAsync(document.Id);

        _repositoryMock.Verify(
            repository =>
                repository.DeleteAsync(
                    document,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteDocumentAsync_ApprovedDocument_ThrowsException()
    {
        Document document =
            CreateDocumentAtApprovedStatus();

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdForUpdateAsync(
                    document.Id,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        Func<Task> action = () =>
            _service.DeleteDocumentAsync(document.Id);

        await action.Should()
            .ThrowAsync<
                DocumentDeletionNotAllowedException>();

        _repositoryMock.Verify(
            repository =>
                repository.DeleteAsync(
                    It.IsAny<Document>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchDocumentsAsync_ValidQuery_ReturnsRepositoryResult()
    {
        DocumentQuery query = new()
        {
            PageNumber = 1,
            PageSize = 20,
            SortBy = DocumentSortField.CreatedAt,
            SortDirection = SortDirection.Descending
        };

        PagedResult<Document> expected = new()
        {
            Items = [CreateDocument()],
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 20,
            TotalPages = 1,
            HasNextPage = false,
            HasPreviousPage = false
        };

        _repositoryMock
            .Setup(repository =>
                repository.SearchAsync(
                    query,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        PagedResult<Document> result =
            await _service.SearchDocumentsAsync(query);

        result.Should().BeSameAs(expected);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task SearchDocumentsAsync_InvalidPagination_ThrowsValidationException(
        int pageNumber,
        int pageSize)
    {
        DocumentQuery query = new()
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        Func<Task> action = () =>
            _service.SearchDocumentsAsync(query);

        await action.Should()
            .ThrowAsync<UseCaseValidationException>();

        _repositoryMock.Verify(
            repository =>
                repository.SearchAsync(
                    It.IsAny<DocumentQuery>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchDocumentsAsync_InvalidDateRange_ThrowsValidationException()
    {
        DocumentQuery query = new()
        {
            CreatedFrom = CurrentTime.AddDays(1),
            CreatedTo = CurrentTime,
            PageNumber = 1,
            PageSize = 20
        };

        Func<Task> action = () =>
            _service.SearchDocumentsAsync(query);

        await action.Should()
            .ThrowAsync<UseCaseValidationException>();

        _repositoryMock.Verify(
            repository =>
                repository.SearchAsync(
                    It.IsAny<DocumentQuery>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static CreateDocumentCommand CreateCommand()
    {
        return new CreateDocumentCommand
        {
            Title = "Supplier Agreement",
            Description = "Annual supplier agreement",
            DocumentType = DocumentType.Contract,
            Department = Department.Purchasing,
            Owner = "Test Owner"
        };
    }

    private static UpdateDocumentCommand CreateUpdateCommand(
        int version = 1)
    {
        return new UpdateDocumentCommand
        {
            Title = "Updated title",
            Description = "Updated description",
            DocumentType = DocumentType.Contract,
            Department = Department.Legal,
            Owner = "Updated Owner",
            Version = version
        };
    }

    private static Document CreateDocument()
    {
        return Document.Create(
            documentNumber: "DOC-000001",
            title: "Supplier Agreement",
            description: "Annual supplier agreement",
            type: DocumentType.Contract,
            department: Department.Purchasing,
            owner: "Test Owner",
            createdByUserId: CurrentUserId,
            createdAt: CurrentTime.AddHours(-1));
    }

    private static Document CreateDocumentAtApprovedStatus()
    {
        Document document = CreateDocument();

        document.ChangeStatus(
            DocumentStatus.Uploaded,
            CurrentUserId,
            CurrentTime.AddMinutes(-40));

        document.ChangeStatus(
            DocumentStatus.UnderReview,
            CurrentUserId,
            CurrentTime.AddMinutes(-30));

        document.ChangeStatus(
            DocumentStatus.PendingApproval,
            CurrentUserId,
            CurrentTime.AddMinutes(-20));

        document.ChangeStatus(
            DocumentStatus.Approved,
            CurrentUserId,
            CurrentTime.AddMinutes(-10));

        return document;
    }
}
using DocuTrack.Application.Common.Exceptions;
using DocuTrack.Application.Documents.Queries;
using DocuTrack.Domain.Documents;
using DocuTrack.Infrastructure.IntegrationTests.Collections;
using DocuTrack.Infrastructure.Persistence;
using DocuTrack.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DocuTrack.Infrastructure.IntegrationTests.Repositories
{
    [Collection(DatabaseCollection.Name)]
    public sealed class EfDocumentRepositoryTests
     : IAsyncLifetime
    {
        private static readonly Guid UserId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        private static readonly DateTimeOffset Now =
            new(
                2026,
                8,
                5,
                12,
                0,
                0,
                TimeSpan.Zero);

        private readonly DatabaseFixture _fixture;

        public EfDocumentRepositoryTests(
        DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            await _fixture.ResetDatabaseAsync();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public async Task AddAsync_ValidDocument_PersistsDocument()
        {
            await using DocuTrackDbContext context =
                _fixture.CreateDbContext();

            EfDocumentRepository repository =
                new(context);

            Document document =
                CreateDocument("DOC-000001");

            Document result =
                await repository.AddAsync(document);

            result.Should().BeSameAs(document);

            await using DocuTrackDbContext verificationContext =
                _fixture.CreateDbContext();

            Document? saved =
                await verificationContext.Documents
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        item => item.Id == document.Id);

            saved.Should().NotBeNull();
            saved!.DocumentNumber.Should().Be("DOC-000001");
            saved.Title.Should().Be(document.Title);
            saved.Version.Should().Be(1);
        }

        [Fact]
        public async Task AddAsync_DuplicateDocumentNumber_ThrowsDatabaseConflict()
        {
            await using DocuTrackDbContext context =
                _fixture.CreateDbContext();

            EfDocumentRepository repository =
                new(context);

            await repository.AddAsync(
                CreateDocument("DOC-000001"));

            Document duplicate =
                CreateDocument("DOC-000001");

            Func<Task> action = () =>
                repository.AddAsync(duplicate);

            await action.Should()
                .ThrowAsync<DatabaseConflictException>();
        }

        [Fact]
        public async Task GetByIdAsync_ExistingDocument_ReturnsUntrackedDocument()
        {
            Guid documentId;

            await using (DocuTrackDbContext seedContext =
                         _fixture.CreateDbContext())
            {
                EfDocumentRepository seedRepository =
                    new(seedContext);

                Document document =
                    CreateDocument("DOC-000001");

                await seedRepository.AddAsync(document);

                documentId = document.Id;
            }

            await using DocuTrackDbContext context =
                _fixture.CreateDbContext();

            EfDocumentRepository repository =
                new(context);

            Document? result =
                await repository.GetByIdAsync(documentId);

            result.Should().NotBeNull();

            context.Entry(result!).State.Should()
                .Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingDocument_ReturnsTrackedDocument()
        {
            Guid documentId =
                await SeedDocumentAsync("DOC-000001");

            await using DocuTrackDbContext context =
                _fixture.CreateDbContext();

            EfDocumentRepository repository =
                new(context);

            Document? result =
                await repository.GetByIdForUpdateAsync(
                    documentId);

            result.Should().NotBeNull();

            context.Entry(result!).State.Should()
                .Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task UpdateAsync_CorrectVersion_UpdatesDocument()
        {
            Guid documentId =
                await SeedDocumentAsync("DOC-000001");

            await using DocuTrackDbContext context =
                _fixture.CreateDbContext();

            EfDocumentRepository repository =
                new(context);

            Document document =
                (await repository.GetByIdForUpdateAsync(documentId))!;

            document.UpdateDetails(
                title: "Updated title",
                description: "Updated description",
                type: DocumentType.Invoice,
                department: Department.Legal,
                owner: "Updated Owner",
                modifiedByUserId: UserId,
                modifiedAt: Now.AddHours(1));

            Document result =
                await repository.UpdateAsync(
                    document,
                    expectedVersion: 1);

            result.Version.Should().Be(2);

            await using DocuTrackDbContext verificationContext =
                _fixture.CreateDbContext();

            Document saved =
                await verificationContext.Documents
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == documentId);

            saved.Title.Should().Be("Updated title");
            saved.Version.Should().Be(2);
        }

        [Fact]
        public async Task UpdateAsync_StaleVersion_ThrowsConcurrencyException()
        {
            Guid documentId =
                await SeedDocumentAsync("DOC-000001");

            await using DocuTrackDbContext firstContext =
                _fixture.CreateDbContext();

            await using DocuTrackDbContext secondContext =
                _fixture.CreateDbContext();

            EfDocumentRepository firstRepository =
                new(firstContext);

            EfDocumentRepository secondRepository =
                new(secondContext);

            Document firstCopy =
                (await firstRepository.GetByIdForUpdateAsync(
                    documentId))!;

            Document secondCopy =
                (await secondRepository.GetByIdForUpdateAsync(
                    documentId))!;

            firstCopy.UpdateDetails(
                title: "First update",
                description: null,
                type: DocumentType.Contract,
                department: Department.Legal,
                owner: "First Owner",
                modifiedByUserId: UserId,
                modifiedAt: Now.AddHours(1));

            await firstRepository.UpdateAsync(
                firstCopy,
                expectedVersion: 1);

            secondCopy.UpdateDetails(
                title: "Stale update",
                description: null,
                type: DocumentType.Contract,
                department: Department.Purchasing,
                owner: "Second Owner",
                modifiedByUserId: UserId,
                modifiedAt: Now.AddHours(2));

            Func<Task> action = () =>
                secondRepository.UpdateAsync(
                    secondCopy,
                    expectedVersion: 1);

            await action.Should()
                .ThrowAsync<DocumentConcurrencyException>();
        }

        [Fact]
        public async Task DeleteAsync_ExistingDocument_RemovesDocument()
        {
            Guid documentId =
                await SeedDocumentAsync("DOC-000001");

            await using DocuTrackDbContext context =
                _fixture.CreateDbContext();

            EfDocumentRepository repository =
                new(context);

            Document document =
                (await repository.GetByIdForUpdateAsync(
                    documentId))!;

            await repository.DeleteAsync(document);

            await using DocuTrackDbContext verificationContext =
                _fixture.CreateDbContext();

            bool exists =
                await verificationContext.Documents
                    .AnyAsync(item => item.Id == documentId);

            exists.Should().BeFalse();
        }

        [Fact]
        public async Task SearchAsync_FiltersByStatusAndDepartment()
        {
            await SeedDocumentsAsync();

            await using DocuTrackDbContext context =
                _fixture.CreateDbContext();

            EfDocumentRepository repository =
                new(context);

            DocumentQuery query = new()
            {
                Status = DocumentStatus.Draft,
                Department = Department.Purchasing,
                PageNumber = 1,
                PageSize = 20,
                SortBy = DocumentSortField.Title,
                SortDirection = SortDirection.Ascending
            };

            var result =
                await repository.SearchAsync(query);

            result.TotalCount.Should().Be(2);
            result.Items.Should().HaveCount(2);

            result.Items.Should().OnlyContain(
                document =>
                    document.Status == DocumentStatus.Draft &&
                    document.Department ==
                    Department.Purchasing);

            result.Items.Select(document => document.Title)
                .Should()
                .BeInAscendingOrder();
        }

        [Fact]
        public async Task SearchAsync_PaginatesResults()
        {
            await SeedDocumentsAsync();

            await using DocuTrackDbContext context =
                _fixture.CreateDbContext();

            EfDocumentRepository repository =
                new(context);

            DocumentQuery query = new()
            {
                PageNumber = 2,
                PageSize = 2,
                SortBy = DocumentSortField.DocumentNumber,
                SortDirection = SortDirection.Ascending
            };

            var result =
                await repository.SearchAsync(query);

            result.TotalCount.Should().Be(4);
            result.TotalPages.Should().Be(2);
            result.PageNumber.Should().Be(2);
            result.Items.Should().HaveCount(2);
            result.HasPreviousPage.Should().BeTrue();
            result.HasNextPage.Should().BeFalse();
        }

        private async Task<Guid> SeedDocumentAsync(
            string documentNumber)
        {
            await using DocuTrackDbContext context =
                _fixture.CreateDbContext();

            EfDocumentRepository repository =
                new(context);

            Document document =
                CreateDocument(documentNumber);

            await repository.AddAsync(document);

            return document.Id;
        }

        private async Task SeedDocumentsAsync()
        {
            await using DocuTrackDbContext context =
                _fixture.CreateDbContext();

            EfDocumentRepository repository =
                new(context);

            Document[] documents =
            [
                CreateDocument(
                "DOC-000001",
                "Alpha",
                Department.Purchasing),

            CreateDocument(
                "DOC-000002",
                "Bravo",
                Department.Purchasing),

            CreateDocument(
                "DOC-000003",
                "Charlie",
                Department.Legal),

            CreateDocument(
                "DOC-000004",
                "Delta",
                Department.InformationTechnology)
            ];

            foreach (Document document in documents)
            {
                await repository.AddAsync(document);
            }
        }

        private static Document CreateDocument(
            string documentNumber,
            string title = "Supplier Agreement",
            Department department = Department.Purchasing)
        {
            return Document.Create(
                documentNumber: documentNumber,
                title: title,
                description: "Integration test document",
                type: DocumentType.Contract,
                department: department,
                owner: "Test Owner",
                createdByUserId: UserId,
                createdAt: Now);
        }
    }
}

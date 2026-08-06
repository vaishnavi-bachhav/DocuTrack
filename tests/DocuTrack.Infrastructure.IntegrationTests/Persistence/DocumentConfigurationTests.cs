using DocuTrack.Domain.Documents;
using DocuTrack.Infrastructure.IntegrationTests.Collections;
using DocuTrack.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DocuTrack.Infrastructure.IntegrationTests.Persistence
{
    [Collection(DatabaseCollection.Name)]
    public sealed class DocumentConfigurationTests
    {
        private readonly DatabaseFixture _fixture;

        public DocumentConfigurationTests(
            DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void DocumentNumber_IsConfiguredAsUniqueIndex()
        {
            using DocuTrackDbContext context =
                _fixture.CreateDbContext();

            IEntityType entityType =
                context.Model.FindEntityType(typeof(Document))
                ?? throw new InvalidOperationException(
                    "Document entity was not found.");

            IIndex? index =
                entityType.GetIndexes()
                    .SingleOrDefault(candidate =>
                        candidate.Properties.Count == 1 &&
                        candidate.Properties[0].Name ==
                        nameof(Document.DocumentNumber));

            index.Should().NotBeNull();
            index!.IsUnique.Should().BeTrue();
        }

        [Fact]
        public void Version_IsConfiguredAsConcurrencyToken()
        {
            using DocuTrackDbContext context =
                _fixture.CreateDbContext();

            IEntityType entityType =
                context.Model.FindEntityType(typeof(Document))
                ?? throw new InvalidOperationException(
                    "Document entity was not found.");

            IProperty versionProperty =
                entityType.FindProperty(nameof(Document.Version))
                ?? throw new InvalidOperationException(
                    "Version property was not found.");

            versionProperty.IsConcurrencyToken.Should().BeTrue();
        }

        [Fact]
        public void Document_IsMappedToDocumentsTable()
        {
            using DocuTrackDbContext context =
                _fixture.CreateDbContext();

            IEntityType entityType =
                context.Model.FindEntityType(typeof(Document))
                ?? throw new InvalidOperationException(
                    "Document entity was not found.");

            entityType.GetTableName().Should().Be("Documents");
        }

    }
}

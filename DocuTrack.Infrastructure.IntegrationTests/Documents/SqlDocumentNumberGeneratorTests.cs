using DocuTrack.Infrastructure.Documents;
using DocuTrack.Infrastructure.IntegrationTests.Collections;
using DocuTrack.Infrastructure.Persistence;
using FluentAssertions;

namespace DocuTrack.Infrastructure.IntegrationTests.Documents
{
    [Collection(DatabaseCollection.Name)]
    public sealed class SqlDocumentNumberGeneratorTests
    : IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;

        public SqlDocumentNumberGeneratorTests(
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
        public async Task GenerateAsync_FirstCall_ReturnsFirstNumber()
        {
            await using DocuTrackDbContext context =
                _fixture.CreateDbContext();

            SqlDocumentNumberGenerator generator =
                new(context);

            string result =
                await generator.GenerateAsync();

            result.Should().Be("DOC-000001");
        }

        [Fact]
        public async Task GenerateAsync_MultipleCalls_ReturnsIncreasingNumbers()
        {
            await using DocuTrackDbContext context =
                _fixture.CreateDbContext();

            SqlDocumentNumberGenerator generator =
                new(context);

            string first =
                await generator.GenerateAsync();

            string second =
                await generator.GenerateAsync();

            string third =
                await generator.GenerateAsync();

            first.Should().Be("DOC-000001");
            second.Should().Be("DOC-000002");
            third.Should().Be("DOC-000003");
        }

        [Fact]
        public async Task GenerateAsync_AcrossContexts_ContinuesSequence()
        {
            string first;

            await using (DocuTrackDbContext firstContext =
                         _fixture.CreateDbContext())
            {
                SqlDocumentNumberGenerator firstGenerator =
                    new(firstContext);

                first = await firstGenerator.GenerateAsync();
            }

            string second;

            await using (DocuTrackDbContext secondContext =
                         _fixture.CreateDbContext())
            {
                SqlDocumentNumberGenerator secondGenerator =
                    new(secondContext);

                second = await secondGenerator.GenerateAsync();
            }

            first.Should().Be("DOC-000001");
            second.Should().Be("DOC-000002");
        }
    }
}

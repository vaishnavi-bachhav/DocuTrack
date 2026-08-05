namespace DocuTrack.Api.IntegrationTests;

[CollectionDefinition(
    Name,
    DisableParallelization = true)]
public sealed class IntegrationTestCollection
    : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name =
        "DocuTrack API integration tests";
}
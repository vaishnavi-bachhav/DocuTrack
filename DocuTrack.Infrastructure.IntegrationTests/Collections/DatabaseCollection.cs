namespace DocuTrack.Infrastructure.IntegrationTests.Collections
{
    [CollectionDefinition(Name)]
    public sealed class DatabaseCollection
    : ICollectionFixture<DatabaseFixture>
    {
        public const string Name =
            "DocuTrack SQL Server collection";
    }
}

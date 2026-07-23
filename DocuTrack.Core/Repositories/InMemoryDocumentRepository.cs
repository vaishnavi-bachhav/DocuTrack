using DocuTrack.Core.Models;

namespace DocuTrack.Core.Repositories
{
    public sealed class InMemoryDocumentRepository : IDocumentRepository // sealed class to prevent inheritance
    {
        private readonly List<Document> _documents = [];

        public Document Add(Document document)
        {
            ArgumentNullException.ThrowIfNull(document);
            _documents.Add(document);
            return document;
        }

        public IReadOnlyCollection<Document> GetAll()
        {
            return _documents.ToList();
        }
    }
}

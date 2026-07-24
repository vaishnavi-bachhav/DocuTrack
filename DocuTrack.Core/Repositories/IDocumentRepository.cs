using DocuTrack.Core.Models;

namespace DocuTrack.Core.Repositories
{
    public interface IDocumentRepository
    {
        public Document Add(Document document);
        public IReadOnlyCollection<Document> GetAll();
        public Document? GetById(Guid id);
    }
}

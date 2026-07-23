using DocuTrack.Core.Models;
using DocuTrack.Core.Repositories;
using DocuTrack.Core.Requests;

namespace DocuTrack.Core.Services
{
    public sealed class DocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private int _nextDocumentNumber = 1;

        public DocumentService(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        }

        public Document CreateDocument(CreateDocumentRequest request)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            DateTimeOffset now = DateTimeOffset.Now;

            var document = new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = $"DOC-{_nextDocumentNumber:D4}",
                Title = request.Title,
                Description = request.Description,
                Type = request.DocumentType,
                Department = request.Department,
                Owner = request.Owner,
                Status = Enums.DocumentStatus.Draft,
                CreatedAt = now,
                LastUpdatedAt = now,
                Version = 1
            };
            _documentRepository.Add(document);
            _nextDocumentNumber++;

            return document;
        }

        public IReadOnlyCollection<Document> GetAllDocuments()
        {
            return _documentRepository.GetAll();
        }
    }
}

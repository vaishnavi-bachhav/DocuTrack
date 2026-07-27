using DocuTrack.Api.Contracts.Responses;
using DocuTrack.Core.Enums;
using DocuTrack.Core.Services;
using Microsoft.AspNetCore.Mvc;
using DocuTrack.Core.Models;
using DocuTrack.Api.Contracts.Requests;
using DocuTrack.Core.Requests;
using DocuTrack.Core.Exceptions;

namespace DocuTrack.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class DocumentsController : ControllerBase
    {
        private readonly DocumentService _documentService;
        public DocumentsController(DocumentService documentService)
        {
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        }

        [HttpPost]
        public async Task<ActionResult<DocumentResponse>> CreateDocument([FromBody] CreateDocumentApiRequest request, CancellationToken cancellationToken)
        {
            if (request.DocumentType == DocumentType.Unknown)
            {
                ModelState.AddModelError(
                    nameof(request.DocumentType),
                    "Document type is required.");
            }

            if (request.Department == Department.Unknown)
            {
                ModelState.AddModelError(
                    nameof(request.Department),
                    "Department is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                ModelState.AddModelError(
                    nameof(request.Title),
                    "Title cannot contain only whitespace.");
            }

            if (string.IsNullOrWhiteSpace(request.Owner))
            {
                ModelState.AddModelError(
                    nameof(request.Owner),
                    "Owner cannot contain only whitespace.");
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }
            var createDocumentRequest = new CreateDocumentRequest
            {
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) 
                            ? null 
                            : request.Description.Trim(),
                DocumentType = request.DocumentType,
                Department = request.Department,
                Owner = request.Owner.Trim(),
            };
            Document document = await _documentService.CreateDocumentAsync(createDocumentRequest, cancellationToken);
            DocumentResponse response = MapToResponse(document);

            return CreatedAtAction(
                nameof(GetDocumentById),
                new { id = document.Id },
                response);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyCollection<DocumentResponse>>> GetAllDocuments(CancellationToken cancellationToken)
        {
            IReadOnlyCollection<DocumentResponse> documents = (await _documentService.GetAllDocumentsAsync(cancellationToken)).Select(MapToResponse).ToList();
            return Ok(documents);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<DocumentResponse>> GetDocumentById(Guid id, CancellationToken cancellationToken)
        {
            Document? document = await _documentService.GetDocumentByIdAsync(id, cancellationToken);

            if (document is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Document not found",
                    Detail = $"No document was found with ID '{id}'.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(MapToResponse(document));
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<DocumentResponse>> UpdateDocument(Guid id, [FromBody] UpdateDocumentApiRequest request, CancellationToken cancellationToken)
        {
            if (request.DocumentType == DocumentType.Unknown)
            {
                ModelState.AddModelError(
                    nameof(request.DocumentType),
                    "Document type is required.");
            }

            if (request.Department == Department.Unknown)
            {
                ModelState.AddModelError(
                    nameof(request.Department),
                    "Department is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                ModelState.AddModelError(
                    nameof(request.Title),
                    "Title cannot contain only whitespace.");
            }
            if (string.IsNullOrWhiteSpace(request.Owner))
            {
                ModelState.AddModelError(nameof(request.Owner), "Owner cannot contain only whitespace.");
            }
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }
            var updateDocumentRequest = new UpdateDocumentRequest
            {
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description)
                            ? null
                            : request.Description.Trim(),
                DocumentType = request.DocumentType,
                Department = request.Department,
                Owner = request.Owner.Trim(),
            };

            Document? document = await _documentService.UpdateDocumentAsync(id, updateDocumentRequest, cancellationToken);
            if(document is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Document not found",
                    Detail = $"No document was found with ID '{id}'.",
                    Status = StatusCodes.Status404NotFound
                });
            }
            DocumentResponse? response = MapToResponse(document);
            return Ok(response);
        }

        [HttpPatch("{id:guid}/status")]
        public async Task<ActionResult<DocumentResponse>> ChangeDocumentStatus(Guid id, [FromBody] ChangeDocumentStatusApiRequest request, CancellationToken cancellationToken)
        {
            if(request.NewStatus == DocumentStatus.Unknown)
            {
                ModelState.AddModelError(
                    nameof(request.NewStatus),
                    "New status is required.");

                return ValidationProblem(ModelState);
            }
            try
            {
                Document? updateDocument = await _documentService.ChangeDocumentStatusAsync(new ChangeDocumentStatusRequest
                {
                    DocumentId = id,
                    NewStatus = request.NewStatus
                }, cancellationToken);

                if (updateDocument is null)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Document not found",
                        Detail = $"No document was found with ID '{id}'.",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                return Ok(MapToResponse(updateDocument));
            }
            catch (InvalidDocumentStatusTransitionException exception)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Invalid document status transition",
                    Detail = exception.Message,
                    Status = StatusCodes.Status409Conflict  
                });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteDocument(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                bool isDeleted = await _documentService.DeleteDocumentAsync(id, cancellationToken);

                if(!isDeleted)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Document not found",
                        Detail = $"No document was found with ID '{id}'.",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                return NoContent();
            }
            catch (DocumentDeletionNotAllowedException exception)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Document cannot be deleted",
                    Detail = exception.Message,
                    Status = StatusCodes.Status409Conflict
                });
            }
        }

        private static DocumentResponse MapToResponse(Document document)
        {
            ArgumentNullException.ThrowIfNull(document, nameof(document));

            return new DocumentResponse
            {
                Id = document.Id,
                DocumentNumber = document.DocumentNumber,
                Title = document.Title,
                Description = document.Description,
                Type = document.Type,
                Department = document.Department,
                Owner = document.Owner,
                Status = document.Status,
                CreatedDate = document.CreatedAt,
                LastUpdatedDate = document.LastUpdatedAt,
                Version = document.Version
            };
        }
    }
}

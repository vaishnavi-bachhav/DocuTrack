using DocuTrack.Api.Contracts.Requests;
using DocuTrack.Api.Contracts.Responses;
using DocuTrack.Core.Enums;
using DocuTrack.Core.Models;
using DocuTrack.Core.Requests;
using DocuTrack.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuTrack.Api.Controllers
{
    [Authorize]
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
        public async Task<ActionResult<PagedResult<DocumentResponse>>> GetDocuments([FromQuery] DocumentQueryApiRequest request, CancellationToken cancellationToken)
        {
            if (request.PageNumber < 1)
            {
                ModelState.AddModelError(
                    nameof(request.PageNumber),
                    "Page number must be greater than or equal to 1.");
            }
            if (request.PageSize < 1 || request.PageSize > 100)
            {
                ModelState.AddModelError(
                    nameof(request.PageSize),
                    "Page size must be between 1 and 100.");
            }
            if (request.Status == DocumentStatus.Unknown)
            {
                ModelState.AddModelError(
                    nameof(request.Status),
                    "A valid document status is required.");
            }

            if (request.Department == Department.Unknown)
            {
                ModelState.AddModelError(
                    nameof(request.Department),
                    "A valid department is required.");
            }

            if (request.CreatedFrom.HasValue &&
                request.CreatedTo.HasValue &&
                request.CreatedFrom > request.CreatedTo)
            {
                ModelState.AddModelError(
                    nameof(request.CreatedFrom),
                    "CreatedFrom cannot be later than CreatedTo.");
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var queryRequest = new DocumentQuery
            {
                Search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
                Status = request.Status,
                Department = request.Department,
                Owner = string.IsNullOrWhiteSpace(request.Owner) ? null : request.Owner.Trim(),
                CreatedFrom = request.CreatedFrom,
                CreatedTo = request.CreatedTo,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                SortDirection = request.SortDirection,
            };
            PagedResult<Document> result = await _documentService.SearchDocumentsAsync(queryRequest, cancellationToken);
            PagedResult<DocumentResponse> response = new()
            {
                Items = result.Items.Select(MapToResponse).ToList(),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
                HasNextPage = result.HasNextPage,
                HasPreviousPage = result.HasPreviousPage
            };
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<DocumentResponse>> GetDocumentById(Guid id, CancellationToken cancellationToken)
        {
            Document document = await _documentService.GetDocumentByIdAsync(id, cancellationToken);

            return Ok(MapToResponse(document));
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<DocumentResponse>> UpdateDocument(Guid id, [FromBody] UpdateDocumentApiRequest request, CancellationToken cancellationToken)
        {
            if (request.DocumentType == DocumentType.Unknown)
            {
                ModelState.AddModelError(nameof(request.DocumentType), "Document type is required.");
            }

            if (request.Department == Department.Unknown)
            {
                ModelState.AddModelError(nameof(request.Department), "Department is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                ModelState.AddModelError(nameof(request.Title), "Title cannot contain only whitespace.");
            }
            if (string.IsNullOrWhiteSpace(request.Owner))
            {
                ModelState.AddModelError(nameof(request.Owner), "Owner cannot contain only whitespace.");
            }
            if (request.Version < 1)
            {
                ModelState.AddModelError(nameof(request.Version), "Version must be greater than or equal to 1.");
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
                Version = request.Version
            };

            Document document = await _documentService.UpdateDocumentAsync(id, updateDocumentRequest, cancellationToken);
            return Ok(MapToResponse(document));
        }

        [Authorize(Roles = "Reviewer,Admin")]
        [HttpPatch("{id:guid}/status")]
        public async Task<ActionResult<DocumentResponse>> ChangeDocumentStatus(Guid id, [FromBody] ChangeDocumentStatusApiRequest request, CancellationToken cancellationToken)
        {
            if (request.NewStatus == DocumentStatus.Unknown)
            {
                ModelState.AddModelError(
                    nameof(request.NewStatus),
                    "New status is required.");

                return ValidationProblem(ModelState);
            }

            Document updateDocument = await _documentService.ChangeDocumentStatusAsync(new ChangeDocumentStatusRequest
            {
                DocumentId = id,
                NewStatus = request.NewStatus,
                Version = request.Version
            }, cancellationToken);

            return Ok(MapToResponse(updateDocument));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteDocument(Guid id, CancellationToken cancellationToken)
        {
            await _documentService.DeleteDocumentAsync(id, cancellationToken);

            return NoContent();
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
                Version = document.Version,
                CreatedByUserId = document.CreatedByUserId,
                LastModifiedByUserId = document.LastModifiedByUserId
            };
        }
    }
}

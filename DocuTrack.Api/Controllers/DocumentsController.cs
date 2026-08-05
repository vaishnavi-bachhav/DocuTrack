using DocuTrack.Api.Contracts.Requests;
using DocuTrack.Api.Contracts.Responses;
using DocuTrack.Api.Mappings;
using DocuTrack.Application.Authorization;
using DocuTrack.Application.Common;
using DocuTrack.Application.Documents;
using DocuTrack.Domain.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuTrack.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public sealed class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        public DocumentsController(IDocumentService documentService)
        {
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        }

        /// <summary>
        /// Creates a new document.
        /// </summary>
        /// <remarks>
        /// Newly created documents are always created in Draft status.
        /// </remarks>
        /// <response code="201">Document created successfully.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Authentication required.</response>
        [HttpPost]
        public async Task<ActionResult<DocumentResponse>> Create([FromBody] CreateDocumentApiRequest request, CancellationToken cancellationToken)
        {
            Document document = await _documentService.CreateDocumentAsync(request.ToCommand(), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = document.Id }, document.ToResponse());
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<DocumentResponse>>> Search([FromQuery] DocumentQueryApiRequest request, CancellationToken cancellationToken)
        {
            PagedResult<Document> result = await _documentService.SearchDocumentsAsync(request.ToQuery(), cancellationToken);
            return Ok(result.ToResponse());
        }

        /// <summary>
        /// Retrieves a document by its identifier.
        /// </summary>
        /// <param name="id">Document identifier.</param>
        /// <response code="200">Document found.</response>
        /// <response code="404">Document was not found.</response>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<DocumentResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            Document document = await _documentService.GetDocumentByIdAsync(id, cancellationToken);
            return Ok(document.ToResponse());
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<DocumentResponse>> Update(Guid id, [FromBody] UpdateDocumentApiRequest request, CancellationToken cancellationToken)
        {
            Document document = await _documentService.UpdateDocumentAsync(id, request.ToCommand(), cancellationToken);
            return Ok(document.ToResponse());
        }

        [Authorize(Policy = AuthorizationPolicies.ReviewDocuments)]
        [HttpPatch("{id:guid}/status")]
        public async Task<ActionResult<DocumentResponse>> ChangeStatus(Guid id, [FromBody] ChangeDocumentStatusApiRequest request, CancellationToken cancellationToken)
        {
            Document document = await _documentService.ChangeDocumentStatusAsync(request.ToCommand(id), cancellationToken);
            return Ok(document.ToResponse());
        }

        [Authorize(
        Policy = AuthorizationPolicies.DeleteDocuments)]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteDocument(Guid id, CancellationToken cancellationToken)
        {
            await _documentService.DeleteDocumentAsync(id, cancellationToken);

            return NoContent();
        }
    }
}

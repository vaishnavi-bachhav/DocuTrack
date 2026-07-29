using DocuTrack.Api.Contracts.Responses;
using DocuTrack.Core.Enums;
using DocuTrack.Core.Models;
using DocuTrack.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocuTrack.Api.IntegrationTests
{
    public sealed class DocumentsApiTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;
        public DocumentsApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }
        private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
        private static JsonSerializerOptions CreateJsonOptions()
        {
            JsonSerializerOptions options = new(JsonSerializerDefaults.Web);

            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }
        public async Task InitializeAsync()
        {
            using IServiceScope scope = _factory.Services.CreateScope();

            DocuTrackDbContext context = scope.ServiceProvider.GetRequiredService<DocuTrackDbContext>();

            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public async Task GetDocumentById_ExistingDocument_ReturnsOk()
        {
            // Arrange
            Document document = await SeedDocumentAsync(DocumentStatus.Draft);

            // Act
            HttpResponseMessage response = await _client.GetAsync($"/api/documents/{document.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            DocumentResponse? body = await response.Content.ReadFromJsonAsync<DocumentResponse>(JsonOptions);

            body.Should().NotBeNull();
            body.Id.Should().Be(document.Id);
            body.Title.Should().Be(document.Title);
            body.Type.Should().Be(DocumentType.Contract);
            body.Status.Should().Be(DocumentStatus.Draft);
        }

        // Invalid status transition
        [Fact]
        public async Task ChangeStatus_InvalidTransition_ReturnsConflict()
        {
            Document document = await SeedDocumentAsync(DocumentStatus.Draft);

            HttpResponseMessage getResponse = await _client.GetAsync($"/api/documents/{document.Id}");

            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var request = new
            {
                newStatus = "Approved"
            };

            HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/documents/{document.Id}/status", request);

            string responseBody = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.Conflict, $"response body was: {responseBody}");
        }

        // Delete Approved document
        [Fact]
        public async Task DeleteDocument_ApprovedDocument_ReturnsConflict()
        {
            Document document = await SeedDocumentAsync(DocumentStatus.Approved);

            HttpResponseMessage response = await _client.DeleteAsync($"/api/documents/{document.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        // Delete Draft document
        [Fact]
        public async Task DeleteDocument_DraftDocument_ReturnsNoContent()
        {
            Document document = await SeedDocumentAsync(DocumentStatus.Draft);

            HttpResponseMessage response = await _client.DeleteAsync($"/api/documents/{document.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using IServiceScope scope = _factory.Services.CreateScope();

            DocuTrackDbContext context = scope.ServiceProvider.GetRequiredService<DocuTrackDbContext>();

            Document? deletedDocument = await context.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == document.Id);

            deletedDocument.Should().BeNull();
        }

        [Fact]
        public async Task ChangeStatus_ValidTransition_ReturnsOk()
        {
            Document document = await SeedDocumentAsync(DocumentStatus.Draft);

            var request = new
            {
                newStatus = "Uploaded"
            };

            HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/documents/{document.Id}/status",request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            DocumentResponse? body = await response.Content.ReadFromJsonAsync<DocumentResponse>(JsonOptions);

            body.Should().NotBeNull();
            body!.Status.Should().Be(DocumentStatus.Uploaded);
            body.Version.Should().Be(2);
        }

        [Fact]
        public async Task GetDocumentById_MissingDocument_ReturnsProblemDetails()
        {
            Guid missingId = Guid.NewGuid();

            HttpResponseMessage response = await _client.GetAsync($"/api/documents/{missingId}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            ProblemDetails? body = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

            body.Should().NotBeNull();
            body!.Status.Should().Be(404);
            body.Title.Should().Be("Document not found");
            body.Detail.Should().Contain(missingId.ToString());
            body.Instance.Should().Be($"/api/documents/{missingId}");
            body.Extensions.Should().ContainKey("traceId");
        }

        [Fact]
        public async Task GetDocuments_WithStatusFilter_ReturnsOnlyMatchingDocuments()
        {
            await SeedDocumentAsync(DocumentStatus.Draft);
            await SeedDocumentAsync(DocumentStatus.Approved);

            HttpResponseMessage response =
                await _client.GetAsync(
                    "/api/documents?status=Approved&pageNumber=1&pageSize=20");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            PagedResult<DocumentResponse>? body =
                await response.Content
                    .ReadFromJsonAsync<PagedResult<DocumentResponse>>(
                        JsonOptions);

            body.Should().NotBeNull();
            body!.TotalCount.Should().Be(1);
            body.Items.Should().ContainSingle();
            body.Items[0].Status.Should().Be(DocumentStatus.Approved);
        }

        [Fact]
        public async Task GetDocuments_PageNumberBelowOne_ReturnsBadRequest()
        {
            HttpResponseMessage response =
                await _client.GetAsync(
                    "/api/documents?pageNumber=0");

            response.StatusCode.Should()
                .Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateDocument_ExistingDocument_ReturnsUpdatedDocument()
        {
            Document document =
                await SeedDocumentAsync(DocumentStatus.Draft);

            var request = new
            {
                title = "Updated Integration Document",
                description = "Updated description",
                documentType = "Invoice",
                department = "Legal",
                owner = "Updated Owner"
            };

            HttpResponseMessage response =
                await _client.PutAsJsonAsync(
                    $"/api/documents/{document.Id}",
                    request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            DocumentResponse? body =
                await response.Content
                    .ReadFromJsonAsync<DocumentResponse>(
                        JsonOptions);

            body.Should().NotBeNull();
            body!.Title.Should().Be("Updated Integration Document");
            body.Owner.Should().Be("Updated Owner");
            body.Type.Should().Be(DocumentType.Invoice);
            body.Department.Should().Be(Department.Legal);
            body.Version.Should().Be(2);
        }

        [Fact]
        public async Task UpdateDocument_MissingDocument_ReturnsNotFound()
        {
            Guid missingId = Guid.NewGuid();

            var request = new
            {
                title = "Updated Document",
                description = "Description",
                documentType = "Contract",
                department = "Purchasing",
                owner = "Test Owner"
            };

            HttpResponseMessage response =
                await _client.PutAsJsonAsync(
                    $"/api/documents/{missingId}",
                    request);

            response.StatusCode.Should()
                .Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateDocument_WhitespaceOwner_ReturnsBadRequest()
        {
            Document document =
                await SeedDocumentAsync(DocumentStatus.Draft);

            var request = new
            {
                title = "Valid Title",
                description = "Description",
                documentType = "Contract",
                department = "Purchasing",
                owner = "   "
            };

            HttpResponseMessage response =
                await _client.PutAsJsonAsync(
                    $"/api/documents/{document.Id}",
                    request);

            response.StatusCode.Should()
                .Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ChangeStatus_MissingDocument_ReturnsNotFound()
        {
            var request = new
            {
                newStatus = "Uploaded"
            };

            HttpResponseMessage response =
                await _client.PatchAsJsonAsync(
                    $"/api/documents/{Guid.NewGuid()}/status",
                    request);

            response.StatusCode.Should()
                .Be(HttpStatusCode.NotFound);
        }
        [Fact]
        public async Task DeleteDocument_MissingDocument_ReturnsNotFound()
        {
            HttpResponseMessage response =
                await _client.DeleteAsync(
                    $"/api/documents/{Guid.NewGuid()}");

            response.StatusCode.Should()
                .Be(HttpStatusCode.NotFound);
        }
        private async Task<Document> SeedDocumentAsync(DocumentStatus status)
        {
            using IServiceScope scope = _factory.Services.CreateScope();

            DocuTrackDbContext context = scope.ServiceProvider.GetRequiredService<DocuTrackDbContext>();

            DateTimeOffset now = DateTimeOffset.UtcNow;

            Document document = new()
            {
                Id = Guid.NewGuid(),
                DocumentNumber = $"DOC-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                Title = "Integration Test Document",
                Description = "Created by integration test",
                Type = DocumentType.Contract,
                Department = Department.Purchasing,
                Owner = "Test User",
                Status = status,
                CreatedAt = now,
                LastUpdatedAt = now,
                Version = 1
            };

            context.Documents.Add(document);
            await context.SaveChangesAsync();

            return document;
        }
    }
}

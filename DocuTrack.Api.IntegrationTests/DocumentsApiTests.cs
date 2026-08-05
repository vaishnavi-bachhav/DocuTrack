using DocuTrack.Api.Contracts.Responses;
using DocuTrack.Api.IntegrationTests.Authentication;
using DocuTrack.Api.IntegrationTests.Database;
using DocuTrack.Application.Abstractions.Persistence;
using DocuTrack.Domain.Documents;
using DocuTrack.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocuTrack.Api.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class DocumentsApiTests
    : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions =
        CreateJsonOptions();

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _employeeClient;

    public DocumentsApiTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;

        _employeeClient =
            CreateClientForRole(
                TestAuthHandler.EmployeeRole);
    }

    public async Task InitializeAsync()
    {
        await ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _employeeClient.Dispose();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetDocuments_AnonymousUser_ReturnsUnauthorized()
    {
        using HttpClient client =
            _factory.CreateClient();

        using HttpRequestMessage request = new(
            HttpMethod.Get,
            "/api/documents?pageNumber=1&pageSize=20");

        request.Headers.Add(
            TestAuthHandler.AnonymousHeader,
            "true");

        HttpResponseMessage response =
            await client.SendAsync(request);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDocuments_AuthenticatedEmployee_ReturnsOk()
    {
        HttpResponseMessage response =
            await _employeeClient.GetAsync(
                "/api/documents?pageNumber=1&pageSize=20");

        string responseBody =
            await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            $"response body was: {responseBody}");
    }

    [Fact]
    public async Task CreateDocument_ValidRequest_ReturnsCreated()
    {
        var request = new
        {
            title = "Supplier Agreement",
            description = "Annual supplier agreement",
            documentType = "Contract",
            department = "Purchasing",
            owner = "Test Owner"
        };

        HttpResponseMessage response =
            await _employeeClient.PostAsJsonAsync(
                "/api/documents",
                request);

        string responseBody =
            await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            $"response body was: {responseBody}");

        DocumentResponse? document =
            await response.Content
                .ReadFromJsonAsync<DocumentResponse>(
                    JsonOptions);

        document.Should().NotBeNull();
        document!.Id.Should().NotBe(Guid.Empty);
        document.DocumentNumber.Should().Be("DOC-000001");
        document.Title.Should().Be("Supplier Agreement");
        document.Status.Should().Be(DocumentStatus.Draft);
        document.Version.Should().Be(1);

        document.CreatedByUserId.Should().Be(
            Guid.Parse(
                TestAuthHandler.DefaultUserId));

        document.LastModifiedByUserId.Should().Be(
            Guid.Parse(
                TestAuthHandler.DefaultUserId));

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString()
            .Should()
            .Contain(document.Id.ToString());
    }

    [Fact]
    public async Task CreateDocument_WhitespaceTitle_ReturnsBadRequest()
    {
        var request = new
        {
            title = "   ",
            description = "Description",
            documentType = "Contract",
            department = "Purchasing",
            owner = "Test Owner"
        };

        HttpResponseMessage response =
            await _employeeClient.PostAsJsonAsync(
                "/api/documents",
                request);

        response.StatusCode.Should()
            .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDocumentById_ExistingDocument_ReturnsOk()
    {
        Document document =
            await SeedDocumentAsync(
                DocumentStatus.Draft);

        HttpResponseMessage response =
            await _employeeClient.GetAsync(
                $"/api/documents/{document.Id}");

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        DocumentResponse? body =
            await response.Content
                .ReadFromJsonAsync<DocumentResponse>(
                    JsonOptions);

        body.Should().NotBeNull();
        body!.Id.Should().Be(document.Id);
        body.Title.Should().Be(document.Title);
        body.Status.Should().Be(DocumentStatus.Draft);
    }

    [Fact]
    public async Task GetDocumentById_MissingDocument_ReturnsProblemDetails()
    {
        Guid missingId = Guid.NewGuid();

        HttpResponseMessage response =
            await _employeeClient.GetAsync(
                $"/api/documents/{missingId}");

        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);

        ProblemDetails? problem =
            await response.Content
                .ReadFromJsonAsync<ProblemDetails>(
                    JsonOptions);

        problem.Should().NotBeNull();
        problem!.Status.Should().Be(404);
        problem.Title.Should().NotBeNullOrWhiteSpace();
        problem.Detail.Should().Contain(
            missingId.ToString());

        problem.Instance.Should().Be(
            $"/api/documents/{missingId}");

        problem.Extensions.Should()
            .ContainKey("traceId");
    }

    [Fact]
    public async Task UpdateDocument_ValidRequest_ReturnsUpdatedDocument()
    {
        Document document =
            await SeedDocumentAsync(
                DocumentStatus.Draft);

        var request = new
        {
            title = "Updated Supplier Agreement",
            description = "Updated description",
            documentType = "Invoice",
            department = "Legal",
            owner = "Updated Owner",
            version = 1
        };

        HttpResponseMessage response =
            await _employeeClient.PutAsJsonAsync(
                $"/api/documents/{document.Id}",
                request);

        string responseBody =
            await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            $"response body was: {responseBody}");

        DocumentResponse? body =
            await response.Content
                .ReadFromJsonAsync<DocumentResponse>(
                    JsonOptions);

        body.Should().NotBeNull();
        body!.Title.Should().Be(
            "Updated Supplier Agreement");

        body.Description.Should().Be(
            "Updated description");

        body.Type.Should().Be(DocumentType.Invoice);
        body.Department.Should().Be(Department.Legal);
        body.Owner.Should().Be("Updated Owner");
        body.Version.Should().Be(2);

        body.LastModifiedByUserId.Should().Be(
            Guid.Parse(
                TestAuthHandler.DefaultUserId));
    }

    [Fact]
    public async Task UpdateDocument_InvalidVersion_ReturnsBadRequest()
    {
        Document document =
            await SeedDocumentAsync(
                DocumentStatus.Draft);

        var request = new
        {
            title = "Updated Document",
            description = "Description",
            documentType = "Contract",
            department = "Purchasing",
            owner = "Test Owner",
            version = 0
        };

        HttpResponseMessage response =
            await _employeeClient.PutAsJsonAsync(
                $"/api/documents/{document.Id}",
                request);

        response.StatusCode.Should()
            .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangeStatus_Employee_ReturnsForbidden()
    {
        Document document =
            await SeedDocumentAsync(
                DocumentStatus.Draft);

        var request = new
        {
            newStatus = "Uploaded",
            version = 1
        };

        HttpResponseMessage response =
            await _employeeClient.PatchAsJsonAsync(
                $"/api/documents/{document.Id}/status",
                request);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeStatus_ReviewerValidTransition_ReturnsOk()
    {
        Document document =
            await SeedDocumentAsync(
                DocumentStatus.Draft);

        using HttpClient reviewerClient =
            CreateClientForRole(
                TestAuthHandler.ReviewerRole);

        var request = new
        {
            newStatus = "Uploaded",
            version = 1
        };

        HttpResponseMessage response =
            await reviewerClient.PatchAsJsonAsync(
                $"/api/documents/{document.Id}/status",
                request);

        string responseBody =
            await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            $"response body was: {responseBody}");

        DocumentResponse? body =
            await response.Content
                .ReadFromJsonAsync<DocumentResponse>(
                    JsonOptions);

        body.Should().NotBeNull();
        body!.Status.Should()
            .Be(DocumentStatus.Uploaded);

        body.Version.Should().Be(2);
    }

    [Fact]
    public async Task ChangeStatus_InvalidTransition_ReturnsConflict()
    {
        Document document =
            await SeedDocumentAsync(
                DocumentStatus.Draft);

        using HttpClient reviewerClient =
            CreateClientForRole(
                TestAuthHandler.ReviewerRole);

        var request = new
        {
            newStatus = "Approved",
            version = 1
        };

        HttpResponseMessage response =
            await reviewerClient.PatchAsJsonAsync(
                $"/api/documents/{document.Id}/status",
                request);

        string responseBody =
            await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.Conflict,
            $"response body was: {responseBody}");
    }

    [Fact]
    public async Task DeleteDocument_Employee_ReturnsForbidden()
    {
        Document document =
            await SeedDocumentAsync(
                DocumentStatus.Draft);

        HttpResponseMessage response =
            await _employeeClient.DeleteAsync(
                $"/api/documents/{document.Id}");

        response.StatusCode.Should()
            .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteDocument_AdminDraftDocument_ReturnsNoContent()
    {
        Document document =
            await SeedDocumentAsync(
                DocumentStatus.Draft);

        using HttpClient adminClient =
            CreateClientForRole(
                TestAuthHandler.AdminRole);

        HttpResponseMessage response =
            await adminClient.DeleteAsync(
                $"/api/documents/{document.Id}");

        response.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        await using AsyncServiceScope scope =
            _factory.Services.CreateAsyncScope();

        DocuTrackDbContext context =
            scope.ServiceProvider
                .GetRequiredService<
                    DocuTrackDbContext>();

        Document? deleted =
            await context.Documents
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == document.Id);

        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteDocument_AdminApprovedDocument_ReturnsConflict()
    {
        Document document =
            await SeedDocumentAsync(
                DocumentStatus.Approved);

        using HttpClient adminClient =
            CreateClientForRole(
                TestAuthHandler.AdminRole);

        HttpResponseMessage response =
            await adminClient.DeleteAsync(
                $"/api/documents/{document.Id}");

        response.StatusCode.Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SearchDocuments_FiltersAndPaginates()
    {
        await SeedDocumentAsync(
            DocumentStatus.Draft,
            title: "Alpha Contract",
            department: Department.Purchasing);

        await SeedDocumentAsync(
            DocumentStatus.Draft,
            title: "Bravo Contract",
            department: Department.Purchasing);

        await SeedDocumentAsync(
            DocumentStatus.Draft,
            title: "Legal Document",
            department: Department.Legal);

        string url =
            "/api/documents" +
            "?status=Draft" +
            "&department=Purchasing" +
            "&pageNumber=1" +
            "&pageSize=1" +
            "&sortBy=Title" +
            "&sortDirection=Ascending";

        HttpResponseMessage response =
            await _employeeClient.GetAsync(url);

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        PagedResultResponse? result =
            await response.Content
                .ReadFromJsonAsync<PagedResultResponse>(
                    JsonOptions);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(2);
        result.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
        result.Items[0].Title.Should().Be("Alpha Contract");
    }

    private HttpClient CreateClientForRole(
        string role)
    {
        HttpClient client =
            _factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            TestAuthHandler.RoleHeader,
            role);

        return client;
    }

    private async Task ResetDatabaseAsync()
    {
        await using AsyncServiceScope scope =
            _factory.Services.CreateAsyncScope();

        DocuTrackDbContext context =
            scope.ServiceProvider
                .GetRequiredService<
                    DocuTrackDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        TestDocumentNumberGenerator generator =
            (TestDocumentNumberGenerator)
            scope.ServiceProvider.GetRequiredService<
                IDocumentNumberGenerator>();

        generator.Reset();
    }

    private async Task<Document> SeedDocumentAsync(
        DocumentStatus status,
        string title = "Integration Test Document",
        Department department = Department.Purchasing)
    {
        await using AsyncServiceScope scope =
            _factory.Services.CreateAsyncScope();

        DocuTrackDbContext context =
            scope.ServiceProvider
                .GetRequiredService<
                    DocuTrackDbContext>();

        DateTimeOffset now =
            DateTimeOffset.UtcNow;

        Document document = Document.Create(
            documentNumber:
                $"DOC-{Random.Shared.Next(1, 999999):D6}",
            title: title,
            description: "Created by integration test",
            type: DocumentType.Contract,
            department: department,
            owner: "Test Owner",
            createdByUserId: Guid.Parse(
                TestAuthHandler.DefaultUserId),
            createdAt: now);

        MoveToStatus(
            document,
            status,
            now);

        context.Documents.Add(document);

        await context.SaveChangesAsync();

        return document;
    }

    private static void MoveToStatus(
        Document document,
        DocumentStatus targetStatus,
        DateTimeOffset startingTime)
    {
        Guid userId =
            Guid.Parse(
                TestAuthHandler.DefaultUserId);

        switch (targetStatus)
        {
            case DocumentStatus.Draft:
                return;

            case DocumentStatus.Uploaded:
                MoveToUploaded(
                    document,
                    userId,
                    startingTime);
                return;

            case DocumentStatus.UnderReview:
                MoveToUnderReview(
                    document,
                    userId,
                    startingTime);
                return;

            case DocumentStatus.PendingApproval:
                MoveToPendingApproval(
                    document,
                    userId,
                    startingTime);
                return;

            case DocumentStatus.Approved:
                MoveToApproved(
                    document,
                    userId,
                    startingTime);
                return;

            case DocumentStatus.Rejected:
                MoveToPendingApproval(
                    document,
                    userId,
                    startingTime);

                document.ChangeStatus(
                    DocumentStatus.Rejected,
                    userId,
                    startingTime.AddMinutes(4));

                return;

            case DocumentStatus.Archived:
                MoveToApproved(
                    document,
                    userId,
                    startingTime);

                document.ChangeStatus(
                    DocumentStatus.Archived,
                    userId,
                    startingTime.AddMinutes(5));

                return;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(targetStatus),
                    targetStatus,
                    "Unsupported test document status.");
        }
    }

    private static void MoveToUploaded(
        Document document,
        Guid userId,
        DateTimeOffset time)
    {
        document.ChangeStatus(
            DocumentStatus.Uploaded,
            userId,
            time.AddMinutes(1));
    }

    private static void MoveToUnderReview(
        Document document,
        Guid userId,
        DateTimeOffset time)
    {
        MoveToUploaded(
            document,
            userId,
            time);

        document.ChangeStatus(
            DocumentStatus.UnderReview,
            userId,
            time.AddMinutes(2));
    }

    private static void MoveToPendingApproval(
        Document document,
        Guid userId,
        DateTimeOffset time)
    {
        MoveToUnderReview(
            document,
            userId,
            time);

        document.ChangeStatus(
            DocumentStatus.PendingApproval,
            userId,
            time.AddMinutes(3));
    }

    private static void MoveToApproved(
        Document document,
        Guid userId,
        DateTimeOffset time)
    {
        MoveToPendingApproval(
            document,
            userId,
            time);

        document.ChangeStatus(
            DocumentStatus.Approved,
            userId,
            time.AddMinutes(4));
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options =
            new(JsonSerializerDefaults.Web);

        options.Converters.Add(
            new JsonStringEnumConverter());

        return options;
    }

    private sealed class PagedResultResponse
    {
        public List<DocumentResponse> Items { get; init; } = [];

        public int TotalCount { get; init; }

        public int PageNumber { get; init; }

        public int PageSize { get; init; }

        public int TotalPages { get; init; }

        public bool HasNextPage { get; init; }

        public bool HasPreviousPage { get; init; }
    }
}
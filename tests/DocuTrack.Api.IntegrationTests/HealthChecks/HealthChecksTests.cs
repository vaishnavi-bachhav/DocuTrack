using FluentAssertions;
using System.Net;

namespace DocuTrack.Api.IntegrationTests.HealthChecks
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class HealthChecksTests : IDisposable
    {
        private readonly HttpClient _client;

        public HealthChecksTests(
      CustomWebApplicationFactory factory)
        {
            ArgumentNullException.ThrowIfNull(factory);

            _client = factory.CreateClient(
                new Microsoft.AspNetCore.Mvc.Testing
                    .WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });
        }

        [Fact]
        public async Task LiveHealthCheck_ReturnsOk()
        {
            // Act
            using HttpResponseMessage response =
                await _client.GetAsync("/health/live");

            // Assert
            string responseBody =
                await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(
                HttpStatusCode.OK,
                $"response body was: {responseBody}");
        }

        [Fact]
        public async Task ReadyHealthCheck_WhenDatabaseAvailable_ReturnsOk()
        {
            // Act
            using HttpResponseMessage response =
                await _client.GetAsync("/health/ready");

            // Assert
            string responseBody =
                await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(
                HttpStatusCode.OK,
                $"response body was: {responseBody}");
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}

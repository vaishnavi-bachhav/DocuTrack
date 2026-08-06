using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DocuTrack.Api.HealthChecks
{
    public static class HealthCheckResponseWriter
    {
        public static Task WriteResponseAsync(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(entry => new
                {
                    key = entry.Key,
                    status = entry.Value.Status.ToString(),
                    duration = entry.Value.Duration.TotalMilliseconds,
                    description = entry.Value.Description,
                    exception = entry.Value.Exception?.Message,
                    data = entry.Value.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                })
            };

            return context.Response.WriteAsync
                (JsonSerializer.Serialize(
                    response, 
                    new JsonSerializerOptions 
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true 
                    }));
        }
    }
}

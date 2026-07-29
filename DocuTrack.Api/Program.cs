using DocuTrack.Core.Repositories;
using System.Text.Json.Serialization;
using DocuTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DocuTrack.Infrastructure.Repositories;
using DocuTrack.Core.Services;
using DocuTrack.Api.ExceptionHandling;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options  =>
{
    options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

if (!builder.Environment.IsEnvironment("Testing"))
{
    string connectionString = builder.Configuration.GetConnectionString("DocuTrackDb")
        ?? throw new InvalidOperationException(
            "Connection string 'DocuTrackDb' was not found.");

    builder.Services.AddDbContext<DocuTrackDbContext>(
        options =>
        {
            options.UseSqlServer(connectionString);
        });
}

builder.Services.AddScoped<IDocumentRepository, EfDocumentRepository>();
builder.Services.AddScoped<DocumentService>();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "DocuTrack API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
using DocuTrack.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DocuTrack.Api.ExceptionHandling
{
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(exception);

            ProblemDetails problemDetails = exception switch
            {
                DocumentNotFoundException notFoundException => new ProblemDetails
                {
                    Title = "Document Not Found",
                    Detail = notFoundException.Message,
                    Status = StatusCodes.Status404NotFound,
                    Type = "https://httpstatuses.com/404"
                },

                InvalidDocumentStatusTransitionException transitionException => new ProblemDetails
                {
                    Title = "Invalid Document Status Transition",
                    Detail = transitionException.Message,
                    Status = StatusCodes.Status409Conflict,
                    Type = "https://httpstatuses.com/409"
                },

                DocumentDeletionNotAllowedException deletionException => new ProblemDetails
                {
                    Title = "Document Deletion Not Allowed",
                    Detail = deletionException.Message,
                    Status = StatusCodes.Status409Conflict,
                    Type = "https://httpstatuses.com/409"
                },

                DomainValidationException validationException => new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = validationException.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://httpstatuses.com/400"
                },

                _ => new ProblemDetails
                {
                    Title = "An unexpected error occurred",
                    Detail = "The server encountered an unexpected error.",
                    Status = StatusCodes.Status500InternalServerError,
                    Type = "https://httpstatuses.com/500"
                }
            };

            if (problemDetails.Status == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(
                                exception,
                                "Unhandled exception occurred while processing {Method} {Path}",
                                context.Request.Method,
                                context.Request.Path);
            }
            else
            {
                _logger.LogWarning(
                                exception,
                                "Handled exception occurred while processing {Method} {Path}",
                                context.Request.Method,
                                context.Request.Path);
            }

            problemDetails.Instance = context.Request.Path;
            problemDetails.Extensions["traceId"] = context.TraceIdentifier;
            context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

            await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}

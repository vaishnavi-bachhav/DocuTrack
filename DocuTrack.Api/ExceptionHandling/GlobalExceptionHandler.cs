using DocuTrack.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            {
                return false;
            }

            ProblemDetails problemDetails = exception switch
            {
                DocumentNotFoundException notFoundException =>
                    CreateProblemDetails(
                        StatusCodes.Status404NotFound,
                        "Document not found",
                        notFoundException.Message,
                        "https://httpstatuses.com/404"),

                DocumentConcurrencyException concurrencyException =>
                    CreateProblemDetails(
                        StatusCodes.Status409Conflict,
                        "Document concurrency conflict",
                        concurrencyException.Message,
                        "https://httpstatuses.com/409"),

                InvalidDocumentStatusTransitionException transitionException =>
                    CreateProblemDetails(
                        StatusCodes.Status409Conflict,
                        "Invalid document status transition",
                        transitionException.Message,
                        "https://httpstatuses.com/409"),

                DocumentDeletionNotAllowedException deletionException =>
                    CreateProblemDetails(
                        StatusCodes.Status409Conflict,
                        "Document deletion not allowed",
                        deletionException.Message,
                        "https://httpstatuses.com/409"),

                DomainValidationException validationException =>
                    CreateProblemDetails(
                        StatusCodes.Status400BadRequest,
                        "Validation failed",
                        validationException.Message,
                        "https://httpstatuses.com/400"),

                DatabaseConflictException conflictException =>
                    CreateProblemDetails(
                        StatusCodes.Status409Conflict,
                        "Database conflict",
                        conflictException.Message,
                        "https://httpstatuses.com/409"),

                DatabaseUnavailableException =>
                    CreateProblemDetails(
                        StatusCodes.Status503ServiceUnavailable,
                        "Service temporarily unavailable",
                        "The database is temporarily unavailable. Please try again later.",
                        "https://httpstatuses.com/503"),

                DbUpdateConcurrencyException =>
                    CreateProblemDetails(
                        StatusCodes.Status409Conflict,
                        "Document concurrency conflict",
                        "The document was modified or deleted by another request.",
                        "https://httpstatuses.com/409"),

                DbUpdateException =>
                    CreateProblemDetails(
                        StatusCodes.Status500InternalServerError,
                        "Database operation failed",
                        "An unexpected database error occurred.",
                        "https://httpstatuses.com/500"),

                AuthenticationFailedException authenticationException =>
                    CreateProblemDetails(
                        StatusCodes.Status401Unauthorized,
                        "Authentication failed",
                        authenticationException.Message,
                        "https://httpstatuses.com/401"),

                UserAlreadyExistsException existingUserException =>
                    CreateProblemDetails(
                        StatusCodes.Status409Conflict,
                        "User already exists",
                        existingUserException.Message,
                        "https://httpstatuses.com/409"),

                AccountLockedException lockedException =>
                    CreateProblemDetails(
                        StatusCodes.Status423Locked,
                        "Account locked",
                        lockedException.Message,
                        "https://httpstatuses.com/423"),

                UserRegistrationException registrationException =>
                    CreateProblemDetails(
                        StatusCodes.Status400BadRequest,
                        "Registration failed",
                        registrationException.Message,
                        "https://httpstatuses.com/400"),

                _ =>
                    CreateProblemDetails(
                        StatusCodes.Status500InternalServerError,
                        "An unexpected error occurred",
                        "The server encountered an unexpected error.",
                        "https://httpstatuses.com/500")
            };

            switch (exception)
            {
                case DocumentNotFoundException:
                case DomainValidationException:
                    _logger.LogInformation(
                        exception,
                        "Request rejected for {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path
                    );
                    break;

                case InvalidDocumentStatusTransitionException:
                case DocumentDeletionNotAllowedException:
                case DocumentConcurrencyException:
                case DatabaseConflictException:
                case DbUpdateConcurrencyException:
                    _logger.LogWarning(
                        exception,
                        "Request conflict for {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path);
                    break;

                case DatabaseUnavailableException:
                    _logger.LogError(
                        exception,
                        "Database unavailable while processing {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path);
                    break;

                case AuthenticationFailedException:
                    _logger.LogInformation(
                        "Authentication failed for {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path);
                    break;

                case UserAlreadyExistsException:
                    _logger.LogInformation(
                        exception,
                        "Registration conflict for {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path);
                    break;

                case AccountLockedException:
                    _logger.LogWarning(
                        exception,
                        "Locked account attempted authentication for {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path);
                    break;

                case UserRegistrationException:
                    _logger.LogWarning(
                        exception,
                        "User registration failed for {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path);
                    break;

                default:
                    _logger.LogError(
                        exception,
                        "Unhandled exception while processing {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path);
                    break;
            }

            problemDetails.Instance = context.Request.Path;
            problemDetails.Extensions["traceId"] = context.TraceIdentifier;
            context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(problemDetails,
                options: null,
                contentType: "application/problem+json",
                cancellationToken);

            return true;
        }

        private static ProblemDetails CreateProblemDetails(int status, string title, string detail, string type)
        {
            return new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Type = type
            };
        }
    }
}
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyCondo.Domain.Exceptions;
using AppEx = MyCondo.Application.Common.Exceptions.ApplicationException;
using AppNotFound = MyCondo.Application.Common.Exceptions.NotFoundException;
using AppConflict = MyCondo.Application.Common.Exceptions.ConflictException;
using AppForbidden = MyCondo.Application.Common.Exceptions.ForbiddenException;

namespace MyCondo.Api.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionMiddleware> logger,
    IWebHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        (int status, string title, string detail) = ex switch
        {
            ValidationException ve =>
                (StatusCodes.Status400BadRequest, "Validation failed", FormatValidation(ve)),
            AppNotFound nf =>
                (StatusCodes.Status404NotFound, "Resource not found", nf.Message),
            AppConflict cf =>
                (StatusCodes.Status409Conflict, "Conflict", cf.Message),
            AppForbidden fb =>
                (StatusCodes.Status403Forbidden, "Forbidden", fb.Message),
            AppEx app =>
                (StatusCodes.Status400BadRequest, "Application error", app.Message),
            DomainException de =>
                (StatusCodes.Status422UnprocessableEntity, "Domain rule violated", de.Message),
            UnauthorizedAccessException =>
                (StatusCodes.Status401Unauthorized, "Unauthorized", "Authentication required."),
            _ =>
                (StatusCodes.Status500InternalServerError, "Internal server error",
                    env.IsDevelopment() ? ex.ToString() : "An unexpected error occurred.")
        };

        if (status >= 500)
        {
            logger.LogError(ex, "Unhandled exception {ExceptionType}", ex.GetType().Name);
        }
        else
        {
            logger.LogWarning("Handled exception {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = status;
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Title = title,
                Detail = detail,
                Status = status,
                Type = $"https://httpstatuses.io/{status}"
            },
            Exception = ex
        });
    }

    private static string FormatValidation(ValidationException ve) =>
        string.Join("; ", ve.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
}

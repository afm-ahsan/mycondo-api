using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyCondo.Domain.Exceptions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Tenancy;
using AppEx = MyCondo.Application.Common.Exceptions.ApplicationException;
using AppNotFound = MyCondo.Application.Common.Exceptions.NotFoundException;
using AppConflict = MyCondo.Application.Common.Exceptions.ConflictException;
using AppForbidden = MyCondo.Application.Common.Exceptions.ForbiddenException;
using FeatureNotEntitled = MyCondo.Application.Common.Exceptions.FeatureNotEntitledException;
using OrgLifecycleDenied = MyCondo.Application.Common.Exceptions.OrganizationLifecycleAccessDeniedException;
using SubscriptionLifecycleDenied = MyCondo.Application.Common.Exceptions.SubscriptionLifecycleAccessDeniedException;

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
            FeatureNotEntitled fe =>
                (StatusCodes.Status403Forbidden, "Feature not entitled", fe.Message),
            OrgLifecycleDenied old =>
                (StatusCodes.Status403Forbidden, "Organization access denied", old.Message),
            SubscriptionLifecycleDenied sld =>
                (StatusCodes.Status403Forbidden, "Subscription access restricted", sld.Message),
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

        ProblemDetails problem = new()
        {
            Title = title,
            Detail = detail,
            Status = status,
            Type = $"https://httpstatuses.io/{status}"
        };

        // Distinct error code (ADR-033 §16) so the frontend can show "not in your plan" instead of the
        // generic RBAC "you don't have permission" 403 it already shows for AppForbidden. Deliberately
        // carries only the feature key — no package price/discount/billing details (ADR-033 Task 06 §12).
        if (ex is FeatureNotEntitled featureNotEntitled)
        {
            problem.Extensions["code"] = "feature_not_entitled";
            problem.Extensions["feature"] = featureNotEntitled.FeatureKey;
        }

        // Lifecycle-specific codes (ADR-032 Task 10 §27/§28) so the frontend can distinguish an
        // organization/subscription lockout from RBAC or feature-entitlement denials — deliberately
        // carries only the lifecycle state, never subscription id/package/price/balance (ADR-032 §29).
        if (ex is OrgLifecycleDenied orgDenied)
        {
            problem.Extensions["code"] = orgDenied.Status == TenantStatus.Closed ? "tenant_closed" : "tenant_suspended";
        }

        if (ex is SubscriptionLifecycleDenied subscriptionDenied)
        {
            problem.Extensions["code"] = subscriptionDenied.SubscriptionStatus == OrganizationSubscriptionStatus.Restricted
                ? "subscription_restricted"
                : "subscription_expired";
        }

        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = ex
        });
    }

    private static string FormatValidation(ValidationException ve) =>
        string.Join("; ", ve.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
}

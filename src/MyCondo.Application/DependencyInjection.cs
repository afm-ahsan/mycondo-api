using System.Reflection;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Behaviors;
using MyCondo.Application.Common.Events;
using MyCondo.Application.Common.Services;
using MyCondo.Application.Features.Amenities.Common;
using MyCondo.Application.Features.Billing.Services;
using MyCondo.Application.Features.Finance.FinancialStatements.Notes;
using MyCondo.Application.Features.Finance.FinancialStatements.Services;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Application.Features.Utilities.Common;

namespace MyCondo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        Assembly assembly = typeof(DependencyInjection).Assembly;

        // Mediator's source generator registers request/command/query handlers automatically, but
        // NOT pipeline behaviors — those need explicit registration (open generics aren't picked up
        // implicitly). Order is registration order = execution order, outermost first: unhandled
        // exceptions wrap everything so they can observe/log any failure; logging brackets each
        // request; performance times validation+handler; validation runs immediately before the
        // handler; tenant lifecycle (ADR-032 §6, Task 10) runs after validation so a structurally
        // invalid request never spends a lifecycle lookup, and before feature entitlement so a hard
        // organization/subscription lockout is authoritative before a commercial entitlement lookup
        // runs (ADR-032 Task 10 §31/§32 — never lets a lifecycle denial masquerade as
        // feature_not_entitled); feature entitlement (ADR-033 §16) runs last, immediately before the
        // handler. Scoped to match handler lifetime and because ValidationBehavior depends on scoped
        // IValidator<T> registrations.
        services.AddMediator(opts =>
        {
            opts.ServiceLifetime = ServiceLifetime.Scoped;
        });

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TenantLifecycleBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(FeatureEntitlementBehavior<,>));

        // ADR-033 Task 09A — one resource-derived feature resolver per request family. .NET's built-in
        // container does NOT support registering multiple open generics against the same open service
        // type distinguished only by generic constraint (it throws on the first non-matching descriptor
        // rather than trying the next), so RegisterResolvedFeatureResolvers scans for each family's
        // IHasXxxId-marked requests and registers one CLOSED IRequestFeatureResolver<TRequest> per
        // request — same reflection-scan shape as RegisterDomainEventHandlers below, so a new request
        // joining an existing family needs no new line here.
        RegisterResolvedFeatureResolvers(services, assembly, typeof(IHasFacilityId), typeof(FacilityFeatureResolver<>));
        RegisterResolvedFeatureResolvers(services, assembly, typeof(IHasBookingId), typeof(BookingFacilityFeatureResolver<>));
        RegisterResolvedFeatureResolvers(services, assembly, typeof(IHasBlackoutDateId), typeof(BlackoutDateFacilityFeatureResolver<>));
        RegisterResolvedFeatureResolvers(services, assembly, typeof(IHasMeterId), typeof(MeterFeatureResolver<>));
        RegisterResolvedFeatureResolvers(services, assembly, typeof(IHasReadingId), typeof(ReadingFeatureResolver<>));
        RegisterResolvedFeatureResolvers(services, assembly, typeof(IHasRatePlanId), typeof(RatePlanFeatureResolver<>));

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<IPermissionSeeder, PermissionSeeder>();
        services.AddScoped<IFeatureCatalogueSeeder, FeatureCatalogueSeeder>();
        services.AddScoped<ITenantEntitlementService, TenantEntitlementService>();
        services.AddScoped<ITenantLifecycleAccessService, TenantLifecycleAccessService>();
        services.AddScoped<ISubscriptionLifecycleAccessService, SubscriptionLifecycleAccessService>();
        services.AddScoped<IOrganizationAdminBootstrapper, OrganizationAdminBootstrapper>();
        services.AddScoped<IDefaultRoleCatalogueSeeder, DefaultRoleCatalogueSeeder>();
        services.AddScoped<ICondominiumRoleCatalogueSeeder, CondominiumRoleCatalogueSeeder>();
        services.AddScoped<IResidentRoleCatalogueSeeder, ResidentRoleCatalogueSeeder>();
        services.AddScoped<IExpenseCategoryCatalogueSeeder, ExpenseCategoryCatalogueSeeder>();
        services.AddScoped<IExpenseTypeCatalogueSeeder, ExpenseTypeCatalogueSeeder>();
        services.AddScoped<IFinanceChartOfAccountSeeder, FinanceChartOfAccountSeeder>();
        services.AddScoped<IFlatAccessAuthorizer, FlatAccessAuthorizer>();
        services.AddScoped<IFlatDisplayNameResolver, FlatDisplayNameResolver>();
        services.AddSingleton<IImageValidationService, ImageValidationService>();
        services.AddScoped<IFinancialPostingService, FinancialPostingService>();
        services.AddScoped<IFinancialStatementReportingService, FinancialStatementReportingService>();
        services.AddScoped<IFinancialStatementNoteService, FinancialStatementNoteService>();
        services.AddScoped<IResponsiblePartyResolver, ResponsiblePartyResolver>();

        // Domain-event dispatch bypasses Mediator (see IDomainEventHandler comment for why).
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        RegisterDomainEventHandlers(services, assembly);

        return services;
    }

    private static void RegisterResolvedFeatureResolvers(
        IServiceCollection services, Assembly assembly, Type markerInterface, Type openResolverType)
    {
        foreach (Type requestType in assembly.GetTypes())
        {
            if (requestType.IsAbstract || requestType.IsInterface)
            {
                continue;
            }

            if (!markerInterface.IsAssignableFrom(requestType) || !typeof(IRequiresResolvedFeature).IsAssignableFrom(requestType))
            {
                continue;
            }

            Type serviceType = typeof(IRequestFeatureResolver<>).MakeGenericType(requestType);
            Type implementationType = openResolverType.MakeGenericType(requestType);
            services.AddScoped(serviceType, implementationType);
        }
    }

    private static void RegisterDomainEventHandlers(IServiceCollection services, Assembly assembly)
    {
        Type openHandler = typeof(IDomainEventHandler<>);

        foreach (Type type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            foreach (Type iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == openHandler)
                {
                    services.AddScoped(iface, type);
                }
            }
        }
    }
}

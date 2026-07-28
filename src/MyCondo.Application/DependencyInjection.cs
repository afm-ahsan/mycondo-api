using System.Reflection;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Behaviors;
using MyCondo.Application.Common.Events;
using MyCondo.Application.Common.Services;

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
        // handler. Scoped to match handler lifetime and because ValidationBehavior depends on
        // scoped IValidator<T> registrations.
        services.AddMediator(opts =>
        {
            opts.ServiceLifetime = ServiceLifetime.Scoped;
        });

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<ISuperAdminBootstrapper, SuperAdminBootstrapper>();
        services.AddScoped<IDefaultRoleCatalogueSeeder, DefaultRoleCatalogueSeeder>();

        // Domain-event dispatch bypasses Mediator (see IDomainEventHandler comment for why).
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        RegisterDomainEventHandlers(services, assembly);

        return services;
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

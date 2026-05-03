using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Common.Events;

namespace MyCondo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        Assembly assembly = typeof(DependencyInjection).Assembly;

        // Mediator's source generator emits AddMediator(...) at compile time and registers all
        // request handlers and pipeline behaviors found in the same compilation. Lifetime is
        // Scoped so handlers can resolve scoped dependencies like the DbContext.
        services.AddMediator(opts =>
        {
            opts.ServiceLifetime = ServiceLifetime.Scoped;
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

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

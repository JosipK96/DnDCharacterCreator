using CharacterCreatorApp.Application.Common.Behaviours;

using FluentValidation;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace CharacterCreatorApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(x => x.RegisterServicesFromAssembly(assembly));

        // Registration order is pipeline order: logging goes outside validation so that a
        // request the validator rejects is logged too.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddValidatorsFromAssembly(assembly);

        // The clock handlers and the audit interceptor read; tests pass a FakeTimeProvider
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}

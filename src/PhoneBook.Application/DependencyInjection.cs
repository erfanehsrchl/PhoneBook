using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PhoneBook.Application.Behaviors;
using System.Reflection;

namespace PhoneBook.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        params Assembly[] additionalMappingAssemblies)
    {
        TypeAdapterConfig mappingConfig = TypeAdapterConfig.GlobalSettings;
        Assembly applicationAssembly = typeof(DependencyInjection).Assembly;
        Assembly[] mappingAssemblies = additionalMappingAssemblies
            .Prepend(applicationAssembly)
            .Distinct()
            .ToArray();
        mappingConfig.Scan(mappingAssemblies);

        services.AddSingleton(mappingConfig);

        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(
                applicationAssembly));

        services.AddValidatorsFromAssembly(applicationAssembly);

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>));

        return services;
    }
}

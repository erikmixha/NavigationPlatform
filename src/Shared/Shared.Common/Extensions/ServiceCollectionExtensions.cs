using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shared.Common.Behaviors;

namespace Shared.Common.Extensions;

/// <summary>
/// Extension methods for configuring common services.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Extension methods for service configuration.
/// Configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for service configuration. Tested via integration tests.")]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds common services including MediatR validation behavior.
    /// </summary>
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }

    /// <summary>
    /// Adds FluentValidation validators from the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="T">The type whose assembly contains the validators.</typeparam>
    public static IServiceCollection AddValidatorsFromAssemblyContaining<T>(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(T).Assembly);
        return services;
    }
}

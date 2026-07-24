using eQuantic.Core.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace eQuantic.Core.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDateTimeProviderService(this IServiceCollection services)
    {
#if NET8_0
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
#endif
        services.TryAddSingleton<IDateTimeProviderService, DateTimeProviderService>();
        return services;
    }
}
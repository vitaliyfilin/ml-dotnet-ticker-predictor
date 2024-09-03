using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TickerPredictor.Services;

namespace TickerPredictor.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTickerPredictor(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddSingleton<ITickerPredictor, TickerPredictor>();
        services.AddLogging(configure => configure.AddConsole());

        return services;
    }
}
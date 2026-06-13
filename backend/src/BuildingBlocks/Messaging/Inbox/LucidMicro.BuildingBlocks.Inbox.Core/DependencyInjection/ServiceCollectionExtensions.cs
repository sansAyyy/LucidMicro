using LucidMicro.BuildingBlocks.Inbox.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LucidMicro.BuildingBlocks.Inbox.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLucidInboxProcessor(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IInboxProcessingTransaction, NoOpInboxProcessingTransaction>();
        services.TryAddScoped<IInboxMessageProcessor, DefaultInboxMessageProcessor>();

        return services;
    }
}

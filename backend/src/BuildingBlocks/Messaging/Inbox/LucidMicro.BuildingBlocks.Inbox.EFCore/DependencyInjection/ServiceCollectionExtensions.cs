using LucidMicro.BuildingBlocks.Inbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Inbox.Core.Contracts;
using LucidMicro.BuildingBlocks.Inbox.EFCore.Stores;
using LucidMicro.BuildingBlocks.Inbox.EFCore.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LucidMicro.BuildingBlocks.Inbox.EFCore.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLucidEfCoreInbox<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IInboxMessageStore, EfCoreInboxMessageStore<TDbContext>>();
        services.AddScoped<IInboxProcessingTransaction, EfCoreInboxProcessingTransaction<TDbContext>>();

        return services;
    }
}

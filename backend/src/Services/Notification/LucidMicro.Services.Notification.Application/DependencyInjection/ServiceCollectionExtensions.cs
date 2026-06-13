using LucidMicro.BuildingBlocks.Inbox.Core.DependencyInjection;
using LucidMicro.Services.Notification.Application.Features.Notifications.Abstractions;
using LucidMicro.Services.Notification.Application.Features.Notifications.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.Services.Notification.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<INotificationApplicationService, NotificationApplicationService>();
        services.AddLucidInboxProcessor();

        return services;
    }
}

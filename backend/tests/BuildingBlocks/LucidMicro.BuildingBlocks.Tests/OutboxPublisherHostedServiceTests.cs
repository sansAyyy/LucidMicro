using System.Collections.Concurrent;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Outbox.Core.DependencyInjection;
using LucidMicro.BuildingBlocks.Outbox.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class OutboxPublisherHostedServiceTests
{
    [Fact]
    public async Task HostedService_PublishesPendingMessagesOnStart()
    {
        var publisher = new TestOutboxPublisher();
        var services = CreateServices(
            publisher,
            options =>
            {
                options.Interval = TimeSpan.FromSeconds(30);
                options.BatchSize = 7;
            });

        await using var serviceProvider = services.BuildServiceProvider();
        var hostedService = serviceProvider.GetServices<IHostedService>().Single();

        await hostedService.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => publisher.CallCount >= 1);
        await hostedService.StopAsync(CancellationToken.None);

        Assert.Equal(7, Assert.Single(publisher.BatchSizes));
    }

    [Fact]
    public async Task HostedService_ContinuesAfterPublisherFailure()
    {
        var publisher = new TestOutboxPublisher
        {
            FailFirstCall = true
        };
        var services = CreateServices(
            publisher,
            options =>
            {
                options.Interval = TimeSpan.FromMilliseconds(10);
                options.BatchSize = 3;
            });

        await using var serviceProvider = services.BuildServiceProvider();
        var hostedService = serviceProvider.GetServices<IHostedService>().Single();

        await hostedService.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => publisher.CallCount >= 2);
        await hostedService.StopAsync(CancellationToken.None);

        Assert.True(publisher.CallCount >= 2);
        Assert.Contains(3, publisher.BatchSizes);
    }

    [Fact]
    public void AddLucidOutboxPublisherHostedService_RegistersHostedService()
    {
        var services = new ServiceCollection();

        services.AddLucidOutboxPublisherHostedService();

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IHostedService));
    }

    private static ServiceCollection CreateServices(
        TestOutboxPublisher publisher,
        Action<OutboxPublisherOptions> configureOptions)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<IOutboxPublisher>(publisher);
        services.AddLucidOutboxPublisherHostedService(configureOptions);

        return services;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        while (!condition())
        {
            await Task.Delay(10, cancellationTokenSource.Token);
        }
    }

    private sealed class TestOutboxPublisher : IOutboxPublisher
    {
        private int _callCount;

        public bool FailFirstCall { get; init; }

        public int CallCount => Volatile.Read(ref _callCount);

        public ConcurrentQueue<int> BatchSizes { get; } = [];

        public Task PublishPendingAsync(
            int maxCount = 50,
            CancellationToken cancellationToken = default)
        {
            var callCount = Interlocked.Increment(ref _callCount);
            BatchSizes.Enqueue(maxCount);

            if (FailFirstCall && callCount == 1)
            {
                throw new InvalidOperationException("publisher failed");
            }

            return Task.CompletedTask;
        }
    }
}

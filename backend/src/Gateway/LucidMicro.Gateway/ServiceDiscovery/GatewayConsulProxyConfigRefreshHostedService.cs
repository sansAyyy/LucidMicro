using LucidMicro.Gateway.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LucidMicro.Gateway.ServiceDiscovery;

public sealed class GatewayConsulProxyConfigRefreshHostedService : IHostedService
{
    private readonly GatewayConsulProxyConfigProvider _configProvider;
    private readonly LucidGatewayServiceDiscoveryOptions _options;
    private readonly ILogger<GatewayConsulProxyConfigRefreshHostedService> _logger;

    private CancellationTokenSource? _stoppingTokenSource;
    private Task? _executingTask;

    public GatewayConsulProxyConfigRefreshHostedService(
        GatewayConsulProxyConfigProvider configProvider,
        LucidGatewayServiceDiscoveryOptions options,
        ILogger<GatewayConsulProxyConfigRefreshHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(configProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        options.Validate();

        _configProvider = configProvider;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RefreshAsync(cancellationToken);

        _stoppingTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = ExecuteAsync(_stoppingTokenSource.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executingTask is null || _stoppingTokenSource is null)
        {
            return;
        }

        await _stoppingTokenSource.CancelAsync();

        await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        _stoppingTokenSource.Dispose();
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.RefreshIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshAsync(stoppingToken);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _configProvider.RefreshAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Gateway service discovery refresh failed.");
        }
    }
}

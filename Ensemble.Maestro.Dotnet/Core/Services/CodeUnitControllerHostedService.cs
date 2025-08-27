using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Ensemble.Maestro.Dotnet.Core.Services;

public class CodeUnitControllerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CodeUnitControllerHostedService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMilliseconds(100); // High-speed polling for mechanical processing

    public CodeUnitControllerHostedService(
        IServiceProvider serviceProvider,
        ILogger<CodeUnitControllerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 CUCS HostedService started - listening for CodeUnitAssignmentMessages");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var messageCoordinator = scope.ServiceProvider.GetRequiredService<IMessageCoordinatorService>();
                var codeUnitController = scope.ServiceProvider.GetRequiredService<CodeUnitControllerService>();

                // Subscribe to CodeUnitAssignment messages and process them
                var subscription = await messageCoordinator.SubscribeToCodeUnitAssignmentsAsync(stoppingToken);
                await foreach (var assignment in subscription)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    try
                    {
                        _logger.LogInformation("📥 CUCS HostedService received assignment: {CodeUnitName}", assignment?.Name);
                        
                        // Process assignment mechanically and rapidly
                        if (assignment != null)
                        {
                            await codeUnitController.ProcessCodeUnitAssignmentAsync(assignment);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error processing CodeUnit assignment: {CodeUnitName}", 
                            assignment?.Name ?? "Unknown");
                        
                        // Continue processing other messages even if one fails
                        continue;
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("CodeUnitController HostedService stopping due to cancellation");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CodeUnitController HostedService main loop");
                
                // Brief delay before retrying to avoid tight error loops
                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }

        _logger.LogInformation("CodeUnitController HostedService stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CodeUnitController HostedService is stopping");
        await base.StopAsync(cancellationToken);
    }
}
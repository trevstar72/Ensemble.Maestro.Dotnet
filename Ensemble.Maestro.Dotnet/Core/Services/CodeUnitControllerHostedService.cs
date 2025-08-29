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
        _logger.LogInformation("🚀 CUCS HostedService ExecuteAsync called - starting initialization");
        _logger.LogInformation("🚀 CUCS HostedService started - listening for CodeUnitAssignmentMessages on Redis queue");
        _logger.LogInformation("⏱️ CUCS HostedService polling interval: {PollingInterval}ms", _pollingInterval.TotalMilliseconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("🔄 CUCS HostedService main loop iteration starting - creating new scope for message processing");
                
                using var scope = _serviceProvider.CreateScope();
                _logger.LogInformation("🔧 CUCS HostedService scope created successfully");
                
                _logger.LogInformation("🔍 CUCS HostedService resolving IMessageCoordinatorService...");
                var messageCoordinator = scope.ServiceProvider.GetRequiredService<IMessageCoordinatorService>();
                _logger.LogInformation("✅ CUCS HostedService IMessageCoordinatorService resolved successfully");
                
                _logger.LogInformation("🔍 CUCS HostedService resolving CodeUnitControllerService...");
                var codeUnitController = scope.ServiceProvider.GetRequiredService<CodeUnitControllerService>();
                _logger.LogInformation("✅ CUCS HostedService CodeUnitControllerService resolved successfully");

                _logger.LogInformation("📡 CUCS HostedService calling SubscribeToCodeUnitAssignmentsAsync on Redis queue");
                
                // Subscribe to CodeUnitAssignment messages and process them
                var subscription = await messageCoordinator.SubscribeToCodeUnitAssignmentsAsync(stoppingToken);
                
                _logger.LogInformation("✅ CUCS HostedService subscription established successfully - beginning message enumeration loop");
                
                var messageCount = 0;
                await foreach (var assignment in subscription)
                {
                    messageCount++;
                    _logger.LogInformation("📨 CUCS HostedService RECEIVED MESSAGE #{MessageCount} from subscription", messageCount);
                    if (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("🛑 CUCS HostedService stopping due to cancellation token");
                        break;
                    }

                    try
                    {
                        _logger.LogInformation("📥 CUCS HostedService processing message #{MessageCount}", messageCount);
                        
                        if (assignment == null)
                        {
                            _logger.LogWarning("⚠️ CUCS HostedService received NULL assignment in message #{MessageCount} - ignoring", messageCount);
                            continue;
                        }
                        
                        _logger.LogInformation("📋 CUCS HostedService assignment details - Name: {CodeUnitName}, Functions: {FunctionCount}, AssignmentId: {AssignmentId}", 
                            assignment.Name ?? "NULL", assignment.Functions?.Count ?? 0, assignment.AssignmentId ?? "NULL");
                        
                        if (assignment.Functions != null && assignment.Functions.Any())
                        {
                            var functionNames = string.Join(", ", assignment.Functions.Select(f => f.FunctionName ?? "Unknown"));
                            _logger.LogInformation("🔧 CUCS HostedService function list: [{Functions}]", functionNames);
                        }
                        
                        _logger.LogInformation("⚡ CUCS HostedService calling ProcessCodeUnitAssignmentAsync for CodeUnit: {CodeUnitName}", assignment.Name);
                        await codeUnitController.ProcessCodeUnitAssignmentAsync(assignment);
                        _logger.LogInformation("✅ CUCS HostedService ProcessCodeUnitAssignmentAsync completed successfully for CodeUnit: {CodeUnitName}", assignment.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ CUCS HostedService CRITICAL ERROR processing message #{MessageCount} for CodeUnit: {CodeUnitName}. Stack trace: {StackTrace}", 
                            messageCount, assignment?.Name ?? "Unknown", ex.StackTrace);
                        
                        // Continue processing other messages even if one fails
                        continue;
                    }
                }
                
                _logger.LogInformation("🔚 CUCS HostedService subscription enumeration ended naturally - will retry main loop");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("🛑 CUCS HostedService main loop stopping due to cancellation token");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 CUCS HostedService CRITICAL ERROR in main loop - Exception: {ExceptionType}, Message: {ExceptionMessage}, Stack: {StackTrace}", 
                    ex.GetType().Name, ex.Message, ex.StackTrace);
                
                // Brief delay before retrying to avoid tight error loops
                _logger.LogInformation("⏳ CUCS HostedService waiting {DelayMs}ms before retrying main loop", _pollingInterval.TotalMilliseconds);
                try
                {
                    await Task.Delay(_pollingInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("🛑 CUCS HostedService stopping during delay due to cancellation token");
                    break;
                }
                _logger.LogInformation("🔄 CUCS HostedService delay completed - retrying main loop");
            }
        }

        _logger.LogInformation("🏁 CUCS HostedService ExecuteAsync method ending - service stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CodeUnitController HostedService is stopping");
        await base.StopAsync(cancellationToken);
    }
}
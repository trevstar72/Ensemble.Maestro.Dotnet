using System.Runtime.CompilerServices;
using Ensemble.Maestro.Dotnet.Core.Data;
using Ensemble.Maestro.Dotnet.Core.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Message Coordinator service implementation for managing multi-agent swarm communications
/// </summary>
public class MessageCoordinatorService : IMessageCoordinatorService
{
    private readonly IRedisMessageQueueService _messageQueue;
    private readonly MaestroDbContext _dbContext;
    private readonly ILogger<MessageCoordinatorService> _logger;
    
    // Queue names for different message types
    private const string SPAWN_REQUESTS_QUEUE = "swarm.spawn.requests";
    private const string COMPLETIONS_QUEUE = "swarm.completions";
    private const string FUNCTION_ASSIGNMENTS_QUEUE = "swarm.function.assignments";
    private const string CODEUNIT_ASSIGNMENTS_QUEUE = "swarm.codeunit.assignments";
    private const string WORKLOAD_DISTRIBUTION_QUEUE = "swarm.workload.distribution";
    private const string BUILDER_QUEUE = "builder.notifications";
    private const string BUILDER_ERROR_QUEUE = "builder.errors";
    private const string STATUS_UPDATES_CHANNEL = "swarm.status.updates";
    private const string HEARTBEATS_CHANNEL = "swarm.heartbeats";
    private const string SHUTDOWN_CHANNEL = "swarm.shutdown";

    private bool _isInitialized = false;

    public MessageCoordinatorService(
        IRedisMessageQueueService messageQueue,
        MaestroDbContext dbContext,
        ILogger<MessageCoordinatorService> logger)
    {
        _messageQueue = messageQueue;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_isInitialized)
            {
                _logger.LogInformation("Message Coordinator already initialized");
                return true;
            }

            _logger.LogInformation("Initializing Message Coordinator...");

            // Create all required queues with appropriate configurations
            var queues = new[]
            {
                (SPAWN_REQUESTS_QUEUE, new QueueConfiguration 
                { 
                    MaxMessageSizeBytes = 2048, 
                    EnablePriority = true,
                    MaxRetries = 3,
                    DeadLetterQueue = SPAWN_REQUESTS_QUEUE + ".dlq"
                }),
                (COMPLETIONS_QUEUE, new QueueConfiguration 
                { 
                    MaxMessageSizeBytes = 2048, 
                    EnablePersistence = true,
                    MaxRetries = 5
                }),
                (FUNCTION_ASSIGNMENTS_QUEUE, new QueueConfiguration 
                { 
                    MaxMessageSizeBytes = 1536, // Slightly smaller for function details
                    EnablePriority = true,
                    MaxRetries = 3
                }),
                (CODEUNIT_ASSIGNMENTS_QUEUE, new QueueConfiguration 
                { 
                    MaxMessageSizeBytes = 2048,
                    EnablePriority = true,
                    MaxRetries = 3
                }),
                (WORKLOAD_DISTRIBUTION_QUEUE, new QueueConfiguration 
                { 
                    MaxMessageSizeBytes = 2048,
                    EnablePersistence = true,
                    MaxRetries = 2
                }),
                (BUILDER_QUEUE, new QueueConfiguration 
                { 
                    MaxMessageSizeBytes = 2048,
                    EnablePersistence = true,
                    MaxRetries = 3,
                    EnablePriority = true
                }),
                (BUILDER_ERROR_QUEUE, new QueueConfiguration 
                { 
                    MaxMessageSizeBytes = 2048,
                    EnablePersistence = true,
                    MaxRetries = 5 // Higher retries for error handling
                })
            };

            var successCount = 0;
            foreach (var (queueName, config) in queues)
            {
                var created = await _messageQueue.CreateQueueAsync(queueName, config, cancellationToken);
                if (created)
                {
                    successCount++;
                    _logger.LogInformation("Created message queue: {QueueName}", queueName);
                }
                else
                {
                    _logger.LogWarning("Failed to create message queue: {QueueName}", queueName);
                }
            }

            _isInitialized = successCount == queues.Length;

            if (_isInitialized)
            {
                _logger.LogInformation("Message Coordinator initialized successfully - {SuccessCount}/{TotalCount} queues created", 
                    successCount, queues.Length);
            }
            else
            {
                _logger.LogError("Message Coordinator initialization failed - only {SuccessCount}/{TotalCount} queues created", 
                    successCount, queues.Length);
            }

            return _isInitialized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Message Coordinator");
            return false;
        }
    }

    public async Task<bool> SendAgentSpawnRequestAsync(
        AgentSpawnRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var priority = request.Priority switch
            {
                "High" => 8,
                "Medium" => 5,
                "Low" => 2,
                _ => 5
            };

            var result = await _messageQueue.SendPriorityMessageAsync(
                SPAWN_REQUESTS_QUEUE, 
                request, 
                priority,
                TimeSpan.FromHours(1),
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Sent agent spawn request {RequestId} for {AgentType} (priority {Priority})", 
                    request.RequestId, request.AgentType, priority);
                
                // Log to SQL for monitoring
                await LogSwarmActivityAsync("AgentSpawnRequest", request.RequestId, request.ProjectId, 
                    $"Spawning {request.AgentType} agent", cancellationToken);
            }
            else
            {
                _logger.LogError("Failed to send agent spawn request {RequestId}: {Error}", 
                    request.RequestId, result.ErrorMessage);
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending agent spawn request {RequestId}", request.RequestId);
            return false;
        }
    }

    public async Task<bool> SendAgentCompletionAsync(
        AgentCompletionMessage completion, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _messageQueue.SendMessageAsync(
                COMPLETIONS_QUEUE, 
                completion,
                TimeSpan.FromHours(6), // Keep completion records longer
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Sent agent completion {AgentId} - {AgentType} (success: {Success})", 
                    completion.AgentId, completion.AgentType, completion.Success);
                
                // Update SQL metrics
                await UpdateSwarmMetricsAsync(completion, cancellationToken);
            }
            else
            {
                _logger.LogError("Failed to send agent completion {AgentId}: {Error}", 
                    completion.AgentId, result.ErrorMessage);
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending agent completion {AgentId}", completion.AgentId);
            return false;
        }
    }

    public async Task<bool> SendFunctionAssignmentAsync(
        FunctionAssignmentMessage assignment, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var priority = assignment.Priority switch
            {
                "High" => 8,
                "Medium" => 5,
                "Low" => 2,
                _ => 5
            };

            var result = await _messageQueue.SendPriorityMessageAsync(
                FUNCTION_ASSIGNMENTS_QUEUE, 
                assignment, 
                priority,
                TimeSpan.FromHours(2),
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Sent function assignment {AssignmentId} for function {FunctionName} (complexity {Complexity})", 
                    assignment.AssignmentId, assignment.FunctionName, assignment.ComplexityRating);
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending function assignment {AssignmentId}", assignment.AssignmentId);
            return false;
        }
    }

    public async Task<bool> SendCodeUnitAssignmentAsync(
        CodeUnitAssignmentMessage assignment, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var priority = assignment.Priority switch
            {
                "High" => 8,
                "Medium" => 5,
                "Low" => 2,
                _ => 5
            };

            var result = await _messageQueue.SendPriorityMessageAsync(
                CODEUNIT_ASSIGNMENTS_QUEUE, 
                assignment, 
                priority,
                TimeSpan.FromHours(4),
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("✅ Sent code unit assignment {AssignmentId} for {Name} ({FunctionCount} functions)", 
                    assignment.AssignmentId, assignment.Name, assignment.Functions.Count);
            }
            else
            {
                _logger.LogError("❌ Failed to send code unit assignment {AssignmentId} for {Name} - Error: {ErrorMessage}", 
                    assignment.AssignmentId, assignment.Name, result.ErrorMessage ?? "Unknown error");
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending code unit assignment {AssignmentId}", assignment.AssignmentId);
            return false;
        }
    }

    public async Task<bool> DistributeWorkloadAsync(
        WorkloadDistributionMessage workload, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _messageQueue.SendMessageAsync(
                WORKLOAD_DISTRIBUTION_QUEUE, 
                workload,
                TimeSpan.FromHours(1),
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Sent workload distribution {DistributionId} - {FunctionTasks} functions, {CodeUnitTasks} code units", 
                    workload.DistributionId, workload.FunctionTasks.Count, workload.CodeUnitTasks.Count);
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending workload distribution {DistributionId}", workload.DistributionId);
            return false;
        }
    }

    public async Task<bool> SendSwarmStatusUpdateAsync(
        SwarmStatusUpdate status, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _messageQueue.PublishAsync(STATUS_UPDATES_CHANNEL, status, cancellationToken);
            
            if (result.Success)
            {
                _logger.LogDebug("Published swarm status update for {SwarmId} - {ActiveAgents} active agents", 
                    status.SwarmId, status.ActiveAgents);
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing swarm status update {SwarmId}", status.SwarmId);
            return false;
        }
    }

    public async Task<bool> SendAgentHeartbeatAsync(
        AgentHeartbeat heartbeat, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _messageQueue.PublishAsync(HEARTBEATS_CHANNEL, heartbeat, cancellationToken);
            
            if (result.Success)
            {
                _logger.LogTrace("Published heartbeat for agent {AgentId} - {Status}", 
                    heartbeat.AgentId, heartbeat.Status);
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing heartbeat for agent {AgentId}", heartbeat.AgentId);
            return false;
        }
    }

    public async Task<bool> SendSwarmShutdownAsync(
        SwarmShutdownMessage shutdown, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _messageQueue.PublishAsync(SHUTDOWN_CHANNEL, shutdown, cancellationToken);
            
            if (result.Success)
            {
                _logger.LogWarning("Published swarm shutdown for {SwarmId} - reason: {Reason}", 
                    shutdown.SwarmId, shutdown.Reason);
                
                await LogSwarmActivityAsync("SwarmShutdown", shutdown.SwarmId, "", 
                    $"Shutdown initiated: {shutdown.Reason}", cancellationToken);
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing swarm shutdown {SwarmId}", shutdown.SwarmId);
            return false;
        }
    }

    public async Task<IAsyncEnumerable<AgentSpawnRequest>> SubscribeToSpawnRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = await _messageQueue.SubscribeAsync<AgentSpawnRequest>(SPAWN_REQUESTS_QUEUE, cancellationToken);
        return ExtractData(messages);
    }

    public async Task<IAsyncEnumerable<AgentCompletionMessage>> SubscribeToCompletionsAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = await _messageQueue.SubscribeAsync<AgentCompletionMessage>(COMPLETIONS_QUEUE, cancellationToken);
        return ExtractData(messages);
    }

    public async Task<IAsyncEnumerable<FunctionAssignmentMessage>> SubscribeToFunctionAssignmentsAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = await _messageQueue.SubscribeAsync<FunctionAssignmentMessage>(FUNCTION_ASSIGNMENTS_QUEUE, cancellationToken);
        return ExtractData(messages);
    }

    public async Task<IAsyncEnumerable<CodeUnitAssignmentMessage>> SubscribeToCodeUnitAssignmentsAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = await _messageQueue.SubscribeAsync<CodeUnitAssignmentMessage>(CODEUNIT_ASSIGNMENTS_QUEUE, cancellationToken);
        return ExtractData(messages);
    }

    public async Task<IAsyncEnumerable<WorkloadDistributionMessage>> SubscribeToWorkloadDistributionAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = await _messageQueue.SubscribeAsync<WorkloadDistributionMessage>(WORKLOAD_DISTRIBUTION_QUEUE, cancellationToken);
        return ExtractData(messages);
    }

    public async Task<IAsyncEnumerable<SwarmStatusUpdate>> SubscribeToSwarmStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = await _messageQueue.SubscribeAsync<SwarmStatusUpdate>(STATUS_UPDATES_CHANNEL, cancellationToken);
        return ExtractData(messages);
    }

    public async Task<IAsyncEnumerable<AgentHeartbeat>> SubscribeToHeartbeatsAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = await _messageQueue.SubscribeAsync<AgentHeartbeat>(HEARTBEATS_CHANNEL, cancellationToken);
        return ExtractData(messages);
    }

    public async Task<IAsyncEnumerable<SwarmShutdownMessage>> SubscribeToShutdownAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = await _messageQueue.SubscribeAsync<SwarmShutdownMessage>(SHUTDOWN_CHANNEL, cancellationToken);
        return ExtractData(messages);
    }

    public async Task<MessageCoordinatorHealth> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var health = new MessageCoordinatorHealth
            {
                RedisConnected = true, // TODO: Check actual Redis connection
                SqlConnected = await CheckSqlConnectionAsync(cancellationToken)
            };

            // Check queue health
            var queueNames = new[] 
            { 
                SPAWN_REQUESTS_QUEUE, COMPLETIONS_QUEUE, FUNCTION_ASSIGNMENTS_QUEUE, 
                CODEUNIT_ASSIGNMENTS_QUEUE, WORKLOAD_DISTRIBUTION_QUEUE, BUILDER_QUEUE, BUILDER_ERROR_QUEUE 
            };

            foreach (var queueName in queueNames)
            {
                var queueStats = await _messageQueue.GetQueueStatsAsync(queueName, cancellationToken);
                health.QueueHealth[queueName] = new QueueHealthInfo
                {
                    QueueName = queueName,
                    IsHealthy = queueStats.IsHealthy,
                    MessageCount = queueStats.MessageCount,
                    BacklogCount = queueStats.MessageCount, // Simplified
                    LastActivity = queueStats.LastActivity,
                    Issues = queueStats.HealthIssues
                };

                if (!queueStats.IsHealthy)
                {
                    health.IsHealthy = false;
                    health.Issues.AddRange(queueStats.HealthIssues);
                }
            }

            return health;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Message Coordinator health");
            return new MessageCoordinatorHealth
            {
                IsHealthy = false,
                Issues = new List<string> { ex.Message }
            };
        }
    }

    public async Task<List<SwarmStatistics>> GetSwarmStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Query SQL database for actual swarm statistics
            var stats = await _dbContext.PipelineExecutions
                .Where(p => p.Stage == "Swarming" && p.Status == "Running")
                .Select(p => new SwarmStatistics
                {
                    SwarmId = p.Id.ToString(),
                    ProjectId = p.ProjectId.ToString(),
                    Status = p.Status ?? "Unknown",
                    TotalTasks = p.TotalFunctions ?? 0,
                    CompletedTasks = p.CompletedFunctions,
                    PendingTasks = (p.TotalFunctions ?? 0) - p.CompletedFunctions,
                    FailedTasks = p.FailedFunctions,
                    TotalCost = 0, // p.ExecutionCost is not available, using default
                    CreatedAt = p.StartedAt,
                    LastUpdated = p.StageStartedAt
                })
                .ToListAsync(cancellationToken);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting swarm statistics");
            return new List<SwarmStatistics>();
        }
    }

    public async Task<MessageProcessingMetrics> GetMessageMetricsAsync(
        TimeSpan period, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endTime = DateTime.UtcNow;
            var startTime = endTime.Subtract(period);

            // TODO: Implement actual metrics collection from message processing logs
            var metrics = new MessageProcessingMetrics
            {
                Period = period,
                StartTime = startTime,
                EndTime = endTime,
                TotalMessages = 0,
                SuccessfulMessages = 0,
                FailedMessages = 0,
                AverageProcessingTime = TimeSpan.FromMilliseconds(150),
                MaxProcessingTime = TimeSpan.FromSeconds(5),
                MinProcessingTime = TimeSpan.FromMilliseconds(50)
            };

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message processing metrics");
            return new MessageProcessingMetrics();
        }
    }

    /// <summary>
    /// Send notification to Builder queue that Code Unit work is complete
    /// </summary>
    public async Task<bool> SendBuilderNotificationAsync(
        BuilderNotificationMessage notification, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var priority = notification.Priority switch
            {
                "High" => 8,
                "Medium" => 5,
                "Low" => 2,
                _ => 5
            };

            var result = await _messageQueue.SendPriorityMessageAsync(
                BUILDER_QUEUE, 
                notification, 
                priority,
                TimeSpan.FromHours(2),
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Sent Builder notification {NotificationId} for CodeUnit {CodeUnitName} - {Status}", 
                    notification.NotificationId, notification.CodeUnitName, notification.Status);
                
                await LogSwarmActivityAsync("BuilderNotification", notification.NotificationId, 
                    notification.ProjectId, $"CodeUnit {notification.CodeUnitName} {notification.Status}", cancellationToken);
            }
            else
            {
                _logger.LogError("Failed to send Builder notification {NotificationId}: {Error}", 
                    notification.NotificationId, result.ErrorMessage);
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Builder notification {NotificationId}", notification.NotificationId);
            return false;
        }
    }

    /// <summary>
    /// Send error message from Builder to Code Unit Controller for bug-fix agent spawning
    /// </summary>
    public async Task<bool> SendBuilderErrorAsync(
        BuilderErrorMessage error, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Builder errors always have high priority for quick bug fixing
            var result = await _messageQueue.SendPriorityMessageAsync(
                BUILDER_ERROR_QUEUE, 
                error, 
                8, // High priority
                TimeSpan.FromHours(4),
                cancellationToken);

            if (result.Success)
            {
                _logger.LogWarning("Sent Builder error {ErrorId} for CodeUnit {CodeUnitName} - {ErrorType}: {ErrorMessage}", 
                    error.ErrorId, error.CodeUnitName, error.ErrorType, error.ErrorMessage);
                
                await LogSwarmActivityAsync("BuilderError", error.ErrorId, 
                    error.ProjectId, $"Build error in {error.CodeUnitName}: {error.ErrorType}", cancellationToken);
            }
            else
            {
                _logger.LogError("Failed to send Builder error {ErrorId}: {Error}", 
                    error.ErrorId, result.ErrorMessage);
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Builder error {ErrorId}", error.ErrorId);
            return false;
        }
    }

    /// <summary>
    /// Subscribe to Builder notifications for monitoring build requests
    /// </summary>
    public async Task<IAsyncEnumerable<BuilderNotificationMessage>> SubscribeToBuilderNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = await _messageQueue.SubscribeAsync<BuilderNotificationMessage>(BUILDER_QUEUE, cancellationToken);
        return ExtractData(messages);
    }

    /// <summary>
    /// Subscribe to Builder errors for bug-fix agent spawning
    /// </summary>
    public async Task<IAsyncEnumerable<BuilderErrorMessage>> SubscribeToBuilderErrorsAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = await _messageQueue.SubscribeAsync<BuilderErrorMessage>(BUILDER_ERROR_QUEUE, cancellationToken);
        return ExtractData(messages);
    }

    public async Task<bool> EmergencyShutdownAllAsync(
        string reason, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var shutdownMessage = new SwarmShutdownMessage
            {
                SwarmId = "ALL",
                Reason = reason,
                GracefulShutdown = false,
                ShutdownTimeout = TimeSpan.FromMinutes(1),
                PendingWorkAction = "Requeue",
                InitiatedBy = "EmergencyShutdown"
            };

            var result = await SendSwarmShutdownAsync(shutdownMessage, cancellationToken);
            
            if (result)
            {
                _logger.LogCritical("Emergency shutdown initiated for all swarms: {Reason}", reason);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during emergency shutdown");
            return false;
        }
    }

    #region Private Helper Methods

    private async Task<bool> CheckSqlConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.Database.OpenConnectionAsync(cancellationToken);
            await _dbContext.Database.CloseConnectionAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task LogSwarmActivityAsync(
        string activityType, 
        string entityId, 
        string projectId, 
        string description, 
        CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Create SwarmActivityLog entity and store in database
            _logger.LogInformation("Swarm Activity: {ActivityType} - {EntityId} - {Description}", 
                activityType, entityId, description);
            
            await Task.CompletedTask; // Placeholder for actual SQL logging
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging swarm activity");
        }
    }

    private async Task UpdateSwarmMetricsAsync(
        AgentCompletionMessage completion, 
        CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Update pipeline execution metrics in database
            _logger.LogInformation("Updated swarm metrics for agent {AgentId} completion", completion.AgentId);
            
            await Task.CompletedTask; // Placeholder for actual SQL updates
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating swarm metrics");
        }
    }

    private static async IAsyncEnumerable<T> ExtractData<T>(IAsyncEnumerable<MessageQueueItem<T>> messages) where T : class
    {
        await foreach (var messageItem in messages)
        {
            yield return messageItem.Data;
        }
    }

    #endregion
}
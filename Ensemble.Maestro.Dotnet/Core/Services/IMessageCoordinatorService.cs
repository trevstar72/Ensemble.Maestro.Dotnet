using Ensemble.Maestro.Dotnet.Core.Messages;

namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Message Coordinator service for managing multi-agent swarm communications with SQL monitoring
/// </summary>
public interface IMessageCoordinatorService
{
    /// <summary>
    /// Initialize the message coordinator with queue setup
    /// </summary>
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send agent spawn request to the swarm
    /// </summary>
    Task<bool> SendAgentSpawnRequestAsync(
        AgentSpawnRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send agent completion notification
    /// </summary>
    Task<bool> SendAgentCompletionAsync(
        AgentCompletionMessage completion, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send function assignment to Method Agent
    /// </summary>
    Task<bool> SendFunctionAssignmentAsync(
        FunctionAssignmentMessage assignment, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send code unit assignment to Code Unit Controller
    /// </summary>
    Task<bool> SendCodeUnitAssignmentAsync(
        CodeUnitAssignmentMessage assignment, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Distribute workload across available agents
    /// </summary>
    Task<bool> DistributeWorkloadAsync(
        WorkloadDistributionMessage workload, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send swarm status update
    /// </summary>
    Task<bool> SendSwarmStatusUpdateAsync(
        SwarmStatusUpdate status, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send agent heartbeat
    /// </summary>
    Task<bool> SendAgentHeartbeatAsync(
        AgentHeartbeat heartbeat, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send swarm shutdown command
    /// </summary>
    Task<bool> SendSwarmShutdownAsync(
        SwarmShutdownMessage shutdown, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send notification to Builder queue that Code Unit work is complete
    /// </summary>
    Task<bool> SendBuilderNotificationAsync(
        BuilderNotificationMessage notification, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send error message from Builder to Code Unit Controller for bug-fix agent spawning
    /// </summary>
    Task<bool> SendBuilderErrorAsync(
        BuilderErrorMessage error, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Subscribe to agent spawn requests
    /// </summary>
    Task<IAsyncEnumerable<AgentSpawnRequest>> SubscribeToSpawnRequestsAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Subscribe to agent completions
    /// </summary>
    Task<IAsyncEnumerable<AgentCompletionMessage>> SubscribeToCompletionsAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Subscribe to function assignments
    /// </summary>
    Task<IAsyncEnumerable<FunctionAssignmentMessage>> SubscribeToFunctionAssignmentsAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Subscribe to code unit assignments
    /// </summary>
    Task<IAsyncEnumerable<CodeUnitAssignmentMessage>> SubscribeToCodeUnitAssignmentsAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Subscribe to workload distribution messages
    /// </summary>
    Task<IAsyncEnumerable<WorkloadDistributionMessage>> SubscribeToWorkloadDistributionAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Subscribe to swarm status updates
    /// </summary>
    Task<IAsyncEnumerable<SwarmStatusUpdate>> SubscribeToSwarmStatusAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Subscribe to agent heartbeats
    /// </summary>
    Task<IAsyncEnumerable<AgentHeartbeat>> SubscribeToHeartbeatsAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Subscribe to swarm shutdown commands
    /// </summary>
    Task<IAsyncEnumerable<SwarmShutdownMessage>> SubscribeToShutdownAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Subscribe to Builder notifications for monitoring build requests
    /// </summary>
    Task<IAsyncEnumerable<BuilderNotificationMessage>> SubscribeToBuilderNotificationsAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Subscribe to Builder errors for bug-fix agent spawning
    /// </summary>
    Task<IAsyncEnumerable<BuilderErrorMessage>> SubscribeToBuilderErrorsAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get message queue health and statistics
    /// </summary>
    Task<MessageCoordinatorHealth> GetHealthAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get active swarm statistics
    /// </summary>
    Task<List<SwarmStatistics>> GetSwarmStatisticsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get message processing metrics
    /// </summary>
    Task<MessageProcessingMetrics> GetMessageMetricsAsync(
        TimeSpan period, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Create emergency shutdown for all swarms
    /// </summary>
    Task<bool> EmergencyShutdownAllAsync(
        string reason, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Message coordinator health information
/// </summary>
public class MessageCoordinatorHealth
{
    public bool IsHealthy { get; set; } = true;
    public List<string> Issues { get; set; } = new();
    
    // Queue health
    public Dictionary<string, QueueHealthInfo> QueueHealth { get; set; } = new();
    
    // Connection status
    public bool RedisConnected { get; set; }
    public bool SqlConnected { get; set; }
    
    // Performance metrics
    public double MessagesPerSecond { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public int ActiveSubscriptions { get; set; }
    
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual queue health information
/// </summary>
public class QueueHealthInfo
{
    public string QueueName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; } = true;
    public long MessageCount { get; set; }
    public long BacklogCount { get; set; }
    public DateTime LastActivity { get; set; }
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// Swarm statistics from SQL monitoring
/// </summary>
public class SwarmStatistics
{
    public string SwarmId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    
    // Agent counts
    public int TotalAgents { get; set; }
    public int ActiveAgents { get; set; }
    public int CompletedAgents { get; set; }
    public int FailedAgents { get; set; }
    
    // Task progress
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingTasks { get; set; }
    public int FailedTasks { get; set; }
    
    // Performance metrics
    public decimal TotalCost { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageTaskTime { get; set; }
    
    // Resource utilization
    public int MaxConcurrentAgents { get; set; }
    public int CurrentConcurrentAgents { get; set; }
    public double UtilizationPercentage { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Message processing metrics
/// </summary>
public class MessageProcessingMetrics
{
    public TimeSpan Period { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    
    // Message counts by type
    public Dictionary<string, long> MessageCountsByType { get; set; } = new();
    
    // Processing performance
    public long TotalMessages { get; set; }
    public long SuccessfulMessages { get; set; }
    public long FailedMessages { get; set; }
    public double SuccessRate => TotalMessages > 0 ? (double)SuccessfulMessages / TotalMessages * 100 : 0;
    
    // Timing metrics
    public TimeSpan AverageProcessingTime { get; set; }
    public TimeSpan MaxProcessingTime { get; set; }
    public TimeSpan MinProcessingTime { get; set; }
    
    // Queue metrics
    public long PeakQueueDepth { get; set; }
    public long AverageQueueDepth { get; set; }
    
    // Error analysis
    public Dictionary<string, int> ErrorsByType { get; set; } = new();
    public List<string> TopErrors { get; set; } = new();
    
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
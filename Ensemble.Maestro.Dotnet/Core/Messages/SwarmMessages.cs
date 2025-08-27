namespace Ensemble.Maestro.Dotnet.Core.Messages;

/// <summary>
/// Message types for swarm coordination and agent spawning
/// </summary>
public static class SwarmMessageTypes
{
    public const string AGENT_SPAWN_REQUEST = "swarm.agent.spawn";
    public const string AGENT_COMPLETION = "swarm.agent.completed";
    public const string FUNCTION_ASSIGNMENT = "swarm.function.assign";
    public const string CODE_UNIT_ASSIGNMENT = "swarm.codeunit.assign";
    public const string SWARM_STATUS_UPDATE = "swarm.status.update";
    public const string WORKLOAD_DISTRIBUTION = "swarm.workload.distribute";
    public const string AGENT_HEARTBEAT = "swarm.agent.heartbeat";
    public const string SWARM_SHUTDOWN = "swarm.shutdown";
    public const string BUILDER_NOTIFICATION = "builder.notification";
    public const string BUILDER_ERROR = "builder.error";
}

/// <summary>
/// Request to spawn a new agent for function implementation
/// </summary>
public class AgentSpawnRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
    public string AgentType { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string PipelineExecutionId { get; set; } = string.Empty;
    public string? ParentAgentId { get; set; }
    
    // For Function-specific agents (Method Agents)
    public string? FunctionSpecificationId { get; set; }
    public string? FunctionName { get; set; }
    public int FunctionComplexity { get; set; }
    
    // For Code Unit Controllers
    public string? CodeUnitId { get; set; }
    public string? CodeUnitName { get; set; }
    public int CodeUnitComplexity { get; set; }
    public int FunctionCount { get; set; }
    
    // Context and input
    public string InputPrompt { get; set; } = string.Empty;
    public string? TargetLanguage { get; set; }
    public string Priority { get; set; } = "Medium";
    public Dictionary<string, object> Context { get; set; } = new();
    
    // Swarm limits and constraints
    public int MaxConcurrentAgents { get; set; } = 5;
    public TimeSpan EstimatedDuration { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(1);
}

/// <summary>
/// Agent completion notification with results
/// </summary>
public class AgentCompletionMessage
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Results
    public string OutputResponse { get; set; } = string.Empty;
    public int QualityScore { get; set; }
    public int ConfidenceScore { get; set; }
    public decimal ExecutionCost { get; set; }
    public int DurationSeconds { get; set; }
    
    // For spawning follow-up agents
    public List<AgentSpawnRequest> FollowUpRequests { get; set; } = new();
    
    // Metrics
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Function assignment to Method Agent
/// </summary>
public class FunctionAssignmentMessage
{
    public string AssignmentId { get; set; } = Guid.NewGuid().ToString("N");
    public string FunctionSpecificationId { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public string CodeUnit { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Implementation requirements
    public string? BusinessLogic { get; set; }
    public string? ValidationRules { get; set; }
    public string? ErrorHandling { get; set; }
    public string? SecurityConsiderations { get; set; }
    public string? TestCases { get; set; }
    
    // Metadata
    public int ComplexityRating { get; set; }
    public int EstimatedMinutes { get; set; }
    public string Priority { get; set; } = "Medium";
    public string TargetLanguage { get; set; } = "CSharp";
    
    // Assignment tracking
    public string? AssignedAgentId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime DueAt { get; set; } = DateTime.UtcNow.AddHours(2);
}

/// <summary>
/// Code Unit assignment to Code Unit Controller agent
/// </summary>
public class CodeUnitAssignmentMessage
{
    public string AssignmentId { get; set; } = Guid.NewGuid().ToString("N");
    public string CodeUnitId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;
    public string? Namespace { get; set; }
    public string? Description { get; set; }
    
    // Functions to implement
    public List<FunctionAssignmentMessage> Functions { get; set; } = new();
    public int SimpleFunctionCount { get; set; }
    public int ComplexFunctionCount { get; set; }
    
    // Implementation details
    public string? Dependencies { get; set; }
    public string? Patterns { get; set; }
    public string? TestingStrategy { get; set; }
    
    // Metadata
    public int ComplexityRating { get; set; }
    public int EstimatedMinutes { get; set; }
    public string Priority { get; set; } = "Medium";
    public string TargetLanguage { get; set; } = "CSharp";
    
    // Assignment tracking
    public string? AssignedControllerId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime DueAt { get; set; } = DateTime.UtcNow.AddHours(4);
}

/// <summary>
/// Swarm status and health update
/// </summary>
public class SwarmStatusUpdate
{
    public string SwarmId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string PipelineExecutionId { get; set; } = string.Empty;
    
    // Current state
    public int ActiveAgents { get; set; }
    public int PendingTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int FailedTasks { get; set; }
    
    // Resource utilization
    public int MaxConcurrentAgents { get; set; }
    public int AvailableSlots { get; set; }
    public decimal CurrentCost { get; set; }
    public decimal EstimatedTotalCost { get; set; }
    
    // Health metrics
    public double SuccessRate { get; set; }
    public TimeSpan AverageTaskTime { get; set; }
    public int QueueBacklog { get; set; }
    
    // Issues and bottlenecks
    public List<string> HealthIssues { get; set; } = new();
    public bool IsHealthy { get; set; } = true;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Workload distribution message for load balancing
/// </summary>
public class WorkloadDistributionMessage
{
    public string DistributionId { get; set; } = Guid.NewGuid().ToString("N");
    public string ProjectId { get; set; } = string.Empty;
    
    // Work to distribute
    public List<FunctionAssignmentMessage> FunctionTasks { get; set; } = new();
    public List<CodeUnitAssignmentMessage> CodeUnitTasks { get; set; } = new();
    
    // Distribution strategy
    public string Strategy { get; set; } = "Priority"; // Priority, RoundRobin, LoadBased
    public int PreferredBatchSize { get; set; } = 10;
    public int MaxConcurrency { get; set; } = 5;
    
    // Constraints
    public TimeSpan Deadline { get; set; }
    public decimal BudgetLimit { get; set; }
    public List<string> RequiredCapabilities { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Agent heartbeat for health monitoring
/// </summary>
public class AgentHeartbeat
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public string Status { get; set; } = "Running"; // Running, Idle, Busy, Completed, Failed
    
    // Current work
    public string? CurrentTaskId { get; set; }
    public string? CurrentTaskType { get; set; }
    public int ProgressPercentage { get; set; }
    
    // Resource usage
    public int InputTokensUsed { get; set; }
    public int OutputTokensUsed { get; set; }
    public decimal CostIncurred { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    
    // Health metrics
    public bool IsHealthy { get; set; } = true;
    public List<string> Issues { get; set; } = new();
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime NextHeartbeat { get; set; } = DateTime.UtcNow.AddSeconds(30);
}

/// <summary>
/// Swarm shutdown command
/// </summary>
public class SwarmShutdownMessage
{
    public string SwarmId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool GracefulShutdown { get; set; } = true;
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromMinutes(5);
    
    // What to do with pending work
    public string PendingWorkAction { get; set; } = "Complete"; // Complete, Cancel, Requeue
    
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    public string InitiatedBy { get; set; } = "System";
}

/// <summary>
/// Notification from Code Unit Controller to Builder that work is complete
/// </summary>
public class BuilderNotificationMessage
{
    public string NotificationId { get; set; } = Guid.NewGuid().ToString("N");
    public string ProjectId { get; set; } = string.Empty;
    public string PipelineExecutionId { get; set; } = string.Empty;
    public string CodeUnitControllerId { get; set; } = string.Empty;
    public string CodeUnitName { get; set; } = string.Empty;
    public string CodeUnitId { get; set; } = string.Empty;
    
    // Status information
    public string Status { get; set; } = string.Empty; // "Ready", "Complete", "Failed"
    public string Message { get; set; } = string.Empty;
    
    // Work summary
    public int TotalFunctions { get; set; }
    public int CompletedFunctions { get; set; }
    public int FailedFunctions { get; set; }
    
    // Quality metrics
    public int QualityScore { get; set; }
    public decimal TotalCost { get; set; }
    public int TotalDurationSeconds { get; set; }
    
    // Priority and scheduling
    public string Priority { get; set; } = "Medium";
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public DateTime NotifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Error message from Builder to Code Unit Controller for bug-fix agent spawning
/// </summary>
public class BuilderErrorMessage
{
    public string ErrorId { get; set; } = Guid.NewGuid().ToString("N");
    public string ProjectId { get; set; } = string.Empty;
    public string PipelineExecutionId { get; set; } = string.Empty;
    public string CodeUnitName { get; set; } = string.Empty;
    public string CodeUnitId { get; set; } = string.Empty;
    public string BuilderAgentId { get; set; } = string.Empty;
    
    // Error details
    public string ErrorType { get; set; } = string.Empty; // "CompileError", "RuntimeError", "TestFailure", "ValidationError"
    public string ErrorMessage { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
    public string? StackTrace { get; set; }
    
    // Affected function/method information
    public string? FunctionName { get; set; }
    public string? FunctionSignature { get; set; }
    public int? LineNumber { get; set; }
    
    // Build context
    public string? BuildStage { get; set; } // "Compilation", "Testing", "Integration", "Deployment"
    public string? BuildOutput { get; set; }
    
    // Error severity and priority
    public int Severity { get; set; } = 5; // 1-10 scale, 10 being critical
    public string Priority { get; set; } = "High"; // Always high priority for build errors
    
    // Suggested fix information
    public string? SuggestedFix { get; set; }
    public List<string> RelatedFunctions { get; set; } = new();
    
    public DateTime ErrorOccurredAt { get; set; } = DateTime.UtcNow;
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
}
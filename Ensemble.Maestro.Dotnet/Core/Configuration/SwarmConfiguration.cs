namespace Ensemble.Maestro.Dotnet.Core.Configuration;

/// <summary>
/// Configurable settings for multi-agent swarm behavior and limits
/// </summary>
public class SwarmConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Swarm";
    
    /// <summary>
    /// Maximum concurrent agents allowed in the swarm (default: 10)
    /// </summary>
    public int MaxConcurrentAgents { get; set; } = 10;
    
    /// <summary>
    /// Maximum number of agents per project (default: 50)
    /// </summary>
    public int MaxAgentsPerProject { get; set; } = 50;
    
    /// <summary>
    /// Maximum number of Method Agents per Code Unit Controller (default: 8)
    /// </summary>
    public int MaxMethodAgentsPerController { get; set; } = 8;
    
    /// <summary>
    /// Maximum number of Code Unit Controllers per project (default: 15)
    /// </summary>
    public int MaxControllers { get; set; } = 15;
    
    /// <summary>
    /// Complexity threshold for spawning Method Agents (functions >= this get dedicated agents)
    /// </summary>
    public int ComplexityThresholdForMethodAgent { get; set; } = 4;
    
    /// <summary>
    /// Maximum cost per project in USD (default: $100)
    /// </summary>
    public decimal MaxCostPerProject { get; set; } = 100.00m;
    
    /// <summary>
    /// Maximum execution time per agent in minutes (default: 10 minutes)
    /// </summary>
    public int MaxAgentExecutionMinutes { get; set; } = 10;
    
    /// <summary>
    /// Maximum queue depth for pending agent requests (default: 1000)
    /// </summary>
    public int MaxQueueDepth { get; set; } = 1000;
    
    /// <summary>
    /// Throttling settings for agent spawning
    /// </summary>
    public SwarmThrottling Throttling { get; set; } = new();
    
    /// <summary>
    /// Retry settings for failed agents
    /// </summary>
    public SwarmRetrySettings Retry { get; set; } = new();
    
    /// <summary>
    /// Resource limits per agent type
    /// </summary>
    public Dictionary<string, AgentResourceLimits> ResourceLimits { get; set; } = new()
    {
        { "Designer", new AgentResourceLimits { MaxTokens = 4000, MaxCostPerExecution = 5.00m, TimeoutMinutes = 5 } },
        { "UIDesigner", new AgentResourceLimits { MaxTokens = 3000, MaxCostPerExecution = 3.00m, TimeoutMinutes = 4 } },
        { "APIDesigner", new AgentResourceLimits { MaxTokens = 3500, MaxCostPerExecution = 4.00m, TimeoutMinutes = 5 } },
        { "CodeUnitController", new AgentResourceLimits { MaxTokens = 2000, MaxCostPerExecution = 2.00m, TimeoutMinutes = 8 } },
        { "MethodAgent", new AgentResourceLimits { MaxTokens = 1500, MaxCostPerExecution = 1.50m, TimeoutMinutes = 3 } }
    };
    
    /// <summary>
    /// Priority settings for different types of work
    /// </summary>
    public SwarmPrioritySettings Priority { get; set; } = new();
    
    /// <summary>
    /// Health monitoring settings
    /// </summary>
    public SwarmHealthSettings Health { get; set; } = new();
    
    /// <summary>
    /// Auto-scaling settings
    /// </summary>
    public SwarmAutoScaling AutoScaling { get; set; } = new();
}

/// <summary>
/// Throttling configuration to prevent overwhelming the system
/// </summary>
public class SwarmThrottling
{
    /// <summary>
    /// Maximum agents spawned per second (default: 2)
    /// </summary>
    public double MaxAgentsPerSecond { get; set; } = 2.0;
    
    /// <summary>
    /// Maximum agents spawned per minute (default: 20)
    /// </summary>
    public int MaxAgentsPerMinute { get; set; } = 20;
    
    /// <summary>
    /// Minimum time between agent spawns in milliseconds (default: 500ms)
    /// </summary>
    public int MinSpawnIntervalMs { get; set; } = 500;
    
    /// <summary>
    /// Enable throttling (default: true)
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Retry configuration for failed agent executions
/// </summary>
public class SwarmRetrySettings
{
    /// <summary>
    /// Maximum retry attempts per agent (default: 3)
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Initial retry delay in seconds (default: 30 seconds)
    /// </summary>
    public int InitialRetryDelaySeconds { get; set; } = 30;
    
    /// <summary>
    /// Retry delay multiplier for exponential backoff (default: 2.0)
    /// </summary>
    public double RetryDelayMultiplier { get; set; } = 2.0;
    
    /// <summary>
    /// Maximum retry delay in seconds (default: 300 seconds / 5 minutes)
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 300;
    
    /// <summary>
    /// Retry only on specific errors (empty means retry on all failures)
    /// </summary>
    public List<string> RetryOnlyOnErrors { get; set; } = new();
}

/// <summary>
/// Resource limits for a specific agent type
/// </summary>
public class AgentResourceLimits
{
    /// <summary>
    /// Maximum tokens per execution
    /// </summary>
    public int MaxTokens { get; set; } = 2000;
    
    /// <summary>
    /// Maximum cost per execution in USD
    /// </summary>
    public decimal MaxCostPerExecution { get; set; } = 2.00m;
    
    /// <summary>
    /// Timeout in minutes
    /// </summary>
    public int TimeoutMinutes { get; set; } = 5;
    
    /// <summary>
    /// Memory limit in MB (for future use with containerized agents)
    /// </summary>
    public int MaxMemoryMB { get; set; } = 512;
    
    /// <summary>
    /// CPU limit percentage (for future use with containerized agents)
    /// </summary>
    public int MaxCpuPercentage { get; set; } = 50;
}

/// <summary>
/// Priority settings for workload management
/// </summary>
public class SwarmPrioritySettings
{
    /// <summary>
    /// High priority agent types get preference in scheduling
    /// </summary>
    public List<string> HighPriorityAgentTypes { get; set; } = new() { "Designer", "APIDesigner" };
    
    /// <summary>
    /// Priority boost for functions with high complexity
    /// </summary>
    public int ComplexityPriorityBoost { get; set; } = 2;
    
    /// <summary>
    /// Priority boost for time-critical functions
    /// </summary>
    public int UrgentPriorityBoost { get; set; } = 5;
    
    /// <summary>
    /// Maximum priority level (default: 10)
    /// </summary>
    public int MaxPriority { get; set; } = 10;
    
    /// <summary>
    /// Default priority for new tasks (default: 5)
    /// </summary>
    public int DefaultPriority { get; set; } = 5;
}

/// <summary>
/// Health monitoring configuration
/// </summary>
public class SwarmHealthSettings
{
    /// <summary>
    /// Health check interval in seconds (default: 30 seconds)
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 30;
    
    /// <summary>
    /// Agent heartbeat timeout in seconds (default: 120 seconds)
    /// </summary>
    public int HeartbeatTimeoutSeconds { get; set; } = 120;
    
    /// <summary>
    /// Minimum success rate to consider swarm healthy (default: 80%)
    /// </summary>
    public double MinSuccessRatePercent { get; set; } = 80.0;
    
    /// <summary>
    /// Maximum average response time in seconds (default: 60 seconds)
    /// </summary>
    public int MaxAverageResponseTimeSeconds { get; set; } = 60;
    
    /// <summary>
    /// Maximum queue backlog before marking as unhealthy (default: 500)
    /// </summary>
    public int MaxQueueBacklog { get; set; } = 500;
    
    /// <summary>
    /// Enable automatic unhealthy agent termination (default: true)
    /// </summary>
    public bool EnableAutoTermination { get; set; } = true;
}

/// <summary>
/// Auto-scaling configuration for dynamic swarm size adjustment
/// </summary>
public class SwarmAutoScaling
{
    /// <summary>
    /// Enable auto-scaling (default: true)
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Minimum number of concurrent agents to maintain (default: 1)
    /// </summary>
    public int MinAgents { get; set; } = 1;
    
    /// <summary>
    /// Target queue depth for scaling decisions (default: 10)
    /// </summary>
    public int TargetQueueDepth { get; set; } = 10;
    
    /// <summary>
    /// Scale up when queue depth exceeds this threshold (default: 20)
    /// </summary>
    public int ScaleUpThreshold { get; set; } = 20;
    
    /// <summary>
    /// Scale down when queue depth is below this threshold (default: 5)
    /// </summary>
    public int ScaleDownThreshold { get; set; } = 5;
    
    /// <summary>
    /// How many agents to add when scaling up (default: 2)
    /// </summary>
    public int ScaleUpIncrement { get; set; } = 2;
    
    /// <summary>
    /// How many agents to remove when scaling down (default: 1)
    /// </summary>
    public int ScaleDownIncrement { get; set; } = 1;
    
    /// <summary>
    /// Minimum time between scaling decisions in seconds (default: 60 seconds)
    /// </summary>
    public int ScalingCooldownSeconds { get; set; } = 60;
    
    /// <summary>
    /// CPU utilization threshold for scaling up (default: 70%)
    /// </summary>
    public double CpuScaleUpThreshold { get; set; } = 70.0;
    
    /// <summary>
    /// CPU utilization threshold for scaling down (default: 30%)
    /// </summary>
    public double CpuScaleDownThreshold { get; set; } = 30.0;
}
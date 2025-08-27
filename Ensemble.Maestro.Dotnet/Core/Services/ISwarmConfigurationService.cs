using Ensemble.Maestro.Dotnet.Core.Configuration;

namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Service for managing and validating swarm configuration settings
/// </summary>
public interface ISwarmConfigurationService
{
    /// <summary>
    /// Get current swarm configuration with runtime validation
    /// </summary>
    SwarmConfiguration GetConfiguration();
    
    /// <summary>
    /// Update swarm configuration (runtime changes)
    /// </summary>
    Task<bool> UpdateConfigurationAsync(SwarmConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get resource limits for a specific agent type
    /// </summary>
    AgentResourceLimits GetResourceLimits(string agentType);
    
    /// <summary>
    /// Check if agent can be spawned based on current limits
    /// </summary>
    Task<SwarmCapacityCheck> CheckSpawnCapacityAsync(
        string agentType, 
        string projectId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get current swarm utilization metrics
    /// </summary>
    Task<SwarmUtilization> GetUtilizationAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Calculate priority for a task based on configuration and context
    /// </summary>
    int CalculateTaskPriority(
        string agentType, 
        int complexityRating, 
        string urgency = "Normal",
        Dictionary<string, object>? context = null);
    
    /// <summary>
    /// Get retry configuration for an agent type
    /// </summary>
    SwarmRetrySettings GetRetrySettings(string agentType);
    
    /// <summary>
    /// Check if throttling limits allow agent spawn
    /// </summary>
    Task<bool> CheckThrottlingLimitsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get auto-scaling recommendation based on current state
    /// </summary>
    Task<AutoScalingRecommendation> GetAutoScalingRecommendationAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate configuration for consistency and safety
    /// </summary>
    ConfigurationValidationResult ValidateConfiguration(SwarmConfiguration configuration);
}

/// <summary>
/// Result of checking whether an agent can be spawned
/// </summary>
public class SwarmCapacityCheck
{
    public bool CanSpawn { get; set; }
    public string? Reason { get; set; }
    public int AvailableSlots { get; set; }
    public int CurrentUtilization { get; set; }
    public int MaxCapacity { get; set; }
    public decimal RemainingBudget { get; set; }
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Current swarm utilization metrics
/// </summary>
public class SwarmUtilization
{
    public int TotalActiveAgents { get; set; }
    public int MaxConcurrentAgents { get; set; }
    public double UtilizationPercentage => MaxConcurrentAgents > 0 ? (double)TotalActiveAgents / MaxConcurrentAgents * 100 : 0;
    
    public Dictionary<string, int> AgentsByType { get; set; } = new();
    public Dictionary<string, int> AgentsByProject { get; set; } = new();
    
    public int QueueDepth { get; set; }
    public int ProcessingRate { get; set; } // agents per minute
    public decimal CurrentCostBurn { get; set; } // cost per hour
    
    public SwarmHealthMetrics Health { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Health metrics for the swarm
/// </summary>
public class SwarmHealthMetrics
{
    public double SuccessRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public int FailedAgentsLastHour { get; set; }
    public int TimeoutAgentsLastHour { get; set; }
    public bool IsHealthy { get; set; } = true;
    public List<string> HealthIssues { get; set; } = new();
}

/// <summary>
/// Auto-scaling recommendation
/// </summary>
public class AutoScalingRecommendation
{
    public ScalingAction Action { get; set; } = ScalingAction.NoAction;
    public int RecommendedChange { get; set; } // positive for scale up, negative for scale down
    public string Reason { get; set; } = string.Empty;
    public double Confidence { get; set; } // 0.0 to 1.0
    public TimeSpan EstimatedImpact { get; set; }
    public List<ScalingFactor> Factors { get; set; } = new();
}

/// <summary>
/// Scaling action types
/// </summary>
public enum ScalingAction
{
    NoAction,
    ScaleUp,
    ScaleDown,
    Emergency
}

/// <summary>
/// Factor contributing to scaling decision
/// </summary>
public class ScalingFactor
{
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; }
    public string Description { get; set; } = string.Empty;
    public ScalingAction Influence { get; set; }
}

/// <summary>
/// Configuration validation result
/// </summary>
public class ConfigurationValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}
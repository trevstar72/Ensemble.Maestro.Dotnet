using Ensemble.Maestro.Dotnet.Core.Configuration;
using Ensemble.Maestro.Dotnet.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Implementation of swarm configuration service with validation and monitoring
/// </summary>
public class SwarmConfigurationService : ISwarmConfigurationService
{
    private readonly SwarmConfiguration _configuration;
    private readonly MaestroDbContext _dbContext;
    private readonly ILogger<SwarmConfigurationService> _logger;
    private readonly Dictionary<string, DateTime> _lastSpawnTimes = new();
    private readonly object _throttleLock = new();

    public SwarmConfigurationService(
        IOptions<SwarmConfiguration> configuration,
        MaestroDbContext dbContext,
        ILogger<SwarmConfigurationService> logger)
    {
        _configuration = configuration.Value;
        _dbContext = dbContext;
        _logger = logger;
    }

    public SwarmConfiguration GetConfiguration()
    {
        return _configuration;
    }

    public async Task<bool> UpdateConfigurationAsync(
        SwarmConfiguration configuration, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validation = ValidateConfiguration(configuration);
            if (!validation.IsValid)
            {
                _logger.LogError("Configuration update failed validation: {Errors}", 
                    string.Join(", ", validation.Errors));
                return false;
            }

            // TODO: Implement runtime configuration updates
            // For now, log the request and return success for static configuration
            _logger.LogInformation("Configuration update requested (requires application restart for full effect)");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating swarm configuration");
            return false;
        }
    }

    public AgentResourceLimits GetResourceLimits(string agentType)
    {
        if (_configuration.ResourceLimits.TryGetValue(agentType, out var limits))
        {
            return limits;
        }

        // Return default limits for unknown agent types
        _logger.LogWarning("No resource limits configured for agent type {AgentType}, using defaults", agentType);
        return new AgentResourceLimits();
    }

    public async Task<SwarmCapacityCheck> CheckSpawnCapacityAsync(
        string agentType, 
        string projectId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = new SwarmCapacityCheck();

            // Check current agent counts
            var currentAgents = await _dbContext.AgentExecutions
                .Where(ae => ae.Status == "Running")
                .CountAsync(cancellationToken);

            var projectAgents = await _dbContext.AgentExecutions
                .Where(ae => ae.ProjectId.ToString() == projectId && ae.Status == "Running")
                .CountAsync(cancellationToken);

            // Check global concurrent limit
            if (currentAgents >= _configuration.MaxConcurrentAgents)
            {
                result.CanSpawn = false;
                result.Reason = $"Global concurrent agent limit reached ({currentAgents}/{_configuration.MaxConcurrentAgents})";
                result.CurrentUtilization = currentAgents;
                result.MaxCapacity = _configuration.MaxConcurrentAgents;
                return result;
            }

            // Check per-project limit
            if (projectAgents >= _configuration.MaxAgentsPerProject)
            {
                result.CanSpawn = false;
                result.Reason = $"Project agent limit reached ({projectAgents}/{_configuration.MaxAgentsPerProject})";
                return result;
            }

            // Check agent-type specific limits
            var agentTypeCount = await _dbContext.AgentExecutions
                .Where(ae => ae.AgentType == agentType && ae.Status == "Running")
                .CountAsync(cancellationToken);

            var agentTypeLimits = GetAgentTypeSpecificLimits(agentType);
            if (agentTypeCount >= agentTypeLimits.MaxConcurrent)
            {
                result.CanSpawn = false;
                result.Reason = $"{agentType} agent limit reached ({agentTypeCount}/{agentTypeLimits.MaxConcurrent})";
                return result;
            }

            // Check cost limits (simplified - in real implementation would track actual costs)
            result.RemainingBudget = _configuration.MaxCostPerProject; // Simplified

            // Success case
            result.CanSpawn = true;
            result.AvailableSlots = _configuration.MaxConcurrentAgents - currentAgents;
            result.CurrentUtilization = currentAgents;
            result.MaxCapacity = _configuration.MaxConcurrentAgents;

            // Add warnings for high utilization
            var utilizationPercent = (double)currentAgents / _configuration.MaxConcurrentAgents * 100;
            if (utilizationPercent > 80)
            {
                result.Warnings.Add($"High utilization: {utilizationPercent:F1}%");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking spawn capacity for {AgentType}", agentType);
            return new SwarmCapacityCheck 
            { 
                CanSpawn = false, 
                Reason = "Error checking capacity: " + ex.Message 
            };
        }
    }

    public async Task<SwarmUtilization> GetUtilizationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var utilization = new SwarmUtilization
            {
                MaxConcurrentAgents = _configuration.MaxConcurrentAgents
            };

            // Get current agent counts
            var activeAgents = await _dbContext.AgentExecutions
                .Where(ae => ae.Status == "Running")
                .GroupBy(ae => ae.AgentType)
                .Select(g => new { AgentType = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            utilization.TotalActiveAgents = activeAgents.Sum(a => a.Count);
            utilization.AgentsByType = activeAgents.ToDictionary(a => a.AgentType ?? "Unknown", a => a.Count);

            // Get agents by project
            var projectAgents = await _dbContext.AgentExecutions
                .Where(ae => ae.Status == "Running")
                .GroupBy(ae => ae.ProjectId)
                .Select(g => new { ProjectId = g.Key.ToString(), Count = g.Count() })
                .ToListAsync(cancellationToken);

            utilization.AgentsByProject = projectAgents.ToDictionary(p => p.ProjectId, p => p.Count);

            // Get queue depth (simplified - would need message queue integration)
            utilization.QueueDepth = 0; // TODO: Get from message queue service

            // Calculate processing rate (agents completed in last hour)
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var completedLastHour = await _dbContext.AgentExecutions
                .Where(ae => ae.CompletedAt >= oneHourAgo && ae.Status == "Completed")
                .CountAsync(cancellationToken);

            utilization.ProcessingRate = completedLastHour;

            // Calculate health metrics
            var totalLastHour = await _dbContext.AgentExecutions
                .Where(ae => ae.CompletedAt >= oneHourAgo && 
                           (ae.Status == "Completed" || ae.Status == "Failed"))
                .CountAsync(cancellationToken);

            var failedLastHour = await _dbContext.AgentExecutions
                .Where(ae => ae.CompletedAt >= oneHourAgo && ae.Status == "Failed")
                .CountAsync(cancellationToken);

            utilization.Health.SuccessRate = totalLastHour > 0 
                ? (double)(totalLastHour - failedLastHour) / totalLastHour * 100 
                : 100.0;

            utilization.Health.FailedAgentsLastHour = failedLastHour;
            utilization.Health.IsHealthy = utilization.Health.SuccessRate >= _configuration.Health.MinSuccessRatePercent;

            // Calculate average response time
            var avgDuration = await _dbContext.AgentExecutions
                .Where(ae => ae.CompletedAt >= oneHourAgo && ae.DurationSeconds.HasValue)
                .AverageAsync(ae => (double?)ae.DurationSeconds!.Value, cancellationToken);

            utilization.Health.AverageResponseTime = TimeSpan.FromSeconds(avgDuration ?? 0);

            return utilization;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting swarm utilization");
            return new SwarmUtilization();
        }
    }

    public int CalculateTaskPriority(
        string agentType, 
        int complexityRating, 
        string urgency = "Normal",
        Dictionary<string, object>? context = null)
    {
        var basePriority = _configuration.Priority.DefaultPriority;

        // Agent type priority boost
        if (_configuration.Priority.HighPriorityAgentTypes.Contains(agentType))
        {
            basePriority += 2;
        }

        // Complexity priority boost
        if (complexityRating >= 7)
        {
            basePriority += _configuration.Priority.ComplexityPriorityBoost;
        }

        // Urgency priority boost
        basePriority += urgency switch
        {
            "Critical" => _configuration.Priority.UrgentPriorityBoost + 2,
            "High" => _configuration.Priority.UrgentPriorityBoost,
            "Normal" => 0,
            "Low" => -2,
            _ => 0
        };

        // Context-based adjustments
        if (context != null)
        {
            if (context.TryGetValue("IsBlocking", out var blocking) && blocking is true)
            {
                basePriority += 3;
            }

            if (context.TryGetValue("HasDependents", out var dependents) && dependents is true)
            {
                basePriority += 1;
            }
        }

        // Clamp to valid range
        return Math.Clamp(basePriority, 1, _configuration.Priority.MaxPriority);
    }

    public SwarmRetrySettings GetRetrySettings(string agentType)
    {
        // For now, return global retry settings
        // Could be extended to have agent-type specific retry configurations
        return _configuration.Retry;
    }

    public async Task<bool> CheckThrottlingLimitsAsync(CancellationToken cancellationToken = default)
    {
        if (!_configuration.Throttling.Enabled)
        {
            return true;
        }

        lock (_throttleLock)
        {
            var now = DateTime.UtcNow;
            var minInterval = TimeSpan.FromMilliseconds(_configuration.Throttling.MinSpawnIntervalMs);

            // Check minimum interval since last spawn
            if (_lastSpawnTimes.Count > 0)
            {
                var lastSpawn = _lastSpawnTimes.Values.Max();
                if (now - lastSpawn < minInterval)
                {
                    return false;
                }
            }

            // Check spawns per second limit
            var oneSecondAgo = now.AddSeconds(-1);
            var spawnsLastSecond = _lastSpawnTimes.Count(kvp => kvp.Value > oneSecondAgo);
            if (spawnsLastSecond >= _configuration.Throttling.MaxAgentsPerSecond)
            {
                return false;
            }

            // Check spawns per minute limit
            var oneMinuteAgo = now.AddMinutes(-1);
            var spawnsLastMinute = _lastSpawnTimes.Count(kvp => kvp.Value > oneMinuteAgo);
            if (spawnsLastMinute >= _configuration.Throttling.MaxAgentsPerMinute)
            {
                return false;
            }

            // Record this spawn attempt
            var spawnId = Guid.NewGuid().ToString();
            _lastSpawnTimes[spawnId] = now;

            // Clean old entries
            var oldEntries = _lastSpawnTimes.Where(kvp => kvp.Value < oneMinuteAgo).ToList();
            foreach (var (key, _) in oldEntries)
            {
                _lastSpawnTimes.Remove(key);
            }

            return true;
        }
    }

    public async Task<AutoScalingRecommendation> GetAutoScalingRecommendationAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_configuration.AutoScaling.Enabled)
        {
            return new AutoScalingRecommendation 
            { 
                Action = ScalingAction.NoAction, 
                Reason = "Auto-scaling disabled",
                Confidence = 1.0
            };
        }

        try
        {
            var utilization = await GetUtilizationAsync(cancellationToken);
            var recommendation = new AutoScalingRecommendation();

            // Analyze queue depth
            var queueFactor = new ScalingFactor
            {
                Name = "QueueDepth",
                Description = $"Current queue depth: {utilization.QueueDepth}"
            };

            if (utilization.QueueDepth > _configuration.AutoScaling.ScaleUpThreshold)
            {
                recommendation.Action = ScalingAction.ScaleUp;
                recommendation.RecommendedChange = _configuration.AutoScaling.ScaleUpIncrement;
                recommendation.Reason = $"Queue depth ({utilization.QueueDepth}) exceeds scale-up threshold ({_configuration.AutoScaling.ScaleUpThreshold})";
                queueFactor.Weight = 0.7;
                queueFactor.Influence = ScalingAction.ScaleUp;
            }
            else if (utilization.QueueDepth < _configuration.AutoScaling.ScaleDownThreshold && 
                     utilization.TotalActiveAgents > _configuration.AutoScaling.MinAgents)
            {
                recommendation.Action = ScalingAction.ScaleDown;
                recommendation.RecommendedChange = -_configuration.AutoScaling.ScaleDownIncrement;
                recommendation.Reason = $"Queue depth ({utilization.QueueDepth}) below scale-down threshold ({_configuration.AutoScaling.ScaleDownThreshold})";
                queueFactor.Weight = 0.5;
                queueFactor.Influence = ScalingAction.ScaleDown;
            }

            recommendation.Factors.Add(queueFactor);

            // Analyze health metrics
            var healthFactor = new ScalingFactor
            {
                Name = "Health",
                Description = $"Success rate: {utilization.Health.SuccessRate:F1}%"
            };

            if (!utilization.Health.IsHealthy)
            {
                // Poor health might indicate need for more resources
                if (recommendation.Action == ScalingAction.NoAction)
                {
                    recommendation.Action = ScalingAction.ScaleUp;
                    recommendation.RecommendedChange = 1;
                    recommendation.Reason = $"Poor swarm health (success rate: {utilization.Health.SuccessRate:F1}%)";
                }
                healthFactor.Weight = 0.3;
                healthFactor.Influence = ScalingAction.ScaleUp;
            }

            recommendation.Factors.Add(healthFactor);

            // Calculate confidence based on data quality and consistency
            recommendation.Confidence = Math.Min(1.0, 
                0.5 + (recommendation.Factors.Sum(f => f.Weight) / recommendation.Factors.Count));

            return recommendation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating auto-scaling recommendation");
            return new AutoScalingRecommendation 
            { 
                Action = ScalingAction.NoAction, 
                Reason = "Error in analysis: " + ex.Message,
                Confidence = 0.0
            };
        }
    }

    public ConfigurationValidationResult ValidateConfiguration(SwarmConfiguration configuration)
    {
        var result = new ConfigurationValidationResult();

        // Validate basic limits
        if (configuration.MaxConcurrentAgents <= 0)
        {
            result.Errors.Add("MaxConcurrentAgents must be greater than 0");
            result.IsValid = false;
        }

        if (configuration.MaxAgentsPerProject <= 0)
        {
            result.Errors.Add("MaxAgentsPerProject must be greater than 0");
            result.IsValid = false;
        }

        if (configuration.MaxCostPerProject <= 0)
        {
            result.Errors.Add("MaxCostPerProject must be greater than 0");
            result.IsValid = false;
        }

        // Validate consistency
        if (configuration.MaxAgentsPerProject > configuration.MaxConcurrentAgents)
        {
            result.Warnings.Add("MaxAgentsPerProject is greater than MaxConcurrentAgents - this may prevent full project utilization");
        }

        // Validate throttling settings
        if (configuration.Throttling.MaxAgentsPerSecond <= 0)
        {
            result.Errors.Add("Throttling.MaxAgentsPerSecond must be greater than 0");
            result.IsValid = false;
        }

        if (configuration.Throttling.MaxAgentsPerMinute < configuration.Throttling.MaxAgentsPerSecond * 60)
        {
            result.Warnings.Add("Throttling limits may be inconsistent - MaxAgentsPerMinute should be >= MaxAgentsPerSecond * 60");
        }

        // Validate retry settings
        if (configuration.Retry.MaxRetryAttempts < 0)
        {
            result.Errors.Add("Retry.MaxRetryAttempts cannot be negative");
            result.IsValid = false;
        }

        // Validate auto-scaling settings
        if (configuration.AutoScaling.MinAgents > configuration.MaxConcurrentAgents)
        {
            result.Errors.Add("AutoScaling.MinAgents cannot be greater than MaxConcurrentAgents");
            result.IsValid = false;
        }

        // Validate resource limits
        foreach (var (agentType, limits) in configuration.ResourceLimits)
        {
            if (limits.MaxTokens <= 0)
            {
                result.Errors.Add($"ResourceLimits[{agentType}].MaxTokens must be greater than 0");
                result.IsValid = false;
            }

            if (limits.MaxCostPerExecution <= 0)
            {
                result.Errors.Add($"ResourceLimits[{agentType}].MaxCostPerExecution must be greater than 0");
                result.IsValid = false;
            }
        }

        // Add recommendations
        if (configuration.MaxConcurrentAgents < 5)
        {
            result.Recommendations.Add("Consider increasing MaxConcurrentAgents for better throughput");
        }

        if (configuration.Health.HealthCheckIntervalSeconds > 300)
        {
            result.Recommendations.Add("Consider decreasing health check interval for better monitoring");
        }

        return result;
    }

    #region Private Helper Methods

    private (int MaxConcurrent, int MaxPerController) GetAgentTypeSpecificLimits(string agentType)
    {
        return agentType switch
        {
            "CodeUnitController" => (_configuration.MaxControllers, 0),
            "MethodAgent" => (_configuration.MaxControllers * _configuration.MaxMethodAgentsPerController, 
                             _configuration.MaxMethodAgentsPerController),
            _ => (int.MaxValue, 0) // No specific limits for other agent types
        };
    }

    #endregion
}
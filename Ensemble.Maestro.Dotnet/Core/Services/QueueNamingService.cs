using Microsoft.Extensions.Logging;

namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Centralized queue naming service to ensure consistent queue names across all Redis operations
/// Prevents queue name mismatches between senders and consumers
/// </summary>
public interface IQueueNamingService
{
    /// <summary>
    /// Gets the full Redis key for a priority queue (sorted set)
    /// </summary>
    string GetPriorityQueueKey(string queueName);
    
    /// <summary>
    /// Gets the full Redis key for a regular queue (list)
    /// </summary>
    string GetRegularQueueKey(string queueName);
    
    /// <summary>
    /// Gets the full Redis key for queue configuration
    /// </summary>
    string GetQueueConfigKey(string queueName);
    
    /// <summary>
    /// Gets the full Redis key for queue statistics
    /// </summary>
    string GetQueueStatsKey(string queueName);
    
    /// <summary>
    /// Gets the base queue name without any prefixes or suffixes
    /// </summary>
    string GetBaseQueueName(string fullQueueKey);
    
    /// <summary>
    /// Validates that a queue name follows the required format
    /// </summary>
    bool IsValidQueueName(string queueName);
}

public class QueueNamingService : IQueueNamingService
{
    private readonly ILogger<QueueNamingService> _logger;
    
    // Centralized constants
    private const string QUEUE_PREFIX = "maestro:queue:";
    private const string CONFIG_PREFIX = "maestro:config:";
    private const string STATS_PREFIX = "maestro:stats:";
    
    // Queue type suffixes
    private const string PRIORITY_SUFFIX = ":priority";
    private const string REGULAR_SUFFIX = "";
    private const string CONFIG_SUFFIX = "";
    private const string STATS_SUFFIX = ":stats";

    public QueueNamingService(ILogger<QueueNamingService> logger)
    {
        _logger = logger;
    }

    public string GetPriorityQueueKey(string queueName)
    {
        if (!IsValidQueueName(queueName))
        {
            throw new ArgumentException($"Invalid queue name: {queueName}", nameof(queueName));
        }
        
        var fullKey = $"{QUEUE_PREFIX}{queueName}{PRIORITY_SUFFIX}";
        _logger.LogDebug("🔑 QueueNaming: Priority queue key - Base: '{QueueName}' → Full: '{FullKey}'", queueName, fullKey);
        return fullKey;
    }

    public string GetRegularQueueKey(string queueName)
    {
        if (!IsValidQueueName(queueName))
        {
            throw new ArgumentException($"Invalid queue name: {queueName}", nameof(queueName));
        }
        
        var fullKey = $"{QUEUE_PREFIX}{queueName}{REGULAR_SUFFIX}";
        _logger.LogDebug("🔑 QueueNaming: Regular queue key - Base: '{QueueName}' → Full: '{FullKey}'", queueName, fullKey);
        return fullKey;
    }

    public string GetQueueConfigKey(string queueName)
    {
        if (!IsValidQueueName(queueName))
        {
            throw new ArgumentException($"Invalid queue name: {queueName}", nameof(queueName));
        }
        
        var fullKey = $"{CONFIG_PREFIX}queue:{queueName}{CONFIG_SUFFIX}";
        _logger.LogDebug("🔑 QueueNaming: Config key - Base: '{QueueName}' → Full: '{FullKey}'", queueName, fullKey);
        return fullKey;
    }

    public string GetQueueStatsKey(string queueName)
    {
        if (!IsValidQueueName(queueName))
        {
            throw new ArgumentException($"Invalid queue name: {queueName}", nameof(queueName));
        }
        
        var fullKey = $"{STATS_PREFIX}{queueName}{STATS_SUFFIX}";
        _logger.LogDebug("🔑 QueueNaming: Stats key - Base: '{QueueName}' → Full: '{FullKey}'", queueName, fullKey);
        return fullKey;
    }

    public string GetBaseQueueName(string fullQueueKey)
    {
        if (string.IsNullOrWhiteSpace(fullQueueKey))
        {
            throw new ArgumentException("Full queue key cannot be null or empty", nameof(fullQueueKey));
        }

        // Remove known prefixes
        var baseName = fullQueueKey;
        
        if (baseName.StartsWith(QUEUE_PREFIX))
        {
            baseName = baseName.Substring(QUEUE_PREFIX.Length);
        }
        else if (baseName.StartsWith(CONFIG_PREFIX))
        {
            baseName = baseName.Substring(CONFIG_PREFIX.Length);
            if (baseName.StartsWith("queue:"))
            {
                baseName = baseName.Substring("queue:".Length);
            }
        }
        else if (baseName.StartsWith(STATS_PREFIX))
        {
            baseName = baseName.Substring(STATS_PREFIX.Length);
        }

        // Remove known suffixes
        if (baseName.EndsWith(PRIORITY_SUFFIX))
        {
            baseName = baseName.Substring(0, baseName.Length - PRIORITY_SUFFIX.Length);
        }
        else if (baseName.EndsWith(STATS_SUFFIX))
        {
            baseName = baseName.Substring(0, baseName.Length - STATS_SUFFIX.Length);
        }

        _logger.LogDebug("🔑 QueueNaming: Base name extraction - Full: '{FullKey}' → Base: '{BaseName}'", fullQueueKey, baseName);
        return baseName;
    }

    public bool IsValidQueueName(string queueName)
    {
        if (string.IsNullOrWhiteSpace(queueName))
        {
            _logger.LogWarning("🚨 QueueNaming: Invalid queue name - null or whitespace");
            return false;
        }

        // Queue names should not contain the prefixes (those are added by this service)
        if (queueName.Contains(QUEUE_PREFIX) || queueName.Contains(CONFIG_PREFIX) || queueName.Contains(STATS_PREFIX))
        {
            _logger.LogWarning("🚨 QueueNaming: Invalid queue name - contains reserved prefixes: '{QueueName}'", queueName);
            return false;
        }

        // Queue names should not contain certain suffixes (those are added by this service)
        if (queueName.EndsWith(PRIORITY_SUFFIX) || queueName.EndsWith(STATS_SUFFIX))
        {
            _logger.LogWarning("🚨 QueueNaming: Invalid queue name - contains reserved suffixes: '{QueueName}'", queueName);
            return false;
        }

        // Basic validation - alphanumeric, dots, hyphens, underscores
        if (!System.Text.RegularExpressions.Regex.IsMatch(queueName, @"^[a-zA-Z0-9._-]+$"))
        {
            _logger.LogWarning("🚨 QueueNaming: Invalid queue name - contains invalid characters: '{QueueName}'", queueName);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets diagnostic information about all queue naming patterns
    /// </summary>
    public QueueNamingDiagnostics GetDiagnostics()
    {
        return new QueueNamingDiagnostics
        {
            QueuePrefix = QUEUE_PREFIX,
            ConfigPrefix = CONFIG_PREFIX,
            StatsPrefix = STATS_PREFIX,
            PrioritySuffix = PRIORITY_SUFFIX,
            RegularSuffix = REGULAR_SUFFIX,
            ConfigSuffix = CONFIG_SUFFIX,
            StatsSuffix = STATS_SUFFIX
        };
    }
}

public class QueueNamingDiagnostics
{
    public string QueuePrefix { get; set; } = string.Empty;
    public string ConfigPrefix { get; set; } = string.Empty;
    public string StatsPrefix { get; set; } = string.Empty;
    public string PrioritySuffix { get; set; } = string.Empty;
    public string RegularSuffix { get; set; } = string.Empty;
    public string ConfigSuffix { get; set; } = string.Empty;
    public string StatsSuffix { get; set; } = string.Empty;
}
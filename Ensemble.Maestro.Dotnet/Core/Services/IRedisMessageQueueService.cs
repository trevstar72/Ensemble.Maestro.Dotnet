namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Redis-based message queue service for coordinating agent communications with size limits
/// </summary>
public interface IRedisMessageQueueService
{
    /// <summary>
    /// Send a message to a specific queue with automatic size validation
    /// </summary>
    Task<MessageQueueResult> SendMessageAsync(
        string queueName, 
        object message, 
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send a message with priority (higher priority = processed first)
    /// </summary>
    Task<MessageQueueResult> SendPriorityMessageAsync(
        string queueName, 
        object message, 
        int priority = 5,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Receive the next message from a queue
    /// </summary>
    Task<MessageQueueItem<T>?> ReceiveMessageAsync<T>(
        string queueName, 
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Receive messages with blocking wait until available
    /// </summary>
    Task<MessageQueueItem<T>?> ReceiveBlockingMessageAsync<T>(
        string queueName, 
        TimeSpan timeout,
        CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Acknowledge message processing completion
    /// </summary>
    Task<bool> AcknowledgeMessageAsync(
        string queueName, 
        string messageId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Mark message as failed and optionally retry
    /// </summary>
    Task<bool> RejectMessageAsync(
        string queueName, 
        string messageId, 
        bool requeue = false,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get queue statistics
    /// </summary>
    Task<QueueStatistics> GetQueueStatsAsync(
        string queueName, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publish message to multiple subscribers (pub/sub pattern)
    /// </summary>
    Task<MessageQueueResult> PublishAsync(
        string channel, 
        object message, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Subscribe to a channel for pub/sub messages
    /// </summary>
    Task<IAsyncEnumerable<MessageQueueItem<T>>> SubscribeAsync<T>(
        string channel, 
        CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Create a new queue with specific configuration
    /// </summary>
    Task<bool> CreateQueueAsync(
        string queueName, 
        QueueConfiguration? configuration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a queue and all its messages
    /// </summary>
    Task<bool> DeleteQueueAsync(
        string queueName, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all available queue names
    /// </summary>
    Task<List<string>> GetQueueNamesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clear all messages from a queue
    /// </summary>
    Task<long> ClearQueueAsync(
        string queueName, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of message queue operation
/// </summary>
public class MessageQueueResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public int MessageSizeBytes { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool WasTruncated { get; set; }
    public string? OriginalSizeInfo { get; set; }
}

/// <summary>
/// Message queue item with metadata
/// </summary>
public class MessageQueueItem<T> where T : class
{
    public string Id { get; set; } = string.Empty;
    public T Data { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int Priority { get; set; } = 5;
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public string QueueName { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}

/// <summary>
/// Queue configuration options
/// </summary>
public class QueueConfiguration
{
    /// <summary>
    /// Maximum message size in bytes (default: 2048)
    /// </summary>
    public int MaxMessageSizeBytes { get; set; } = 2048;
    
    /// <summary>
    /// Maximum number of messages in queue (default: 10000)
    /// </summary>
    public int MaxQueueSize { get; set; } = 10000;
    
    /// <summary>
    /// Default message expiration time (default: 1 hour)
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(1);
    
    /// <summary>
    /// Enable message persistence (default: true)
    /// </summary>
    public bool EnablePersistence { get; set; } = true;
    
    /// <summary>
    /// Enable priority queue (default: false)
    /// </summary>
    public bool EnablePriority { get; set; } = false;
    
    /// <summary>
    /// Maximum retry count for failed messages (default: 3)
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Dead letter queue name for failed messages
    /// </summary>
    public string? DeadLetterQueue { get; set; }
}

/// <summary>
/// Queue statistics and health information
/// </summary>
public class QueueStatistics
{
    public string QueueName { get; set; } = string.Empty;
    public long MessageCount { get; set; }
    public long ProcessedMessages { get; set; }
    public long FailedMessages { get; set; }
    public DateTime LastActivity { get; set; }
    public TimeSpan AverageProcessingTime { get; set; }
    public int ActiveConsumers { get; set; }
    public long TotalMessagesSent { get; set; }
    public long TotalMessagesReceived { get; set; }
    public double MessagesPerSecond { get; set; }
    public bool IsHealthy { get; set; } = true;
    public List<string> HealthIssues { get; set; } = new();
}
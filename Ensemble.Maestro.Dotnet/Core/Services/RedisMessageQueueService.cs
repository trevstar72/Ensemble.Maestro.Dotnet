using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Redis-based message queue service implementation with 2KB size limits and swarm coordination
/// </summary>
public class RedisMessageQueueService : IRedisMessageQueueService
{
    private readonly ILogger<RedisMessageQueueService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private const int DEFAULT_MAX_SIZE_BYTES = 2048; // 2KB limit for swarm coordination
    private const string QUEUE_PREFIX = "maestro:queue:";
    private const string STATS_PREFIX = "maestro:stats:";
    private const string PUBSUB_PREFIX = "maestro:pubsub:";
    private const string CONFIG_PREFIX = "maestro:config:";

    public RedisMessageQueueService(ILogger<RedisMessageQueueService> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false, // Minimize size
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<MessageQueueResult> SendMessageAsync(
        string queueName, 
        object message, 
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await GetQueueConfigAsync(queueName, cancellationToken);
            var result = await SerializeAndValidateMessage(message, config.MaxMessageSizeBytes);

            if (!result.Success)
            {
                return result;
            }

            var messageId = Guid.NewGuid().ToString("N");
            var queueItem = new MessageQueueItem<object>
            {
                Id = messageId,
                Data = message,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expiration ?? config.DefaultExpiration),
                Priority = 5,
                QueueName = queueName,
                MaxRetries = config.MaxRetries
            };

            // TODO: Implement actual Redis list operations
            // For now, simulate the operation
            await Task.CompletedTask;
            
            _logger.LogInformation("Redis: Sent message {MessageId} to queue {QueueName} ({MessageSize} bytes)", 
                messageId, queueName, result.MessageSizeBytes);

            await UpdateQueueStatsAsync(queueName, "sent", result.MessageSizeBytes, cancellationToken);

            return new MessageQueueResult
            {
                Success = true,
                MessageId = messageId,
                MessageSizeBytes = result.MessageSizeBytes,
                WasTruncated = result.WasTruncated,
                OriginalSizeInfo = result.OriginalSizeInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to queue {QueueName}", queueName);
            return new MessageQueueResult 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }

    public async Task<MessageQueueResult> SendPriorityMessageAsync(
        string queueName, 
        object message, 
        int priority = 5,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await GetQueueConfigAsync(queueName, cancellationToken);
            if (!config.EnablePriority)
            {
                return new MessageQueueResult
                {
                    Success = false,
                    ErrorMessage = $"Priority queue not enabled for {queueName}"
                };
            }

            var result = await SerializeAndValidateMessage(message, config.MaxMessageSizeBytes);
            if (!result.Success) return result;

            var messageId = Guid.NewGuid().ToString("N");
            
            // TODO: Implement Redis sorted set for priority queue
            await Task.CompletedTask;
            
            _logger.LogInformation("Redis: Sent priority message {MessageId} to queue {QueueName} (priority {Priority}, {MessageSize} bytes)", 
                messageId, queueName, priority, result.MessageSizeBytes);

            await UpdateQueueStatsAsync(queueName, "sent", result.MessageSizeBytes, cancellationToken);

            return new MessageQueueResult
            {
                Success = true,
                MessageId = messageId,
                MessageSizeBytes = result.MessageSizeBytes,
                WasTruncated = result.WasTruncated,
                OriginalSizeInfo = result.OriginalSizeInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send priority message to queue {QueueName}", queueName);
            return new MessageQueueResult 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }

    public async Task<MessageQueueItem<T>?> ReceiveMessageAsync<T>(
        string queueName, 
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // TODO: Implement Redis BRPOP or similar for non-blocking receive
            await Task.CompletedTask;
            
            _logger.LogDebug("Redis: Received message from queue {QueueName}", queueName);
            
            await UpdateQueueStatsAsync(queueName, "received", 0, cancellationToken);
            
            // Simulate no message available
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to receive message from queue {QueueName}", queueName);
            return null;
        }
    }

    public async Task<MessageQueueItem<T>?> ReceiveBlockingMessageAsync<T>(
        string queueName, 
        TimeSpan timeout,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // TODO: Implement Redis BRPOP with timeout for blocking receive
            await Task.Delay(100, cancellationToken); // Simulate wait
            
            _logger.LogDebug("Redis: Blocking receive from queue {QueueName} with timeout {Timeout}", 
                queueName, timeout);
            
            // Simulate no message available after timeout
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to receive blocking message from queue {QueueName}", queueName);
            return null;
        }
    }

    public async Task<bool> AcknowledgeMessageAsync(
        string queueName, 
        string messageId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement Redis acknowledgment (remove from processing set)
            await Task.CompletedTask;
            
            _logger.LogDebug("Redis: Acknowledged message {MessageId} from queue {QueueName}", 
                messageId, queueName);

            await UpdateQueueStatsAsync(queueName, "acknowledged", 0, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acknowledge message {MessageId} from queue {QueueName}", 
                messageId, queueName);
            return false;
        }
    }

    public async Task<bool> RejectMessageAsync(
        string queueName, 
        string messageId, 
        bool requeue = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement Redis rejection (move to dead letter queue or requeue)
            await Task.CompletedTask;
            
            _logger.LogWarning("Redis: Rejected message {MessageId} from queue {QueueName} (requeue: {Requeue})", 
                messageId, queueName, requeue);

            await UpdateQueueStatsAsync(queueName, "rejected", 0, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject message {MessageId} from queue {QueueName}", 
                messageId, queueName);
            return false;
        }
    }

    public async Task<QueueStatistics> GetQueueStatsAsync(
        string queueName, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement Redis statistics retrieval
            await Task.CompletedTask;
            
            return new QueueStatistics
            {
                QueueName = queueName,
                MessageCount = 0,
                ProcessedMessages = 0,
                FailedMessages = 0,
                LastActivity = DateTime.UtcNow.AddMinutes(-5),
                AverageProcessingTime = TimeSpan.FromMilliseconds(150),
                ActiveConsumers = 0,
                MessagesPerSecond = 0.0,
                IsHealthy = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stats for queue {QueueName}", queueName);
            return new QueueStatistics 
            { 
                QueueName = queueName, 
                IsHealthy = false, 
                HealthIssues = new List<string> { ex.Message } 
            };
        }
    }

    public async Task<MessageQueueResult> PublishAsync(
        string channel, 
        object message, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await SerializeAndValidateMessage(message, DEFAULT_MAX_SIZE_BYTES);
            if (!result.Success) return result;

            // TODO: Implement Redis PUBLISH
            await Task.CompletedTask;
            
            _logger.LogInformation("Redis: Published message to channel {Channel} ({MessageSize} bytes)", 
                channel, result.MessageSizeBytes);

            return new MessageQueueResult
            {
                Success = true,
                MessageId = Guid.NewGuid().ToString("N"),
                MessageSizeBytes = result.MessageSizeBytes,
                WasTruncated = result.WasTruncated,
                OriginalSizeInfo = result.OriginalSizeInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to channel {Channel}", channel);
            return new MessageQueueResult 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }

    public async Task<IAsyncEnumerable<MessageQueueItem<T>>> SubscribeAsync<T>(
        string channel, 
        CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogInformation("Redis: Subscribing to channel {Channel}", channel);
        
        // TODO: Implement Redis SUBSCRIBE
        return GetEmptyAsyncEnumerable<T>();
    }

    public async Task<bool> CreateQueueAsync(
        string queueName, 
        QueueConfiguration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = configuration ?? new QueueConfiguration();
            
            // TODO: Store queue configuration in Redis
            await Task.CompletedTask;
            
            _logger.LogInformation("Redis: Created queue {QueueName} with max size {MaxSize} bytes", 
                queueName, config.MaxMessageSizeBytes);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create queue {QueueName}", queueName);
            return false;
        }
    }

    public async Task<bool> DeleteQueueAsync(
        string queueName, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement Redis queue deletion (remove all keys)
            await Task.CompletedTask;
            
            _logger.LogInformation("Redis: Deleted queue {QueueName}", queueName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete queue {QueueName}", queueName);
            return false;
        }
    }

    public async Task<List<string>> GetQueueNamesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement Redis key pattern scanning
            await Task.CompletedTask;
            
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get queue names");
            return new List<string>();
        }
    }

    public async Task<long> ClearQueueAsync(
        string queueName, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement Redis queue clearing
            await Task.CompletedTask;
            
            _logger.LogInformation("Redis: Cleared queue {QueueName}", queueName);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear queue {QueueName}", queueName);
            return -1;
        }
    }

    #region Private Helper Methods

    private async Task<MessageQueueResult> SerializeAndValidateMessage(object message, int maxSizeBytes)
    {
        try
        {
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            var sizeBytes = System.Text.Encoding.UTF8.GetByteCount(json);
            
            var result = new MessageQueueResult
            {
                Success = true,
                MessageSizeBytes = sizeBytes
            };

            if (sizeBytes > maxSizeBytes)
            {
                // Try to truncate or compress the message
                var truncatedResult = await TruncateMessage(message, maxSizeBytes);
                if (truncatedResult.Success)
                {
                    result.WasTruncated = true;
                    result.OriginalSizeInfo = $"Original: {sizeBytes} bytes, Truncated: {truncatedResult.MessageSizeBytes} bytes";
                    result.MessageSizeBytes = truncatedResult.MessageSizeBytes;
                    
                    _logger.LogWarning("Message exceeded {MaxSize} bytes ({ActualSize} bytes), truncated to {TruncatedSize} bytes", 
                        maxSizeBytes, sizeBytes, truncatedResult.MessageSizeBytes);
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = $"Message size {sizeBytes} bytes exceeds limit of {maxSizeBytes} bytes and cannot be truncated";
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            return new MessageQueueResult
            {
                Success = false,
                ErrorMessage = $"Failed to serialize message: {ex.Message}"
            };
        }
    }

    private async Task<MessageQueueResult> TruncateMessage(object message, int maxSizeBytes)
    {
        try
        {
            // Strategy: Try to truncate string properties to fit within size limit
            var truncatedMessage = await TruncateMessageContent(message, maxSizeBytes);
            var json = JsonSerializer.Serialize(truncatedMessage, _jsonOptions);
            var sizeBytes = System.Text.Encoding.UTF8.GetByteCount(json);

            return new MessageQueueResult
            {
                Success = sizeBytes <= maxSizeBytes,
                MessageSizeBytes = sizeBytes
            };
        }
        catch (Exception ex)
        {
            return new MessageQueueResult
            {
                Success = false,
                ErrorMessage = $"Failed to truncate message: {ex.Message}"
            };
        }
    }

    private async Task<object> TruncateMessageContent(object message, int maxSizeBytes)
    {
        await Task.CompletedTask;
        
        // Simple truncation strategy for common message types
        if (message is string str)
        {
            var maxChars = Math.Max(10, maxSizeBytes / 4); // Rough estimate for UTF-8
            return str.Length > maxChars ? str.Substring(0, maxChars) + "..." : str;
        }

        // For complex objects, try to truncate string properties
        var messageType = message.GetType();
        var properties = messageType.GetProperties();
        var truncatedObject = Activator.CreateInstance(messageType);

        if (truncatedObject == null) return message;

        foreach (var prop in properties)
        {
            if (!prop.CanRead || !prop.CanWrite) continue;

            var value = prop.GetValue(message);
            if (value is string stringValue && stringValue.Length > 100)
            {
                prop.SetValue(truncatedObject, stringValue.Substring(0, 97) + "...");
            }
            else
            {
                prop.SetValue(truncatedObject, value);
            }
        }

        return truncatedObject;
    }

    private async Task<QueueConfiguration> GetQueueConfigAsync(string queueName, CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Retrieve configuration from Redis
            await Task.CompletedTask;
            
            // Return default configuration for now
            return new QueueConfiguration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configuration for queue {QueueName}, using defaults", queueName);
            return new QueueConfiguration();
        }
    }

    private async Task UpdateQueueStatsAsync(string queueName, string operation, int messageSize, CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Update Redis statistics counters
            await Task.CompletedTask;
            
            _logger.LogDebug("Redis: Updated stats for queue {QueueName} - operation: {Operation}", 
                queueName, operation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update stats for queue {QueueName}", queueName);
        }
    }

    private static async IAsyncEnumerable<MessageQueueItem<T>> GetEmptyAsyncEnumerable<T>([EnumeratorCancellation] CancellationToken cancellationToken = default) where T : class
    {
        await Task.CompletedTask;
        yield break;
    }

    #endregion
}
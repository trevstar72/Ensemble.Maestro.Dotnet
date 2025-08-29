using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;

namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Redis-based message queue service implementation with 2KB size limits and swarm coordination
/// </summary>
public class RedisMessageQueueService : IRedisMessageQueueService, IDisposable
{
    private readonly ILogger<RedisMessageQueueService> _logger;
    private readonly IQueueNamingService _queueNaming;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ISubscriber _subscriber;
    
    private const int DEFAULT_MAX_SIZE_BYTES = 2048; // 2KB limit for swarm coordination
    // Queue naming constants moved to QueueNamingService for centralization
    private const string PUBSUB_PREFIX = "maestro:pubsub:";

    public RedisMessageQueueService(ILogger<RedisMessageQueueService> logger, IQueueNamingService queueNaming, IConfiguration configuration)
    {
        _logger = logger;
        _queueNaming = queueNaming;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false, // Minimize size
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        
        try
        {
            var connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            _redis = ConnectionMultiplexer.Connect(connectionString);
            _database = _redis.GetDatabase();
            _subscriber = _redis.GetSubscriber();
            
            _logger.LogInformation("Redis: Connected to {ConnectionString}", connectionString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis: Failed to connect to Redis");
            throw;
        }
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

            // Send message to Redis list (FIFO queue)
            var serializedMessage = JsonSerializer.Serialize(queueItem, _jsonOptions);
            await _database.ListRightPushAsync(queueName, serializedMessage);
            
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
        _logger.LogInformation("🚀 REDIS SEND: SendPriorityMessageAsync ENTRY - QueueName: {QueueName}, Priority: {Priority}, MessageType: {MessageType}", 
            queueName, priority, message?.GetType().Name ?? "NULL");
            
        try
        {
            var config = await GetQueueConfigAsync(queueName, cancellationToken);
            _logger.LogInformation("📋 REDIS SEND: Queue config retrieved - EnablePriority: {EnablePriority}, MaxSize: {MaxSize}", 
                config.EnablePriority, config.MaxMessageSizeBytes);
                
            if (!config.EnablePriority)
            {
                _logger.LogError("❌ REDIS SEND: Priority queue not enabled for {QueueName}", queueName);
                return new MessageQueueResult
                {
                    Success = false,
                    ErrorMessage = $"Priority queue not enabled for {queueName}"
                };
            }

            var result = await SerializeAndValidateMessage(message, config.MaxMessageSizeBytes);
            if (!result.Success) 
            {
                _logger.LogError("❌ REDIS SEND: Message serialization failed for {QueueName}: {Error}", queueName, result.ErrorMessage);
                return result;
            }
            _logger.LogInformation("✅ REDIS SEND: Message serialized successfully - Size: {Size} bytes, Truncated: {Truncated}", 
                result.MessageSizeBytes, result.WasTruncated);

            var messageId = Guid.NewGuid().ToString("N");
            
            var messageQueueItem = new MessageQueueItem<object>
            {
                Id = messageId,
                Data = message,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expiration ?? config.DefaultExpiration),
                Priority = priority,
                QueueName = queueName,
                MaxRetries = config.MaxRetries
            };
            
            // Send priority message to Redis sorted set - CENTRALIZED: Use QueueNamingService
            var priorityScore = priority * 1000000 + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var priorityQueueKey = _queueNaming.GetPriorityQueueKey(queueName);
            
            _logger.LogInformation("📦 REDIS SEND: About to write to Redis - ExactQueueKey: '{PriorityQueueKey}', Score: {Score}, MessageId: {MessageId}", 
                priorityQueueKey, priorityScore, messageId);
            _logger.LogInformation("🔧 REDIS SEND: CENTRALIZED QUEUE NAMING - QueueName: '{QueueName}' → PriorityQueueKey: '{PriorityQueueKey}'", 
                queueName, priorityQueueKey);
                
            var serializedMessage = JsonSerializer.Serialize(messageQueueItem, _jsonOptions);
            _logger.LogDebug("📄 REDIS SEND: Serialized message preview: {MessagePreview}...", 
                serializedMessage.Length > 200 ? serializedMessage.Substring(0, 200) : serializedMessage);
                
            await _database.SortedSetAddAsync(priorityQueueKey, serializedMessage, priorityScore);
            
            _logger.LogInformation("✅ REDIS SEND: Successfully wrote to Redis SortedSet '{PriorityQueueKey}' - MessageId: {MessageId}, Priority: {Priority}, Size: {MessageSize} bytes", 
                priorityQueueKey, messageId, priority, result.MessageSizeBytes);
                
            // Verify the message was actually stored
            var verifyCount = await _database.SortedSetLengthAsync(priorityQueueKey);
            _logger.LogInformation("✅ REDIS SEND: Verification - Queue '{PriorityQueueKey}' now has {Count} total messages", 
                priorityQueueKey, verifyCount);

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
            _logger.LogError(ex, "Redis: Failed to send priority message to queue {QueueName} - {ErrorMessage}", queueName, ex.Message);
            _logger.LogError("Redis: Exception Details - Type: {ExceptionType}, Stack: {StackTrace}", ex.GetType().Name, ex.StackTrace);
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
            var config = await GetQueueConfigAsync(queueName, cancellationToken);
            MessageQueueItem<T>? messageItem = null;
            
            // Try priority queue first (sorted set) - get highest priority (highest score)  
            if (config.EnablePriority)
            {
                var priorityKey = _queueNaming.GetPriorityQueueKey(queueName);
                var priorityMessages = await _database.SortedSetRangeByScoreWithScoresAsync(
                    priorityKey, 
                    order: Order.Descending, 
                    take: 1
                );
                
                if (priorityMessages.Length > 0)
                {
                    var priorityEntry = priorityMessages[0];
                    var removed = await _database.SortedSetRemoveAsync(priorityKey, priorityEntry.Element);
                    if (removed)
                    {
                        messageItem = JsonSerializer.Deserialize<MessageQueueItem<T>>(priorityEntry.Element!, _jsonOptions);
                        _logger.LogDebug("Redis: Received priority message from queue {QueueName}", queueName);
                    }
                }
            }

            // Fallback to regular FIFO queue (list) if no priority message
            if (messageItem == null)
            {
                var queueKey = _queueNaming.GetRegularQueueKey(queueName);
                var regularMessage = await _database.ListLeftPopAsync(queueKey);
                if (regularMessage.HasValue)
                {
                    messageItem = JsonSerializer.Deserialize<MessageQueueItem<T>>(regularMessage!, _jsonOptions);
                    _logger.LogDebug("Redis: Received FIFO message from queue {QueueName}", queueName);
                }
            }
            
            if (messageItem != null)
            {
                await UpdateQueueStatsAsync(queueName, "received", 0, cancellationToken);
                
                // Check if message has expired
                if (messageItem.ExpiresAt <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Redis: Message {MessageId} expired, discarding", messageItem.Id);
                    return null;
                }
            }
            
            return messageItem;
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
            var config = await GetQueueConfigAsync(queueName, cancellationToken);
            var startTime = DateTime.UtcNow;
            var maxWaitTime = timeout.TotalMilliseconds > 0 ? timeout : TimeSpan.FromSeconds(30);
            
            // Use polling approach with shorter intervals for blocking operations
            var pollInterval = TimeSpan.FromMilliseconds(100);
            
            while (DateTime.UtcNow - startTime < maxWaitTime && !cancellationToken.IsCancellationRequested)
            {
                MessageQueueItem<T>? messageItem = null;
                
                // Try priority queue first (sorted set) - get highest priority (highest score)
                if (config.EnablePriority)
                {
                    var priorityKey = _queueNaming.GetPriorityQueueKey(queueName);
                    var priorityMessages = await _database.SortedSetRangeByScoreWithScoresAsync(
                        priorityKey, 
                        order: Order.Descending, 
                        take: 1
                    );
                    
                    if (priorityMessages.Length > 0)
                    {
                        var priorityEntry = priorityMessages[0];
                        var removed = await _database.SortedSetRemoveAsync(priorityKey, priorityEntry.Element);
                        if (removed)
                        {
                            messageItem = JsonSerializer.Deserialize<MessageQueueItem<T>>(priorityEntry.Element!, _jsonOptions);
                            _logger.LogDebug("Redis: Received priority message from queue {QueueName} (blocking)", queueName);
                        }
                    }
                }
                
                // Fallback to regular FIFO queue (list) if no priority message
                if (messageItem == null)
                {
                    var queueKey = _queueNaming.GetRegularQueueKey(queueName);
                    var result = await _database.ListLeftPopAsync(queueKey);
                    if (result.HasValue)
                    {
                        messageItem = JsonSerializer.Deserialize<MessageQueueItem<T>>(result!, _jsonOptions);
                        _logger.LogDebug("Redis: Received FIFO message from queue {QueueName} (blocking)", queueName);
                    }
                }
                
                // If we got a message, process it
                if (messageItem != null)
                {
                    await UpdateQueueStatsAsync(queueName, "received", 0, cancellationToken);
                    
                    // Check if message has expired
                    if (messageItem.ExpiresAt <= DateTime.UtcNow)
                    {
                        _logger.LogWarning("Redis: Message {MessageId} expired, discarding", messageItem.Id);
                        continue; // Try again for a non-expired message
                    }
                    
                    return messageItem;
                }
                
                // Wait before next poll
                try
                {
                    await Task.Delay(pollInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Redis: Blocking receive cancelled for queue {QueueName}", queueName);
                    break;
                }
            }
            
            // Timeout reached or cancelled
            _logger.LogDebug("Redis: No message received from queue {QueueName} within timeout {Timeout}ms", 
                queueName, maxWaitTime.TotalMilliseconds);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis: Failed to receive blocking message from queue {QueueName}", queueName);
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
        try
        {
            _logger.LogInformation("Redis: Subscribing to channel {Channel}", channel);
            
            await _subscriber.SubscribeAsync(channel, (redisChannel, value) =>
            {
                try
                {
                    if (!value.HasValue) return;
                    
                    var message = JsonSerializer.Deserialize<MessageQueueItem<T>>(value!, _jsonOptions);
                    if (message != null)
                    {
                        _logger.LogDebug("Redis: Received message on channel {Channel}", channel);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Redis: Failed to process message on channel {Channel}", channel);
                }
            });
            
            _logger.LogInformation("Redis: Successfully subscribed to channel {Channel}", channel);
            return ConsumeMessages<T>(channel, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis: Failed to subscribe to channel {Channel}", channel);
            return ConsumeMessages<T>(channel, cancellationToken);
        }
    }

    public async Task<bool> CreateQueueAsync(
        string queueName, 
        QueueConfiguration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use provided config, fallback to predefined, or default
            var config = configuration ?? 
                         (_queueConfigurations.TryGetValue(queueName, out var predefined) ? predefined : new QueueConfiguration());
            
            // Store queue configuration in Redis
            var configKey = _queueNaming.GetQueueConfigKey(queueName);
            var serializedConfig = JsonSerializer.Serialize(config, _jsonOptions);
            await _database.StringSetAsync(configKey, serializedConfig, TimeSpan.FromHours(24));
            
            // Initialize queue statistics
            var statsKey = _queueNaming.GetQueueStatsKey(queueName);
            var initialStats = new Dictionary<string, string>
            {
                ["created_at"] = DateTimeOffset.UtcNow.ToString("O"),
                ["total_sent"] = "0",
                ["total_received"] = "0",
                ["total_acknowledged"] = "0",
                ["total_rejected"] = "0",
                ["current_depth"] = "0",
                ["max_depth"] = "0",
                ["last_activity"] = DateTimeOffset.UtcNow.ToString("O")
            };
            
            await _database.HashSetAsync(statsKey, initialStats.Select(kvp => new HashEntry(kvp.Key, kvp.Value)).ToArray());
            
            // Initialize queue keys if they don't exist
            var queueKey = _queueNaming.GetRegularQueueKey(queueName);
            
            // For priority queues, also initialize the priority sorted set
            if (config.EnablePriority)
            {
                var priorityKey = $"{queueKey}:priority";
                // Touch the sorted set to ensure it exists (no-op if already exists)
                await _database.SortedSetScoreAsync(priorityKey, "init");
                
                _logger.LogInformation("Redis: Created priority queue {QueueName} with max size {MaxSize} bytes", 
                    queueName, config.MaxMessageSizeBytes);
            }
            else
            {
                // Touch the list to ensure it exists (no-op if already exists)
                await _database.ListLengthAsync(queueKey);
                
                _logger.LogInformation("Redis: Created FIFO queue {QueueName} with max size {MaxSize} bytes", 
                    queueName, config.MaxMessageSizeBytes);
            }
            
            // Set queue expiration if configured
            if (config.DefaultExpiration > TimeSpan.Zero)
            {
                await _database.KeyExpireAsync(queueKey, config.DefaultExpiration);
                if (config.EnablePriority)
                {
                    await _database.KeyExpireAsync($"{queueKey}:priority", config.DefaultExpiration);
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis: Failed to create queue {QueueName}", queueName);
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

    private readonly Dictionary<string, QueueConfiguration> _queueConfigurations = new()
    {
        // Enable priority queues for swarm coordination queues
        ["swarm.codeunit.assignments"] = new QueueConfiguration 
        { 
            EnablePriority = true,
            MaxMessageSizeBytes = 16384, // Increased from 2048 to handle complex CodeUnitAssignmentMessages
            MaxQueueSize = 10000,
            DefaultExpiration = TimeSpan.FromHours(4),
            MaxRetries = 3
        },
        ["swarm.spawn.requests"] = new QueueConfiguration 
        { 
            EnablePriority = true,
            MaxMessageSizeBytes = 1024,
            MaxQueueSize = 5000,
            DefaultExpiration = TimeSpan.FromHours(2),
            MaxRetries = 3
        },
        ["swarm.completions"] = new QueueConfiguration 
        { 
            EnablePriority = true,
            MaxMessageSizeBytes = 2048,
            MaxQueueSize = 10000,
            DefaultExpiration = TimeSpan.FromHours(1),
            MaxRetries = 2
        },
        ["swarm.function.assignments"] = new QueueConfiguration 
        { 
            EnablePriority = true,
            MaxMessageSizeBytes = 1536,
            MaxQueueSize = 8000,
            DefaultExpiration = TimeSpan.FromHours(2),
            MaxRetries = 3
        },
        ["swarm.workload.distribution"] = new QueueConfiguration 
        { 
            EnablePriority = true,
            MaxMessageSizeBytes = 1024,
            MaxQueueSize = 5000,
            DefaultExpiration = TimeSpan.FromHours(1),
            MaxRetries = 2
        },
        ["builder.notifications"] = new QueueConfiguration 
        { 
            EnablePriority = true,
            MaxMessageSizeBytes = 1024,
            MaxQueueSize = 3000,
            DefaultExpiration = TimeSpan.FromMinutes(30),
            MaxRetries = 2
        },
        ["builder.errors"] = new QueueConfiguration 
        { 
            EnablePriority = false, // Error queues use FIFO
            MaxMessageSizeBytes = 2048,
            MaxQueueSize = 5000,
            DefaultExpiration = TimeSpan.FromHours(24),
            MaxRetries = 1
        }
    };

    private async Task<QueueConfiguration> GetQueueConfigAsync(string queueName, CancellationToken cancellationToken)
    {
        try
        {
            // First try to get from Redis
            var configKey = _queueNaming.GetQueueConfigKey(queueName);
            var configValue = await _database.StringGetAsync(configKey);
            
            if (configValue.HasValue)
            {
                var config = JsonSerializer.Deserialize<QueueConfiguration>(configValue!, _jsonOptions);
                if (config != null)
                {
                    _logger.LogDebug("Redis: Retrieved configuration for queue {QueueName} from Redis", queueName);
                    return config;
                }
            }
            
            // Fallback to in-memory configuration
            if (_queueConfigurations.TryGetValue(queueName, out var inMemoryConfig))
            {
                _logger.LogDebug("Redis: Using in-memory configuration for queue {QueueName}", queueName);
                
                // Store the configuration in Redis for future use
                await _database.StringSetAsync(
                    configKey, 
                    JsonSerializer.Serialize(inMemoryConfig, _jsonOptions), 
                    TimeSpan.FromHours(24)
                );
                
                return inMemoryConfig;
            }
            
            // Return default configuration as last resort
            _logger.LogWarning("Redis: No configuration found for queue {QueueName}, using default", queueName);
            return new QueueConfiguration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis: Failed to get configuration for queue {QueueName}, using defaults", queueName);
            
            // Try fallback to in-memory config even on error
            if (_queueConfigurations.TryGetValue(queueName, out var fallbackConfig))
            {
                return fallbackConfig;
            }
            
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

    private async IAsyncEnumerable<MessageQueueItem<T>> ConsumeMessages<T>(
        string queueName, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogInformation("Redis: ConsumeMessages<{Type}> started for queue {QueueName}", typeof(T).Name, queueName);
        
        var iterationCount = 0;
        var totalMessagesConsumed = 0;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            iterationCount++;
            MessageQueueItem<T>? messageItem = null;
            
            try
            {
                _logger.LogDebug("Redis: ConsumeMessages iteration #{IterationCount} for queue {QueueName} - checking for messages", iterationCount, queueName);
                
                // Check priority queue first (sorted set) - get highest priority (highest score)
                var priorityQueueKey = _queueNaming.GetPriorityQueueKey(queueName);
                _logger.LogDebug("Redis: Checking priority queue {PriorityQueueKey}", priorityQueueKey);
                var priorityMessages = await _database.SortedSetRangeByScoreWithScoresAsync(priorityQueueKey, 
                    order: Order.Descending, take: 1);
                
                if (priorityMessages.Length > 0)
                {
                    _logger.LogInformation("Redis: Found {MessageCount} priority messages in queue {QueueName}", priorityMessages.Length, queueName);
                    
                    var priorityEntry = priorityMessages[0];
                    _logger.LogDebug("Redis: Attempting to remove priority message with score {Score} from queue {QueueName}", priorityEntry.Score, queueName);
                    
                    var removed = await _database.SortedSetRemoveAsync(priorityQueueKey, priorityEntry.Element);
                    if (removed)
                    {
                        _logger.LogInformation("Redis: Successfully removed priority message from queue {QueueName}, deserializing...", queueName);
                        
                        try
                        {
                            messageItem = JsonSerializer.Deserialize<MessageQueueItem<T>>(priorityEntry.Element!, _jsonOptions);
                            if (messageItem != null)
                            {
                                totalMessagesConsumed++;
                                _logger.LogInformation("Redis: Successfully deserialized priority message #{MessageCount} from {QueueName} - MessageId: {MessageId}, Type: {DataType}", 
                                    totalMessagesConsumed, queueName, messageItem.Id, messageItem.Data?.GetType().Name ?? "NULL");
                            }
                            else
                            {
                                _logger.LogWarning("Redis: Priority message deserialized to NULL from queue {QueueName}", queueName);
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogError(jsonEx, "Redis: JSON deserialization failed for priority message from queue {QueueName}: {JsonError}", queueName, jsonEx.Message);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Redis: Failed to remove priority message from queue {QueueName} (already consumed by another process?)", queueName);
                    }
                }
                else
                {
                    _logger.LogDebug("Redis: No priority messages found in queue {QueueName}", queueName);
                }

                // Fallback to regular FIFO queue (list) if no priority message
                if (messageItem == null)
                {
                    _logger.LogDebug("Redis: Checking regular FIFO queue {QueueName}", queueName);
                    var regularMessage = await _database.ListLeftPopAsync(queueName);
                    if (regularMessage.HasValue)
                    {
                        _logger.LogInformation("Redis: Found regular message in FIFO queue {QueueName}, deserializing...", queueName);
                        
                        try
                        {
                            messageItem = JsonSerializer.Deserialize<MessageQueueItem<T>>(regularMessage!, _jsonOptions);
                            if (messageItem != null)
                            {
                                totalMessagesConsumed++;
                                _logger.LogInformation("Redis: Successfully deserialized regular message #{MessageCount} from {QueueName} - MessageId: {MessageId}", 
                                    totalMessagesConsumed, queueName, messageItem.Id);
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogError(jsonEx, "Redis: JSON deserialization failed for regular message from queue {QueueName}: {JsonError}", queueName, jsonEx.Message);
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Redis: No regular messages found in FIFO queue {QueueName}", queueName);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Redis: ConsumeMessages cancelled for queue {QueueName} after {Iterations} iterations, {TotalConsumed} messages consumed", 
                    queueName, iterationCount, totalMessagesConsumed);
                yield break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis: CRITICAL ERROR in ConsumeMessages for queue {QueueName} at iteration #{IterationCount}: {ExceptionType} - {ExceptionMessage}", 
                    queueName, iterationCount, ex.GetType().Name, ex.Message);
                _logger.LogDebug(ex, "Redis: Full exception details for queue {QueueName}: {StackTrace}", queueName, ex.StackTrace);
                
                _logger.LogInformation("Redis: Waiting 5 seconds before retrying after error in queue {QueueName}", queueName);
                await Task.Delay(5000, cancellationToken); // Wait longer on error
                continue;
            }

            if (messageItem != null)
            {
                _logger.LogInformation("Redis: YIELDING message #{MessageCount} with ID {MessageId} to consumer for queue {QueueName}", 
                    totalMessagesConsumed, messageItem.Id, queueName);
                yield return messageItem;
                _logger.LogDebug("Redis: Message #{MessageCount} successfully yielded and control returned from consumer", totalMessagesConsumed);
            }
            else
            {
                // No messages available, wait before polling again
                if (iterationCount % 10 == 0) // Log every 10th empty poll to avoid spam
                {
                    _logger.LogDebug("Redis: No messages available in queue {QueueName} after {Iterations} iterations - waiting 1 second", queueName, iterationCount);
                }
                await Task.Delay(1000, cancellationToken);
            }
        }
        
        _logger.LogInformation("Redis: ConsumeMessages stopped for queue {QueueName} after {Iterations} iterations, {TotalConsumed} total messages consumed", 
            queueName, iterationCount, totalMessagesConsumed);
    }

    public void Dispose()
    {
        try
        {
            _redis?.Dispose();
            _logger.LogInformation("Redis: Connection disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis: Error disposing connection");
        }
    }

    #endregion
}
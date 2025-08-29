using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;
using Ensemble.Maestro.Dotnet.Core.Services;
using StackExchange.Redis;
using System.Text.Json;

namespace Ensemble.Maestro.Dotnet.Api.Test;

/// <summary>
/// Redis inspection endpoint for direct Redis investigation
/// Provides raw Redis query capabilities to investigate message queue issues
/// </summary>
public class RedisInspectionEndpoint : EndpointWithoutRequest<RedisInspectionResponse>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RedisInspectionEndpoint> _logger;

    public RedisInspectionEndpoint(IServiceProvider serviceProvider, ILogger<RedisInspectionEndpoint> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/api/test/redis/inspect");
        AllowAnonymous();
        Summary(s => {
            s.Summary = "Redis Direct Inspection - Raw Redis investigation for message queue debugging";
            s.Description = "Directly queries Redis to inspect keys, queue contents, and message storage";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        _logger.LogInformation("🔍 REDIS INSPECTION - Starting direct Redis investigation");

        var response = new RedisInspectionResponse
        {
            InspectionStarted = DateTime.UtcNow,
            Steps = new List<RedisInspectionStep>()
        };

        try
        {
            // Step 1: Get Redis connection
            response.Steps.Add(new RedisInspectionStep { Step = "1", Description = "Get Redis connection", Status = "Starting", Timestamp = DateTime.UtcNow });
            
            using var scope = _serviceProvider.CreateScope();
            var redisService = scope.ServiceProvider.GetRequiredService<IRedisMessageQueueService>();
            
            // Use reflection to get the Redis database
            var databaseField = typeof(RedisMessageQueueService).GetField("_database", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var database = databaseField?.GetValue(redisService) as IDatabase;
            
            if (database == null)
            {
                throw new InvalidOperationException("Could not access Redis database from RedisMessageQueueService");
            }
            
            response.Steps.Last().Status = "Success";
            response.Steps.Last().Details = "Successfully obtained Redis database connection";
            _logger.LogInformation("✅ Step 1 completed - Redis connection obtained");

            // Step 2: Get all Redis keys
            response.Steps.Add(new RedisInspectionStep { Step = "2", Description = "Scan all Redis keys", Status = "Starting", Timestamp = DateTime.UtcNow });
            
            var server = database.Multiplexer.GetServer(database.Multiplexer.GetEndPoints().First());
            var allKeys = server.Keys(pattern: "*").ToList();
            
            response.AllKeys = allKeys.Select(k => k.ToString()).ToList();
            response.Steps.Last().Status = "Success";
            response.Steps.Last().Details = $"Found {allKeys.Count} keys in Redis";
            _logger.LogInformation("🔑 Step 2 completed - Found {KeyCount} keys: [{Keys}]", 
                allKeys.Count, string.Join(", ", allKeys.Take(10).Select(k => k.ToString())));

            // Step 3: Check specific queue keys
            response.Steps.Add(new RedisInspectionStep { Step = "3", Description = "Check CUCS queue keys", Status = "Starting", Timestamp = DateTime.UtcNow });
            
            var queuePatterns = new[] { "*cucs*", "*codeunit*", "*assignment*", "*swarm*" };
            var queueKeys = new List<string>();
            
            foreach (var pattern in queuePatterns)
            {
                var matchingKeys = server.Keys(pattern: pattern).Select(k => k.ToString()).ToList();
                queueKeys.AddRange(matchingKeys);
                _logger.LogInformation("🎯 Pattern '{Pattern}' matches: [{Keys}]", pattern, string.Join(", ", matchingKeys));
            }
            
            response.QueueKeys = queueKeys.Distinct().ToList();
            response.Steps.Last().Status = "Success";
            response.Steps.Last().Details = $"Found {response.QueueKeys.Count} queue-related keys";

            // Step 4: Inspect queue contents
            response.Steps.Add(new RedisInspectionStep { Step = "4", Description = "Inspect queue contents", Status = "Starting", Timestamp = DateTime.UtcNow });
            
            response.QueueContents = new List<QueueContent>();
            
            foreach (var queueKey in response.QueueKeys)
            {
                var queueContent = new QueueContent { QueueName = queueKey };
                
                try
                {
                    // Check if it's a sorted set (priority queue)
                    var sortedSetLength = await database.SortedSetLengthAsync(queueKey);
                    if (sortedSetLength > 0)
                    {
                        queueContent.Type = "SortedSet";
                        queueContent.Length = sortedSetLength;
                        
                        // Get some sample entries
                        var entries = await database.SortedSetRangeByScoreWithScoresAsync(queueKey, take: 5);
                        queueContent.SampleEntries = entries.Select(e => $"Score: {e.Score}, Value: {e.Element}").ToList();
                        
                        _logger.LogInformation("📊 SortedSet '{QueueKey}': Length={Length}, Sample=[{Samples}]", 
                            queueKey, sortedSetLength, string.Join(" | ", queueContent.SampleEntries.Take(2)));
                    }
                    
                    // Check if it's a list
                    var listLength = await database.ListLengthAsync(queueKey);
                    if (listLength > 0)
                    {
                        queueContent.Type = queueContent.Type == null ? "List" : queueContent.Type + "+List";
                        queueContent.Length = Math.Max(queueContent.Length, listLength);
                        
                        // Get some sample entries
                        var listEntries = await database.ListRangeAsync(queueKey, 0, 4);
                        var listSamples = listEntries.Select(e => e.ToString()).ToList();
                        queueContent.SampleEntries.AddRange(listSamples);
                        
                        _logger.LogInformation("📋 List '{QueueKey}': Length={Length}, Sample=[{Samples}]", 
                            queueKey, listLength, string.Join(" | ", listSamples.Take(2)));
                    }
                    
                    // Check if it's a string
                    if (queueContent.Type == null)
                    {
                        var stringValue = await database.StringGetAsync(queueKey);
                        if (stringValue.HasValue)
                        {
                            queueContent.Type = "String";
                            queueContent.Length = 1;
                            queueContent.SampleEntries = new List<string> { stringValue.ToString().Substring(0, Math.Min(100, stringValue.ToString().Length)) };
                            
                            _logger.LogInformation("📄 String '{QueueKey}': Value preview: {Preview}...", 
                                queueKey, stringValue.ToString().Substring(0, Math.Min(50, stringValue.ToString().Length)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    queueContent.Error = ex.Message;
                    _logger.LogWarning("⚠️ Error inspecting queue '{QueueKey}': {Error}", queueKey, ex.Message);
                }
                
                response.QueueContents.Add(queueContent);
            }
            
            response.Steps.Last().Status = "Success";
            response.Steps.Last().Details = $"Inspected {response.QueueContents.Count} queues";

            // Step 5: Check specific CUCS queue
            response.Steps.Add(new RedisInspectionStep { Step = "5", Description = "Check exact CUCS queue name", Status = "Starting", Timestamp = DateTime.UtcNow });
            
            var cucsQueueName = "swarm.codeunit.assignments";  // This should match what CUCS is listening to
            var cucsQueueExists = await database.KeyExistsAsync(cucsQueueName);
            
            if (cucsQueueExists)
            {
                var cucsQueueLength = await database.SortedSetLengthAsync(cucsQueueName);
                var cucsListLength = await database.ListLengthAsync(cucsQueueName);
                
                response.CucsQueueInfo = new QueueContent
                {
                    QueueName = cucsQueueName,
                    Type = cucsQueueLength > 0 ? "SortedSet" : (cucsListLength > 0 ? "List" : "Empty"),
                    Length = Math.Max(cucsQueueLength, cucsListLength)
                };
                
                if (response.CucsQueueInfo.Length > 0)
                {
                    // Get actual messages from CUCS queue
                    if (cucsQueueLength > 0)
                    {
                        var entries = await database.SortedSetRangeByScoreWithScoresAsync(cucsQueueName, take: 3);
                        response.CucsQueueInfo.SampleEntries = entries.Select(e => e.Element.ToString()).ToList();
                    }
                    else if (cucsListLength > 0)
                    {
                        var entries = await database.ListRangeAsync(cucsQueueName, 0, 2);
                        response.CucsQueueInfo.SampleEntries = entries.Select(e => e.ToString()).ToList();
                    }
                }
                
                _logger.LogInformation("🎯 CUCS queue '{QueueName}' exists: Type={Type}, Length={Length}", 
                    cucsQueueName, response.CucsQueueInfo.Type, response.CucsQueueInfo.Length);
            }
            else
            {
                response.CucsQueueInfo = new QueueContent
                {
                    QueueName = cucsQueueName,
                    Type = "NotFound",
                    Length = 0,
                    Error = "Queue does not exist in Redis"
                };
                
                _logger.LogWarning("❌ CUCS queue '{QueueName}' does NOT exist in Redis!", cucsQueueName);
            }
            
            response.Steps.Last().Status = cucsQueueExists ? "Success" : "Warning";
            response.Steps.Last().Details = cucsQueueExists 
                ? $"CUCS queue exists with {response.CucsQueueInfo.Length} items"
                : "CUCS queue does not exist - this is the problem!";

            response.Success = true;
            response.Message = cucsQueueExists 
                ? $"Redis inspection completed - CUCS queue has {response.CucsQueueInfo.Length} messages"
                : "Redis inspection completed - CUCS queue is missing!";
            
            _logger.LogInformation("🎉 REDIS INSPECTION COMPLETED - Queue exists: {Exists}, Messages: {Count}", 
                cucsQueueExists, response.CucsQueueInfo?.Length ?? 0);
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Redis inspection failed: {ex.Message}";
            
            if (response.Steps.Any())
            {
                response.Steps.Last().Status = "Failed";
                response.Steps.Last().Details = $"Exception: {ex.GetType().Name} - {ex.Message}";
            }
            
            _logger.LogError(ex, "💥 REDIS INSPECTION FAILED - Exception: {ExceptionType}, Message: {ExceptionMessage}",
                ex.GetType().Name, ex.Message);
        }
        finally
        {
            response.InspectionCompleted = DateTime.UtcNow;
            response.DurationSeconds = (int)(response.InspectionCompleted.Value - response.InspectionStarted).TotalSeconds;
        }

        await Send.OkAsync(response, ct);
    }
}

public class RedisInspectionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime InspectionStarted { get; set; }
    public DateTime? InspectionCompleted { get; set; }
    public int DurationSeconds { get; set; }
    public List<RedisInspectionStep> Steps { get; set; } = new();
    public List<string> AllKeys { get; set; } = new();
    public List<string> QueueKeys { get; set; } = new();
    public List<QueueContent> QueueContents { get; set; } = new();
    public QueueContent? CucsQueueInfo { get; set; }
}

public class RedisInspectionStep
{
    public string Step { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class QueueContent
{
    public string QueueName { get; set; } = string.Empty;
    public string? Type { get; set; }
    public long Length { get; set; }
    public List<string> SampleEntries { get; set; } = new();
    public string? Error { get; set; }
}
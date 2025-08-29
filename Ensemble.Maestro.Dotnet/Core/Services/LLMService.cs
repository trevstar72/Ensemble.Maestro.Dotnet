using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Ensemble.Maestro.Dotnet.Core.Configuration;

namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Service for LLM interactions using Semantic Kernel
/// </summary>
public class LLMService : ILLMService
{
    private readonly Kernel _kernel;
    private readonly ILogger<LLMService> _logger;
    private readonly IConfiguration _configuration;
    private readonly OutputPathsConfiguration _outputPaths;
    
    public LLMService(Kernel kernel, ILogger<LLMService> logger, IConfiguration configuration, IOptions<OutputPathsConfiguration> outputPaths)
    {
        _kernel = kernel;
        _logger = logger;
        _configuration = configuration;
        _outputPaths = outputPaths.Value;
    }
    
    public async Task<LLMResponse> GenerateResponseAsync(
        string systemPrompt,
        string userPrompt,
        int maxTokens = 4000,
        float temperature = 0.7f,
        CancellationToken cancellationToken = default,
        string agentType = "Unknown",
        string pipelineStage = "Unknown")
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("=== LLM CALL START ===");
            _logger.LogInformation("Model: {Model}, MaxTokens: {MaxTokens}, Temperature: {Temperature}", 
                GetConfiguredModel(), maxTokens, temperature);
            _logger.LogInformation("System Prompt: {SystemPrompt}", 
                !string.IsNullOrEmpty(systemPrompt) ? systemPrompt.Substring(0, Math.Min(200, systemPrompt.Length)) + "..." : "NONE");
            _logger.LogInformation("User Prompt: {UserPrompt}", userPrompt.Substring(0, Math.Min(200, userPrompt.Length)) + "...");
            
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            
            // Create chat history
            var chatHistory = new ChatHistory();
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                chatHistory.AddSystemMessage(systemPrompt);
            }
            chatHistory.AddUserMessage(userPrompt);
            
            // Configure execution settings
            var executionSettings = new PromptExecutionSettings
            {
                ExtensionData = new Dictionary<string, object>
                {
                    { "max_tokens", maxTokens },
                    { "temperature", temperature }
                }
            };
            
            _logger.LogInformation("Making LLM API call...");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MAKING LLM API CALL - Model: {GetConfiguredModel()}");
            
            // Add timeout for hanging calls
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            // Generate response
            var response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                _kernel,
                combinedCts.Token);
            
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] LLM API CALL SUCCESS - Response Length: {response.Content?.Length ?? 0}");
            
            stopwatch.Stop();
            
            // Calculate token counts and costs
            var inputTokens = EstimateTokenCount(systemPrompt + userPrompt);
            var outputTokens = EstimateTokenCount(response.Content ?? "");
            var cost = CalculateCost(inputTokens, outputTokens);
            
            var result = new LLMResponse
            {
                Success = true,
                Content = response.Content ?? "",
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                Cost = cost,
                Duration = stopwatch.Elapsed,
                ModelUsed = GetConfiguredModel(),
                Temperature = temperature
            };
            
            _logger.LogInformation("=== LLM CALL SUCCESS ===");
            _logger.LogInformation("Response: {Response}", result.Content.Substring(0, Math.Min(300, result.Content.Length)) + "...");
            _logger.LogInformation("Metrics: {InputTokens} input + {OutputTokens} output = {TotalTokens} tokens, Cost: ${Cost:F4}, Duration: {Duration}ms",
                result.InputTokens, result.OutputTokens, result.TotalTokens, result.Cost, result.Duration.TotalMilliseconds);
            
            // Console log for immediate verification
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] === AI GENERATED CONTENT ===");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {result.Content}");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] === END AI CONTENT ({result.InputTokens}+{result.OutputTokens}={result.TotalTokens} tokens, ${result.Cost:F4}) ===");
            
            // Save AI-generated content to file
            await SaveContentToFileAsync(result.Content, result.ModelUsed, result.InputTokens, result.OutputTokens, result.Cost, agentType, pipelineStage);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "=== LLM CALL FAILED === Error: {ErrorMessage}, Duration: {Duration}ms", ex.Message, stopwatch.ElapsedMilliseconds);
            
            // Console log for immediate debugging
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] === LLM CALL FAILED ===");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Error: {ex.Message}");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Duration: {stopwatch.ElapsedMilliseconds}ms");
            if (ex is OperationCanceledException)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] TIMEOUT DETECTED - Call was cancelled");
            }
            
            return new LLMResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed,
                ModelUsed = GetConfiguredModel(),
                Temperature = temperature
            };
        }
    }
    
    /// <summary>
    /// Estimate token count using simple word-based calculation
    /// This is a rough approximation - real implementations would use tiktoken or similar
    /// </summary>
    private static int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        
        // Rough estimation: 1 token ≈ 0.75 words, with minimum counts for punctuation
        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var characterCount = text.Length;
        
        // More accurate approximation that considers punctuation and whitespace
        return Math.Max(wordCount, characterCount / 4);
    }
    
    /// <summary>
    /// Calculate cost based on model and token usage
    /// </summary>
    private decimal CalculateCost(int inputTokens, int outputTokens)
    {
        var model = GetConfiguredModel();
        
        // Pricing per 1K tokens (as of early 2025)
        var (inputCost, outputCost) = model.ToLowerInvariant() switch
        {
            "gpt-4o-mini" => (0.000150m, 0.000600m),
            "gpt-3.5-turbo" => (0.0015m, 0.002m),
            "claude-3-haiku-20240307" => (0.00025m, 0.00125m),
            "claude-3-5-sonnet-20241022" => (0.003m, 0.015m),
            _ => (0.001m, 0.002m) // fallback pricing
        };
        
        return (inputTokens / 1000m * inputCost) + (outputTokens / 1000m * outputCost);
    }
    
    /// <summary>
    /// Get the configured model name
    /// </summary>
    private string GetConfiguredModel()
    {
        return _configuration["SemanticKernel:OpenAI:ModelId"] ?? 
               _configuration["SemanticKernel:Anthropic:ModelId"] ?? 
               "unknown";
    }
    
    /// <summary>
    /// Save AI-generated content to file with metadata
    /// </summary>
    private async Task SaveContentToFileAsync(string content, string model, int inputTokens, int outputTokens, decimal cost, string agentType = "Unknown", string pipelineStage = "Unknown")
    {
        try
        {
            // Create outputs directory if it doesn't exist using configured path
            var outputsDir = _outputPaths.AgentOutputsDirectory;
            Directory.CreateDirectory(outputsDir);
            
            // Create filename with timestamp and agent info
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            var filename = $"ai-generated-{agentType}-{pipelineStage}-{timestamp}.md";
            var filePath = Path.Combine(outputsDir, filename);
            
            // Create content with metadata header
            var fileContent = $@"# AI Generated Content
**Agent:** {agentType}  
**Pipeline Stage:** {pipelineStage}  
**Model:** {model}  
**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}  
**Input Tokens:** {inputTokens}  
**Output Tokens:** {outputTokens}  
**Total Tokens:** {inputTokens + outputTokens}  
**Cost:** ${cost:F4}  

---

{content}
";
            
            await File.WriteAllTextAsync(filePath, fileContent);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] === CONTENT SAVED TO FILE: {filename} ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save AI content to file: {ErrorMessage}", ex.Message);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ERROR: Failed to save content to file - {ex.Message}");
        }
    }
}
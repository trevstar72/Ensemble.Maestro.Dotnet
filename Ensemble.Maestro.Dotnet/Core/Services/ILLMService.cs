namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Service interface for LLM interactions
/// </summary>
public interface ILLMService
{
    /// <summary>
    /// Generate a response using the configured LLM
    /// </summary>
    /// <param name="systemPrompt">System prompt</param>
    /// <param name="userPrompt">User prompt</param>
    /// <param name="maxTokens">Maximum tokens to generate</param>
    /// <param name="temperature">Temperature for generation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="agentType">Type of agent making the request</param>
    /// <param name="pipelineStage">Current pipeline stage</param>
    /// <returns>LLM response with usage statistics</returns>
    Task<LLMResponse> GenerateResponseAsync(
        string systemPrompt,
        string userPrompt,
        int maxTokens = 4000,
        float temperature = 0.7f,
        CancellationToken cancellationToken = default,
        string agentType = "Unknown",
        string pipelineStage = "Unknown");
}

/// <summary>
/// Response from LLM with usage statistics
/// </summary>
public class LLMResponse
{
    public bool Success { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens => InputTokens + OutputTokens;
    public decimal Cost { get; set; }
    public TimeSpan Duration { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public float Temperature { get; set; }
}
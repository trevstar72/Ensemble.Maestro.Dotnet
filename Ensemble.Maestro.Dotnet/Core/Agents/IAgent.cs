namespace Ensemble.Maestro.Dotnet.Core.Agents;

/// <summary>
/// Base interface for all agents in the Maestro system
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Unique identifier for the agent type
    /// </summary>
    string AgentType { get; }
    
    /// <summary>
    /// Human-readable name for the agent
    /// </summary>
    string AgentName { get; }
    
    /// <summary>
    /// Priority level for agent execution
    /// </summary>
    string Priority { get; }
    
    /// <summary>
    /// Execute the agent with the given context
    /// </summary>
    /// <param name="context">Execution context containing input and configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of agent execution</returns>
    Task<AgentExecutionResult> ExecuteAsync(AgentExecutionContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate if the agent can execute with the given context
    /// </summary>
    /// <param name="context">Execution context to validate</param>
    /// <returns>True if agent can execute, false otherwise</returns>
    bool CanExecute(AgentExecutionContext context);
    
    /// <summary>
    /// Get estimated duration for execution in seconds
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Estimated duration in seconds</returns>
    int GetEstimatedDurationSeconds(AgentExecutionContext context);
}
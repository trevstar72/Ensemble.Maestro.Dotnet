namespace Ensemble.Maestro.Dotnet.Core.Agents;

/// <summary>
/// Factory interface for creating agent instances
/// </summary>
public interface IAgentFactory
{
    /// <summary>
    /// Create an agent by type name
    /// </summary>
    /// <param name="agentType">Type of agent to create</param>
    /// <returns>Agent instance or null if type not found</returns>
    IAgent? CreateAgent(string agentType);
    
    /// <summary>
    /// Get all available agent types
    /// </summary>
    /// <returns>Collection of available agent types</returns>
    IEnumerable<string> GetAvailableAgentTypes();
    
    /// <summary>
    /// Get agents for a specific stage
    /// </summary>
    /// <param name="stageName">Name of the pipeline stage</param>
    /// <returns>Collection of agents for the stage</returns>
    IEnumerable<IAgent> GetAgentsForStage(string stageName);
    
    /// <summary>
    /// Check if an agent type is available
    /// </summary>
    /// <param name="agentType">Agent type to check</param>
    /// <returns>True if agent type is available</returns>
    bool IsAgentTypeAvailable(string agentType);
}
using System;

namespace MoniaAgent.Agent.Outputs
{
    /// <summary>
    /// Base class for all agent execution outputs
    /// </summary>
    public abstract class AgentOutput
    {
        /// <summary>
        /// Indicates if the agent completed successfully
        /// </summary>
        public bool Success { get; set; } = true;
        
        /// <summary>
        /// Error message if Success is false
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Content of the output - unified access across all output types
        /// </summary>
        public virtual string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// Metadata about the execution
        /// </summary>
        public ExecutionMetadata Metadata { get; set; } = new();
    }
}
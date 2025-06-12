using System;

namespace MoniaAgent.Core.Outputs
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
        /// Metadata about the execution
        /// </summary>
        public ExecutionMetadata Metadata { get; set; } = new();
    }
}
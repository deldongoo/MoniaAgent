using System;

namespace MoniaAgent.Core.Outputs
{
    /// <summary>
    /// Metadata about agent execution
    /// </summary>
    public class ExecutionMetadata
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public string AgentName { get; set; } = string.Empty;
    }
}
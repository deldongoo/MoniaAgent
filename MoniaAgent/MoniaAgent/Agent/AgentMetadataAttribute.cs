using System;

namespace MoniaAgent.Agent
{
    /// <summary>
    /// Attribute to define static metadata for agents without requiring instantiation.
    /// This metadata is used by the PlannerAgent to select appropriate agents.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AgentMetadataAttribute : Attribute
    {
        /// <summary>
        /// The unique name of the agent
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A brief description of the agent's specialty
        /// </summary>
        public string Specialty { get; set; } = string.Empty;

        /// <summary>
        /// Keywords that help identify when this agent should be used
        /// </summary>
        public string[] Keywords { get; set; } = Array.Empty<string>();

        /// <summary>
        /// The system prompt/goal that defines the agent's behavior
        /// </summary>
        public string Goal { get; set; } = string.Empty;
    }
}
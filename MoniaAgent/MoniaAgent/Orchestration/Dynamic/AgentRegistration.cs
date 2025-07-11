using System;

namespace MoniaAgent.Orchestration.Dynamic
{
    /// <summary>
    /// Contains comprehensive metadata about an agent for registration and discovery
    /// </summary>
    public class AgentRegistration
    {
        /// <summary>
        /// The type of the agent class
        /// </summary>
        public Type AgentType { get; set; } = null!;

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

        /// <summary>
        /// The input types this agent supports (extracted from generic parameters)
        /// </summary>
        public Type[] SupportedInputTypes { get; set; } = Array.Empty<Type>();

        /// <summary>
        /// The output type this agent produces (extracted from generic parameters)
        /// </summary>
        public Type? ExpectedOutputType { get; set; }

        /// <summary>
        /// Names of tools this agent uses (for informational purposes)
        /// </summary>
        public string[] ToolNames { get; set; } = Array.Empty<string>();
    }
}
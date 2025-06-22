using MoniaAgent.Configuration;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;
using System.ComponentModel;

namespace MoniaAgent.Core
{
    public class PlannerAgent : TypedAgent<TextInput, TextOutput>
    {
        private readonly AgentRegistry registry;

        public PlannerAgent(LLM llm, AgentRegistry registry) : base(llm)
        {
            this.registry = registry;
        }

        protected override AgentConfig Configure() => new()
        {
            Name = "PlannerAgent",
            Specialty = "Agent selection and task planning",
            Keywords = new[] { "plan", "select", "choose", "agent" },
            ToolMethods = new Delegate[] { GetAvailableAgents },
            Goal = "You are an intelligent agent planner. Your role is to analyze tasks and select the most appropriate agent from the available registry. Always use the get_available_agents tool to see what agents are available, then respond with ONLY the exact agent name that should handle the task."
        };

        [Description("Get list of all available agents with their descriptions")]
        private string GetAvailableAgents()
        {
            return registry.GetAgentsInfo();
        }

        protected override TextOutput ConvertResultToOutput(string textResult, ExecutionMetadata metadata)
        {
            var selectedAgent = ExtractAgentName(textResult);
            
            return new TextOutput
            {
                Success = !string.IsNullOrEmpty(selectedAgent) && registry.GetRegisteredAgentNames().Contains(selectedAgent),
                Content = selectedAgent ?? textResult,
                Metadata = metadata
            };
        }

        private string? ExtractAgentName(string response)
        {
            var registeredNames = registry.GetRegisteredAgentNames();
            
            foreach (var agentName in registeredNames)
            {
                if (response.Contains(agentName, StringComparison.OrdinalIgnoreCase))
                {
                    return agentName;
                }
            }
            
            return null;
        }
    }
}
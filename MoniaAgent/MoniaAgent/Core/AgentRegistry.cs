using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoniaAgent.Configuration;

namespace MoniaAgent.Core
{
    public class AgentRegistry
    {
        private readonly Dictionary<string, (Type AgentType, string Description)> agents = new();

        public void Register<T>(string description) where T : Agent
        {
            var agentType = typeof(T);
            var agentName = agentType.Name;
            agents[agentName] = (agentType, description);
        }

        public Agent Create(string agentName, LLM llm)
        {
            if (!agents.ContainsKey(agentName))
                throw new ArgumentException($"Agent type '{agentName}' not registered");

            var (agentType, _) = agents[agentName];
            return (Agent)Activator.CreateInstance(agentType, llm)!;
        }

        public string GetAgentsInfo()
        {
            if (!agents.Any())
                return "No agents registered";

            var sb = new StringBuilder();
            sb.AppendLine("Available agents:");
            
            foreach (var (agentName, (_, description)) in agents)
            {
                sb.AppendLine($"- {agentName}: {description}");
            }

            return sb.ToString();
        }

        public IReadOnlyList<string> GetRegisteredAgentNames()
        {
            return agents.Keys.ToList().AsReadOnly();
        }
    }
}
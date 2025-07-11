using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using MoniaAgent.Configuration;
using MoniaAgent.Agent;
using Agent = MoniaAgent.Agent.Agent;

namespace MoniaAgent.Orchestration.Dynamic
{
    public class AgentRegistry
    {
        private readonly Dictionary<string, AgentRegistration> agents = new();

        public void Register<T>() where T : MoniaAgent.Agent.Agent
        {
            var agentType = typeof(T);
            
            // Extract metadata via reflection
            var registration = AgentMetadataExtractor.ExtractMetadata(agentType);
            if (registration == null)
            {
                throw new InvalidOperationException($"Agent type '{agentType.Name}' must have AgentMetadataAttribute or valid metadata.");
            }
            
            agents[registration.Name] = registration;
        }

        public MoniaAgent.Agent.Agent Create(string agentName, LLM llm)
        {
            if (!agents.ContainsKey(agentName))
                throw new ArgumentException($"Agent type '{agentName}' not registered");

            var registration = agents[agentName];
            return (MoniaAgent.Agent.Agent)Activator.CreateInstance(registration.AgentType, llm)!;
        }

        public string GetAgentsInfo()
        {
            if (!agents.Any())
                return "No agents registered";

            var sb = new StringBuilder();
            sb.AppendLine("Available agents:");
            
            foreach (var (agentName, registration) in agents)
            {
                sb.AppendLine($"- {agentName}: {registration.Specialty}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get detailed capabilities of all registered agents in JSON format
        /// </summary>
        public string GetAgentCapabilities()
        {
            if (!agents.Any())
                return "[]";

            var capabilities = agents.Values.Select(reg => new
            {
                Name = reg.Name,
                Specialty = reg.Specialty,
                Keywords = reg.Keywords,
                Goal = reg.Goal,
                InputTypes = reg.SupportedInputTypes?.Select(t => t.Name) ?? Array.Empty<string>(),
                OutputType = reg.ExpectedOutputType?.Name,
                Tools = reg.ToolNames
            }).ToList();

            return JsonSerializer.Serialize(capabilities, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        public IReadOnlyList<string> GetRegisteredAgentNames()
        {
            return agents.Keys.ToList().AsReadOnly();
        }
    }
}
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoniaAgent.Agents;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;

namespace MoniaAgent.Core
{
    public class AgentOrchestrator
    {
        private static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        private static readonly ILogger logger = loggerFactory.CreateLogger<AgentOrchestrator>();

        private readonly List<Agent> agents = new List<Agent>();

        public void RegisterAgent(Agent agent)
        {
            agents.Add(agent);
            logger.LogInformation("Registered agent: {AgentName} - {Specialty}", agent.Name, agent.Specialty);
        }

        public async Task<AgentOutput> Execute(string task)
        {
            var selectedAgent = SelectBestAgent(task);
            logger.LogInformation("Selected agent {AgentName} for task: {Task}", selectedAgent.Name, task);
            return await selectedAgent.ExecuteAsync(new TextInput(task));
        }

        private Agent SelectBestAgent(string task)
        {
            var capableAgents = agents.Where(a => a.CanHandle(task)).ToList();
            
            if (!capableAgents.Any())
                throw new InvalidOperationException($"No agent can handle the task: {task}");

            // Prioritize specialized agents (non-general)
            var specializedAgents = capableAgents.Where(a => a.Name != "Agent").ToList();
            
            return specializedAgents.FirstOrDefault() ?? capableAgents.First();
        }

       /* public IReadOnlyList<string> GetRegisteredAgentNames()
        {
            return agents.Select(a => $"{a.Name} - {a.Specialty}").ToList().AsReadOnly();
        }*/
    }
}
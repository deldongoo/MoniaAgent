using Microsoft.Extensions.Logging;

namespace MoniaAgent
{
    public class AgentOrchestrator
    {
        private static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        private static readonly ILogger logger = loggerFactory.CreateLogger<AgentOrchestrator>();

        private readonly List<IAgent> agents = new List<IAgent>();

        public void RegisterAgent(IAgent agent)
        {
            agents.Add(agent);
            logger.LogInformation("Registered agent: {AgentName} - {Specialty}", agent.Name, agent.Specialty);
        }

        public async Task<string> Execute(string task)
        {
            var selectedAgent = SelectBestAgent(task);
            logger.LogInformation("Selected agent {AgentName} for task: {Task}", selectedAgent.Name, task);
            return await selectedAgent.Execute(task);
        }

        private IAgent SelectBestAgent(string task)
        {
            var capableAgents = agents.Where(a => a.CanHandle(task)).ToList();
            
            if (!capableAgents.Any())
                throw new InvalidOperationException($"No agent can handle the task: {task}");

            // Prioritize specialized agents (non-general)
            var specializedAgents = capableAgents.Where(a => a.Name != "Agent").ToList();
            
            return specializedAgents.FirstOrDefault() ?? capableAgents.First();
        }

        public IReadOnlyList<IAgent> GetRegisteredAgents()
        {
            return agents.AsReadOnly();
        }
    }
}
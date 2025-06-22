using Microsoft.Extensions.Logging;
using MoniaAgent.Configuration;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;
using System;
using System.Threading.Tasks;

namespace MoniaAgent.Core
{
    public class SmartWorkflow
    {
        private static readonly ILogger logger = MoniaLogging.CreateLogger<SmartWorkflow>();
        
        private readonly AgentRegistry registry;
        private readonly PlannerAgent planner;
        private readonly LLM llm;

        public SmartWorkflow(LLM llm)
        {
            this.llm = llm;
            this.registry = new AgentRegistry();
            this.planner = new PlannerAgent(llm, registry);
        }

        public void RegisterAgentType<T>(string description) where T : Agent
        {
            registry.Register<T>(description);
            logger.LogInformation("Registered agent type: {AgentType} - {Description}", typeof(T).Name, description);
        }

        public async Task<AgentOutput> ExecuteWithPlanning(string task)
        {
            try
            {
                logger.LogInformation("Starting smart execution for task: {Task}", task);
                
                var planningResult = await planner.ExecuteAsync(new TextInput($"Select the best agent to handle this task: {task}"));
                
                if (!planningResult.Success)
                {
                    logger.LogError("Planning failed: {Error}", planningResult.ErrorMessage);
                    return new TextOutput
                    {
                        Success = false,
                        ErrorMessage = $"Planning failed: {planningResult.ErrorMessage}",
                        Content = string.Empty
                    };
                }

                var selectedAgentName = (planningResult as TextOutput)?.Content?.Trim();
                if (string.IsNullOrEmpty(selectedAgentName))
                {
                    logger.LogError("No agent selected by planner");
                    return new TextOutput
                    {
                        Success = false,
                        ErrorMessage = "No agent was selected by the planner",
                        Content = string.Empty
                    };
                }

                logger.LogInformation("Planner selected agent: {AgentName}", selectedAgentName);

                var selectedAgent = registry.Create(selectedAgentName, llm);
                var result = await selectedAgent.ExecuteAsync(new TextInput(task));
                
                logger.LogInformation("Task execution completed with agent: {AgentName}, Success: {Success}", 
                    selectedAgentName, result.Success);
                
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during smart workflow execution");
                return new TextOutput
                {
                    Success = false,
                    ErrorMessage = $"Workflow execution failed: {ex.Message}",
                    Content = string.Empty
                };
            }
        }
    }
}
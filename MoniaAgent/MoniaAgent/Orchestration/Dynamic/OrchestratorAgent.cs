using Microsoft.Extensions.Logging;
using MoniaAgent.Configuration;
using MoniaAgent.Agent;
using MoniaAgent.Agent.Inputs;
using MoniaAgent.Agent.Outputs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Agent = MoniaAgent.Agent.Agent;

namespace MoniaAgent.Orchestration.Dynamic
{
    [AgentMetadata(
        Name = "OrchestratorAgent", 
        Specialty = "Dynamic agent discovery and orchestration",
        Keywords = new[] { "orchestrate", "coordinate", "multi-agent", "discover", "execute" },
        Goal = @"You are an intelligent orchestrator that can discover and use other agents to complete tasks.

Your workflow:
1. Analyze the user's task to understand what needs to be done
2. Use discover_agents to see what agents are available
3. Determine which agent(s) to use based on their specialties
4. Execute agents as needed using execute_agent or execute_agents_parallel
5. Combine results if multiple agents were used
6. Provide a comprehensive response to the user

Important:
- For simple tasks requiring only one agent, use execute_agent
- For complex tasks, break them down and use multiple agents
- Use execute_agents_parallel when tasks can be done concurrently
- Always check agent execution results for success/failure
- Provide clear, integrated responses that combine all agent outputs"
    )]
    public class OrchestratorAgent : TypedAgent<TextInput, TextOutput>
    {
        private static readonly ILogger logger = MoniaLogging.CreateLogger<OrchestratorAgent>();
        private readonly AgentRegistry registry;

        public OrchestratorAgent(LLM llm) : base(llm)
        {
            this.registry = new AgentRegistry();
        }

        public void RegisterAgentType<T>() where T : MoniaAgent.Agent.Agent
        {
            registry.Register<T>();
            logger.LogInformation("Registered agent type: {AgentType}", typeof(T).Name);
        }

        protected override AgentConfig ConfigureTools() => new()
        {
            ToolMethods = new Delegate[] 
            { 
                DiscoverAgents,
                ExecuteAgent,
                ExecuteAgentsParallel
            }
        };

        [Description("Discover all available agents and their capabilities")]
        private string DiscoverAgents()
        {
            logger.LogDebug("Discovering available agents");
            return registry.GetAgentCapabilities();
        }

        [Description("Execute a specific agent with a given task")]
        private async Task<string> ExecuteAgent(
            [Description("The name of the agent to execute")] string agentName, 
            [Description("The task or prompt for the agent")] string task)
        {
            logger.LogInformation("Executing agent {AgentName} with task: {Task}", agentName, task);
            
            try
            {
                var agent = registry.Create(agentName, llm);
                var result = await agent.ExecuteAsync(new TextInput(task));
                
                var response = new
                {
                    agentName,
                    success = result.Success,
                    content = result.Content,
                    errorMessage = result.ErrorMessage
                };
                
                return JsonSerializer.Serialize(response, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing agent {AgentName}", agentName);
                
                var errorResponse = new
                {
                    agentName,
                    success = false,
                    content = string.Empty,
                    errorMessage = $"Failed to execute agent: {ex.Message}"
                };
                
                return JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
        }

        [Description("Execute multiple agents in parallel with their respective tasks")]
        private async Task<string> ExecuteAgentsParallel(
            [Description("Array of agent execution requests in JSON format")] string executionRequests)
        {
            logger.LogInformation("Executing multiple agents in parallel");
            
            try
            {
                var requests = JsonSerializer.Deserialize<AgentExecutionRequest[]>(executionRequests, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (requests == null || requests.Length == 0)
                {
                    return JsonSerializer.Serialize(new { success = false, errorMessage = "No execution requests provided" });
                }

                var tasks = requests.Select(async request =>
                {
                    try
                    {
                        var agent = registry.Create(request.AgentName, llm);
                        var result = await agent.ExecuteAsync(new TextInput(request.Task));
                        
                        return new
                        {
                            agentName = request.AgentName,
                            task = request.Task,
                            success = result.Success,
                            content = result.Content,
                            errorMessage = result.ErrorMessage
                        };
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error executing agent {AgentName}", request.AgentName);
                        return new
                        {
                            agentName = request.AgentName,
                            task = request.Task,
                            success = false,
                            content = string.Empty,
                            errorMessage = $"Failed to execute agent: {ex.Message}"
                        };
                    }
                }).ToArray();

                var results = await Task.WhenAll(tasks);
                
                return JsonSerializer.Serialize(new { results }, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing agents in parallel");
                return JsonSerializer.Serialize(new 
                { 
                    success = false, 
                    errorMessage = $"Failed to execute agents in parallel: {ex.Message}" 
                });
            }
        }

        private class AgentExecutionRequest
        {
            public string AgentName { get; set; } = string.Empty;
            public string Task { get; set; } = string.Empty;
        }

        protected override TextOutput ConvertResultToOutput(string finalLLMAnswer, ExecutionMetadata metadata)
        {
            return new TextOutput
            {
                Content = finalLLMAnswer,
                Success = true,
                Metadata = metadata
            };
        }
    }
}
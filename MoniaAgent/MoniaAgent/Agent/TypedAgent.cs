using Microsoft.Extensions.AI;
using MoniaAgent.Configuration;
using MoniaAgent.Agent.Inputs;
using MoniaAgent.Agent.Outputs;
using MoniaAgent.Tools;
using System.Linq;
using System.Reflection;

namespace MoniaAgent.Agent
{
    /// <summary>
    /// Base class for strongly typed agents with configuration support
    /// </summary>
    public abstract class TypedAgent<TInput, TOutput> : Agent, IAgent<TInput, TOutput>
        where TInput : AgentInput
        where TOutput : AgentOutput, new()
    {
        private readonly AgentConfig config;

        protected TypedAgent(LLM llm) : base(llm)
        {
            config = BuildConfiguration();
            var tools = BuildTools(config);
            Initialize(tools, config);
        }

        /// <summary>
        /// Configure tools and MCP servers for this agent.
        /// Metadata (Name, Specialty, Keywords, Goal) comes from AgentMetadataAttribute.
        /// </summary>
        protected virtual AgentConfig ConfigureTools()
        {
            return new AgentConfig
            {
                ToolMethods = new Delegate[0],
                McpServers = new McpServer[0]
            };
        }

        private AgentConfig BuildConfiguration()
        {
            // Get tools configuration first
            var toolsConfig = ConfigureTools();
            
            // Check if ConfigureTools returned a complete configuration (e.g., PlannerAgent)
            if (!string.IsNullOrEmpty(toolsConfig.Name))
            {
                // ConfigureTools provided full configuration - use it as-is
                return toolsConfig;
            }

            // Otherwise, build configuration from attribute + tools
            var config = new AgentConfig();

            // Get metadata from attribute if present
            var metadataAttribute = GetType().GetCustomAttribute<AgentMetadataAttribute>();
            if (metadataAttribute != null)
            {
                config.Name = metadataAttribute.Name;
                config.Specialty = metadataAttribute.Specialty;
                config.Keywords = metadataAttribute.Keywords;
                config.Goal = metadataAttribute.Goal;
            }
            else
            {
                // Default values if no attribute
                config.Name = GetType().Name;
                config.Specialty = "General purpose assistant";
                config.Keywords = new string[0];
                config.Goal = "You are a helpful AI assistant.";
            }

            // Apply tools configuration
            config.ToolMethods = toolsConfig.ToolMethods ?? new Delegate[0];
            config.McpServers = toolsConfig.McpServers ?? new McpServer[0];
            config.UseStructuredOutput = toolsConfig.UseStructuredOutput;

            return config;
        }

        public override string Name => config.Name;
        public override string Specialty => config.Specialty;

        // Override base ExecuteAsync to handle type validation with fallback
        public override async Task<AgentOutput> ExecuteAsync(AgentInput input, CancellationToken cancellationToken = default)
        {
            if (input is TInput typedInput)
            {
                // Use typed path when input matches expected type
                return await ExecuteAsync(typedInput, cancellationToken);
            }
            
            // Fallback to base Agent.ExecuteAsync for other input types (like TextInput from SmartWorkflow)
            // This allows ConvertInputToPrompt to handle the conversion
            return await base.ExecuteAsync(input, cancellationToken);
        }

        // Typed version - primary implementation
        public virtual async Task<TOutput> ExecuteAsync(TInput input, CancellationToken cancellationToken = default)
        {
            // Call base class with typed input
            var result = await base.ExecuteAsync(input, cancellationToken);
            return (TOutput)result;
        }

        // Override the result conversion for typed output
        protected override AgentOutput ConvertToTypedResult(string textResult, ExecutionMetadata metadata)
        {
            // Let derived classes handle conversion
            return ConvertResultToOutput(textResult, metadata);
        }

        // New abstract method for typed conversion
        protected abstract TOutput ConvertResultToOutput(string finalLLMAnswer, ExecutionMetadata metadata);

        private TOutput CreateTypedError(string message)
        {
            return new TOutput
            {
                Success = false,
                ErrorMessage = message,
                Metadata = new ExecutionMetadata
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow,
                    AgentName = Name
                }
            };
        }

        private static IList<AITool> BuildTools(AgentConfig config)
        {
            var registry = new ToolRegistry();

            // Add method-based tools
            foreach (var method in config.ToolMethods)
            {
                registry.RegisterTool(AIFunctionFactory.Create(method));
            }

            // Add MCP servers
            foreach (var mcpServer in config.McpServers)
            {
                try
                {
                    Task.Run(async () => await registry.RegisterMcpServerAsync(mcpServer))
                        .Wait(TimeSpan.FromSeconds(10));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not load MCP server {mcpServer.Name}: {ex.Message}");
                }
            }

            return registry.GetAllTools();
        }
    }
}
using Microsoft.Extensions.AI;
using MoniaAgent.Configuration;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;
using MoniaAgent.Tools;
using System.Linq;

namespace MoniaAgent.Core
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
            config = Configure();
            var tools = BuildTools(config);
            Initialize(tools, config);
        }

        protected virtual AgentConfig Configure()
        {
            return new AgentConfig
            {
                Name = "Agent",
                Specialty = "General purpose assistant",
                Keywords = new string[0],
                ToolMethods = new Delegate[0],
                McpServers = new McpServer[0],
                Goal = "You are a helpful AI assistant."
            };
        }

        public override string Name => config.Name;
        public override string Specialty => config.Specialty;
        public override Type[] SupportedInputTypes => new[] { typeof(TInput) };
        public override Type ExpectedOutputType => typeof(TOutput);

        public override bool CanHandle(string task)
        {
            return config.Keywords.Any(k => task.ToLower().Contains(k.ToLower()));
        }

        // Override base ExecuteAsync to handle type validation
        public override async Task<AgentOutput> ExecuteAsync(AgentInput input, CancellationToken cancellationToken = default)
        {
            if (input is not TInput typedInput)
            {
                return CreateTypedError($"Invalid input type. Expected {typeof(TInput).Name}, got {input.GetType().Name}");
            }
            
            return await ExecuteAsync(typedInput, cancellationToken);
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
            return ConvertStringToOutput(textResult, metadata);
        }

        // New abstract method for typed conversion
        protected abstract TOutput ConvertStringToOutput(string textResult, ExecutionMetadata metadata);

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
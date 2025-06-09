using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoniaAgent
{
    public abstract class SpecializedAgent : Agent
    {
        private readonly AgentConfig config;

        protected SpecializedAgent(LLM llm) : base(llm)
        {
            config = Configure();
            var tools = BuildTools(config);
            Initialize(tools, config.Goal);
        }

        protected abstract AgentConfig Configure();

        public override string Name => config.Name;
        public override string Specialty => config.Specialty;

        public override bool CanHandle(string task)
        {
            return config.Keywords.Any(k => task.ToLower().Contains(k.ToLower()));
        }

        private static IList<AITool> BuildTools(AgentConfig config)
        {
            var registry = new ToolRegistry();

            // Ajouter les outils basés sur les méthodes
            foreach (var method in config.ToolMethods)
            {
                registry.RegisterTool(AIFunctionFactory.Create(method));
            }

            // Ajouter les serveurs MCP de manière synchrone (limitation actuelle)
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
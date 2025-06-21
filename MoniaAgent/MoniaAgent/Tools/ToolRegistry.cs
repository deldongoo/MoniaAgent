using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Collections.Generic;
using System.Text.Json;
using MoniaAgent.Configuration;
using MoniaAgent.Core;

namespace MoniaAgent.Tools
{
    internal class ToolRegistry
    {
        private readonly ILogger logger;
        private readonly List<AITool> tools = new List<AITool>();

        public ToolRegistry()
        {
            logger = MoniaLogging.CreateLogger<ToolRegistry>();
        }

        public void RegisterTool(AITool tool)
        {
            tools.Add(tool);
            logger.LogInformation("Registered tool: {ToolName}", tool.Name);
        }

        public async Task RegisterMcpServerAsync(McpServer server)
        {
            server?.Validate();            
            try
            {
                IClientTransport clientTransport = server.GetTransport();
                var mcpClient = await McpClientFactory.CreateAsync(clientTransport);
                var mcpTools = await mcpClient.ListToolsAsync();
                tools.AddRange(mcpTools);
                logger.LogInformation("Successfully loaded {ToolCount} tools from MCP server '{ServerName}'",
                    mcpTools.Count(), server.Name);

                /*  logger.LogDebug("Connecting to MCP server '{ServerName}'", server!.Name);
                  IClientTransport clientTransport = server.GetTransport();
                  var mcpClient = await McpClientFactory.CreateAsync(clientTransport);

                  logger.LogDebug("Listing tools from MCP server '{ServerName}'", server.Name);
                  var mcpTools = await mcpClient.ListToolsAsync();

                  logger.LogDebug("Found {ToolCount} tools from MCP server '{ServerName}':", mcpTools.Count(), server.Name);
                  foreach (var tool in mcpTools)
                  {
                      logger.LogDebug("  - {ToolName}: {ToolDescription}", tool.Name, tool.Description);
                  }

                  tools.AddRange(mcpTools);
                  logger.LogInformation("Successfully loaded {ToolCount} tools from MCP server '{ServerName}'", 
                      mcpTools.Count(), server.Name);

                  logger.LogDebug("Total tools in registry after MCP server: {TotalCount}", tools.Count);*/
            }
            catch (Exception ex)
            {
                logger.LogWarning("MCP server '{ServerName}' failed to load: {ErrorMessage}", 
                    server!.Name, ex.Message);
                logger.LogDebug(ex, "Full exception details for MCP server '{ServerName}'", server!.Name);
            }
        }

        public IList<AITool> GetAllTools()
        {
            return tools.AsReadOnly();
        }

        public void Clear()
        {
            tools.Clear();
            logger.LogInformation("Cleared all tools from registry");
        }
    }
}
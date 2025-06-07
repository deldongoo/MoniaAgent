using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace MoniaAgent
{
    public class ToolRegistry
    {
        private static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        private static readonly ILogger logger = loggerFactory.CreateLogger<ToolRegistry>();
        private readonly List<Tool> tools = new List<Tool>();

        public void RegisterTool(Tool tool)
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
                var adaptedTools = mcpTools.Select(t => new McpTool(t, mcpClient));
                
                tools.AddRange(adaptedTools);
                logger.LogInformation("Successfully loaded {ToolCount} tools from MCP server '{ServerName}'", 
                    mcpTools.Count(), server.Name);
            }
            catch (Exception ex)
            {
                logger.LogWarning("MCP server '{ServerName}' failed to load: {ErrorMessage}", 
                    server.Name, ex.Message);
            }
        }

        public IList<Tool> GetAllTools()
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
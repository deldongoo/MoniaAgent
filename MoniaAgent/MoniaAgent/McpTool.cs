using ModelContextProtocol.Client;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoniaAgent
{
    public class McpTool : Tool
    {
        private readonly McpClientTool mcpTool;
        private readonly IMcpClient mcpClient;
        
        public McpTool(McpClientTool mcpTool, IMcpClient mcpClient)
        {
            this.mcpTool = mcpTool;
            this.mcpClient = mcpClient;
        }
        
        public override string Name => mcpTool.Name;
        public override string Description => mcpTool.Description ?? "";
        
        public override async Task<object> ExecuteAsync(Dictionary<string, object> parameters)
        {
            // Convert parameters to the format expected by MCP
            var mcpArguments = new Dictionary<string, object>();
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    mcpArguments[param.Key] = param.Value;
                }
            }
            
            var result = await mcpClient.CallToolAsync(mcpTool.Name, mcpArguments);
            return result?.Content?.FirstOrDefault()?.Text ?? "";
        }
        
        public override object Execute(Dictionary<string, object> parameters)
        {
            // For synchronous calls, we'll use the async version and wait
            return ExecuteAsync(parameters).GetAwaiter().GetResult();
        }
        
        internal override Microsoft.Extensions.AI.AITool ToMicrosoftAI()
        {
            // For MCP tools, we need a slightly different approach
            return Microsoft.Extensions.AI.AIFunctionFactory.Create(
                (Dictionary<string, object> args) => ExecuteAsync(args ?? new Dictionary<string, object>()).GetAwaiter().GetResult(),
                name: Name,
                description: Description
            );
        }
    }
}
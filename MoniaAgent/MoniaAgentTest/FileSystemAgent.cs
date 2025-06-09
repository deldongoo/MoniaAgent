using Microsoft.Extensions.AI;
using MoniaAgent;
using ModelContextProtocol.Client;

namespace MoniaAgentTest
{
    public class FileSystemAgent : Agent
    {
        public override string Name => "FileSystemAgent";
        public override string Specialty => "Secure file operations with configurable access controls";

        public override bool CanHandle(string task)
        {
            var keywords = new[] { 
                "read", "write", "edit", "file", "create", "list", "delete", 
                "directories", "folder", "files", "directory", "move", "search"
            };
            return keywords.Any(k => task.ToLower().Contains(k));
        }

        private static IMcpClient? _mcpClient;

        public FileSystemAgent(LLM llm) : base(llm, CreateFileSystemTools(), GetFileSystemGoal(), _mcpClient)
        {
        }
        
        private static IList<AITool> CreateFileSystemTools()
        {
            var registry = new ToolRegistry();
            
            // Create MCP client directly like in your example
            Task.Run(async () =>
            {
                try
                {
                    var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
                    {
                        Name = "filesystem",
                        Command = "npx",
                        Arguments = ["-y", "@modelcontextprotocol/server-filesystem", "C:\\Users\\serva\\source\\repos\\MoniaSandbox"]
                    });

                    _mcpClient = await McpClientFactory.CreateAsync(clientTransport);
                    
                    // Get tools for the registry (for tool list display)
                    var tools = await _mcpClient.ListToolsAsync();
                    registry.tools.AddRange(tools);
                    
                    Console.WriteLine($"Successfully connected to MCP filesystem server with {tools.Count()} tools");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not load FileSystem MCP server: {ex.Message}");
                }
            }).Wait(TimeSpan.FromSeconds(10)); // Wait max 10 seconds for MCP server startup
            
            return registry.GetAllTools();
        }
        
        private static string GetFileSystemGoal()
        {
            return @"You are a filesystem agent with access to file operations. 
You can:
- List directory contents using available tools
- Read and write files
- Create and delete directories
- Move files and directories
- Search for files
- Get file metadata

Use the available filesystem tools to complete user requests. Call task_complete when finished.";
        }
    }
}
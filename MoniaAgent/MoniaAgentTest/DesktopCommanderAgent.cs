using MoniaAgent;

namespace MoniaAgentTest
{
    public class DesktopCommanderAgent : Agent
    {
        public override string Name => "DesktopCommanderAgent";
        public override string Specialty => "Desktop automation and system commands";

        public override bool CanHandle(string task)
        {
            var keywords = new[] { 
                "desktop", "window", "application", "app", "open", "close", "launch", 
                "file", "folder", "screenshot", "system", "computer", "mouse", "click",
                "keyboard", "type", "automation", "command", "execute", "run"
            };
            return keywords.Any(k => task.ToLower().Contains(k));
        }

        public DesktopCommanderAgent(LLM llm) : base(llm, CreateDesktopTools(), GetDesktopGoal())
        {
        }
        
        private static IList<Tool> CreateDesktopTools()
        {
            var registry = new ToolRegistry();
            
            // Configure MCP server for desktop-commander
            var desktopCommanderServer = new McpServer
            {
                Name = "desktop-commander",
                Command = "npx",
                Args = new List<string> { "-y", "@wonderwhy-er/desktop-commander" }
            };
            
            // Register MCP server tools asynchronously
            Task.Run(async () =>
            {
                try
                {
                    await registry.RegisterMcpServerAsync(desktopCommanderServer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not load desktop-commander MCP server: {ex.Message}");
                }
            }).Wait(TimeSpan.FromSeconds(10)); // Wait max 10 seconds for MCP server startup
            
            return registry.GetAllTools();
        }
        
        private static string GetDesktopGoal()
        {
            return @"You are a desktop automation specialist AI assistant. You help users with:
- Opening and managing applications
- File and folder operations
- Taking screenshots
- System automation tasks
- Window management
- Keyboard and mouse automation

Always use the available desktop-commander tools to perform these operations. 
Be careful with destructive operations and ask for confirmation when needed.
Provide clear explanations of what actions you're taking.";
        }
    }
}
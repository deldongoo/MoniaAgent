using Microsoft.Extensions.AI;
using MoniaAgent;

namespace MoniaAgentTest
{
    public class McpTimeAgent : Agent
    {
        public override string Name => "McpTimeAgent";
        public override string Specialty => "Time and date queries using MCP Time server";

        public override bool CanHandle(string task)
        {
            var keywords = new[] { 
                "time", "heure", "date", "hour", "minute", "fuseau", "timezone", 
                "horaire", "when", "maintenant", "now", "aujourd'hui", "today"
            };
            return keywords.Any(k => task.ToLower().Contains(k));
        }

        public McpTimeAgent(LLM llm) : base(llm, CreateTimeTools(), GetTimeGoal())
        {
        }
        
        private static IList<AITool> CreateTimeTools()
        {
            var registry = new ToolRegistry();
            
            // Configure MCP server for time
            var mcpTimeServer = new McpServer
            {
                Name = "McpTime",
                Command = "dotnet",
                Args = new List<string> { "run", "--project", "C:\\Users\\serva\\source\\repos\\MoniaAgent\\MoniaAgent\\McpServerTime\\McpServerTime.csproj", "--no-build" }
            };
            
            // Register MCP server tools asynchronously
            Task.Run(async () =>
            {
                try
                {
                    await registry.RegisterMcpServerAsync(mcpTimeServer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not load MCP Time server: {ex.Message}");
                }
            }).Wait(TimeSpan.FromSeconds(10)); // Wait max 10 seconds for MCP server startup
            
            return registry.GetAllTools();
        }
        
        private static string GetTimeGoal()
        {
            return @"You are a time and date specialist agent with access to time-related operations.
You can:
- Get current time in various timezones
- Provide date and time information
- Handle timezone conversions
- Answer time-related queries in French or English

Use the available time tools to complete user requests. Call task_complete when finished.";
        }
    }
}
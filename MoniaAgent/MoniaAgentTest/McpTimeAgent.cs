using MoniaAgent.Agents;
using MoniaAgent.Configuration;

namespace MoniaAgentTest
{
    public class McpTimeAgent : SpecializedAgent
    {
        public McpTimeAgent(LLM llm) : base(llm)
        {
        }

        protected override AgentConfig Configure() => new()
        {
            Name = "McpTimeAgent",
            Specialty = "Time and date queries using MCP Time server",
            Keywords = new[] { 
                "time", "heure", "date", "hour", "minute", "fuseau", "timezone", 
                "horaire", "when", "maintenant", "now", "aujourd'hui", "today"
            },
            McpServers = new[] {
                new McpServer
                {
                    Name = "McpTime",
                    Command = "dotnet",
                    Args = new List<string> { "run", "--project", "C:\\Users\\serva\\source\\repos\\MoniaAgent\\MoniaAgent\\McpServerTime\\McpServerTime.csproj", "--no-build" }
                }
            },
            Goal = @"You are a time and date specialist agent with access to time-related operations.
                    You can:
                    - Get current time in various timezones
                    - Provide date and time information
                    - Handle timezone conversions
                    - Answer time-related queries in French or English

                    Use the available time tools to complete user requests. Call task_complete when finished."
        };
    }
}
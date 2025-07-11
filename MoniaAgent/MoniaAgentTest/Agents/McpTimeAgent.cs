using MoniaAgent.Agent;
using MoniaAgent.Agent.Inputs;
using MoniaAgent.Agent.Outputs;
using MoniaAgent.Configuration;

namespace MoniaAgentTest.Agents
{
    [AgentMetadata(
        Name = "McpTimeAgent",
        Specialty = "Time and date queries using MCP Time server",
        Keywords = new[] { 
            "time", "heure", "date", "hour", "minute", "fuseau", "timezone", 
            "horaire", "when", "maintenant", "now", "aujourd'hui", "today"
        },
        Goal = @"You are a time and date specialist agent with access to time-related operations.
You can:
- Get current time in various timezones
- Provide date and time information
- Handle timezone conversions
- Answer time-related queries in French or English

Use the available time tools to complete user requests."
    )]
    public class McpTimeAgent : TypedAgent<TextInput, TextOutput>
    {
        public McpTimeAgent(LLM llm) : base(llm)
        {
        }

        protected override AgentConfig ConfigureTools() => new()
        {
            McpServers = new[] {
                new McpServer
                {
                    Name = "McpTime",
                    Command = "dotnet",
                    Args = new List<string> { "run", "--project", "C:\\Users\\serva\\source\\repos\\MoniaAgent\\MoniaAgent\\McpServerTime\\McpServerTime.csproj", "--no-build" }
                }
            }
        };


        // Implement abstract method for string to output conversion
        protected override TextOutput ConvertResultToOutput(string finalLLMAnswer, ExecutionMetadata metadata)
        {
            return new TextOutput { Success = !finalLLMAnswer.Contains("Error") && !finalLLMAnswer.Contains("Failed"), Content = finalLLMAnswer, Metadata = metadata };
        }
    }
}
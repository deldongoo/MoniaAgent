using MoniaAgent.Core;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;
using MoniaAgent.Configuration;
using System.ComponentModel;

namespace MoniaAgentTest.Agents
{
    public class TimeAgent : TypedAgent<TextInput, TextOutput>
    {
        public TimeAgent(LLM llm) : base(llm)
        {
        }

        protected override AgentConfig Configure() => new()
        {
            Name = "TimeAgent",
            Specialty = "Time and scheduling queries",
            Keywords = new[] { "time", "date", "when", "schedule", "timezone" },
            ToolMethods = new Delegate[] { GetCurrentTime },
            Goal = "You are a time specialist AI assistant. You help users with time-related queries, timezone conversions, and scheduling. Always use the get_current_time tool when users ask about current time."
        };

        [Description("Gets the current date and time. Parameters: timezone (optional: 'utc', 'local', or timezone ID), format (optional: .NET datetime format string)")]
        private static string GetCurrentTime()
        {
            DateTime dateTime = DateTime.Now;
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss zzz");
        }


        // Implement abstract method for string to output conversion
        protected override TextOutput ConvertStringToOutput(string textResult, ExecutionMetadata metadata)
        {
            return new TextOutput
            {
                Success = !textResult.Contains("Error") && !textResult.Contains("Failed"),
                Content = textResult,
                Metadata = metadata
            };
        }
    }
}
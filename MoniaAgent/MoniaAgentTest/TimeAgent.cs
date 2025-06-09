using MoniaAgent.Agents;
using MoniaAgent.Configuration;
using System.ComponentModel;

namespace MoniaAgentTest
{
    public class TimeAgent : SpecializedAgent
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
    }
}
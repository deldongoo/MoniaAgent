using MoniaAgent.Core;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;
using MoniaAgent.Configuration;
using System.ComponentModel;
using System.Text.Json;

namespace MoniaAgentTest.Agents
{
    public class TimeOutput : TextOutput
    {
        public DateTime CurrentTime { get; set; }
        public string FormattedTime { get; set; } = string.Empty;
    }

    public class TimeAgent : TypedAgent<TextInput, TimeOutput>
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

        [Description("Return the current time in the system timezone")]
        private static TimeOutput GetCurrentTime()
        {
            var now = DateTime.Now;
            var formattedTime = now.ToString("yyyy-MM-dd HH:mm:ss zzz");
            return new TimeOutput
            {
                CurrentTime = now,
                FormattedTime = formattedTime,
                Content = $"Current time: {formattedTime}",
                Success = true
            };
        }


        // Implement abstract method for string to output conversion
        protected override TimeOutput ConvertResultToOutput(string finalLLMAnswer, ExecutionMetadata metadata)
        {
            var toolResult = metadata.FindToolResult("GetCurrentTime");
            if (!string.IsNullOrEmpty(toolResult))
            {
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                };
                var result = JsonSerializer.Deserialize<TimeOutput>(toolResult, options);
                if (result != null)
                {
                    // Preserve Content from tool - don't overwrite with LLM response
                    return result;
                }
            }

            return new TimeOutput
            {
                Success = false,
                Content = finalLLMAnswer,
                ErrorMessage = "No time result found"
                // Framework handles metadata assignment
            };
        }
    }
}
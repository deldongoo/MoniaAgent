using MoniaAgent;

namespace MoniaAgentTest
{
    public class TimeAgent : Agent
    {
        public override string Name => "TimeAgent";
        public override string Specialty => "Time and scheduling queries";

        public override bool CanHandle(string task)
        {
            var keywords = new[] { "time", "date", "when", "schedule", "timezone" };
            return keywords.Any(k => task.ToLower().Contains(k));
        }

        public TimeAgent(LLM llm) : base(llm, CreateTimeTools(), GetTimeGoal())
        {
        }
        
        private static IList<Tool> CreateTimeTools()
        {
            var registry = new ToolRegistry();
            registry.RegisterTool(new CurrentTimeTool());
            return registry.GetAllTools();
        }
        
        private static string GetTimeGoal()
        {
            return "You are a time specialist AI assistant. You help users with time-related queries, timezone conversions, and scheduling. Always use the get_current_time tool when users ask about current time.";
        }
    }
}
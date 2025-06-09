using System;

namespace MoniaAgent
{
    public class SimpleAgent : SpecializedAgent
    {
        private readonly string customGoal;
        
        public SimpleAgent(LLM llm, string goal) : base(llm)
        {
            customGoal = goal;
        }
        
        protected override AgentConfig Configure() => new()
        {
            Name = "SimpleAgent",
            Specialty = "General purpose assistant",
            Keywords = Array.Empty<string>(), // Accepts all tasks
            ToolMethods = Array.Empty<Delegate>(),
            McpServers = Array.Empty<McpServer>(),
            Goal = customGoal
        };
        
        public override bool CanHandle(string task) => true;
    }
}
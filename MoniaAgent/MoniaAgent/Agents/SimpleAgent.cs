using System;
using MoniaAgent.Configuration;
using MoniaAgent.Core;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;

namespace MoniaAgent.Agents
{
    public class SimpleAgent : TypedAgent<TextInput, TextOutput>
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
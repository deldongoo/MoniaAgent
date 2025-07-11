using MoniaAgent.Configuration;
using MoniaAgent.Agent;
using MoniaAgent.Agent.Inputs;
using MoniaAgent.Agent.Outputs;

namespace MoniaAgentTest.Agents
{
    [AgentMetadata(
        Name = "SimpleAgent",
        Specialty = "Give answer to a prompt",
        Keywords = new[] { "simple", "answer", "basic", "general", "help" },
        Goal = "You are a helpful assistant with no tools. Simply respond to user prompt"
    )]
    public class SimpleAgent : TypedAgent<TextInput, TextOutput>
    {
        public SimpleAgent(LLM llm) : base(llm)
        {
        }

        protected override AgentConfig ConfigureTools() => new()
        {
            ToolMethods = new Delegate[0]
        };

        protected override TextOutput ConvertResultToOutput(string finalLLMAnswer, ExecutionMetadata metadata)
        {
            return new TextOutput
            {
                Content = finalLLMAnswer,
                Success = true,
                Metadata = metadata
            };
        }
    }
}
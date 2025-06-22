using MoniaAgent.Configuration;
using MoniaAgent.Core;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;

namespace MoniaAgent.Agents
{
    public class SimpleAgent : TypedAgent<TextInput, TextOutput>
    {
        public SimpleAgent(LLM llm) : base(llm)
        {
        }

        protected override AgentConfig Configure() => new()
        {
            Name = "SimpleAgent",
            Specialty = "Give answer to a prompt",
            Keywords = new[] { "simple", "answer" },
            ToolMethods = new Delegate[0],
            Goal = "You are a helpful assistant with no tools. Simply respond to user prompt"
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
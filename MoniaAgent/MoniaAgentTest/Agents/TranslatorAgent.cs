using MoniaAgent.Configuration;
using MoniaAgent.Agent;
using MoniaAgent.Agent.Inputs;
using MoniaAgent.Agent.Outputs;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace MoniaAgentTest.Agents
{
    public class TranslationInput : AgentInput
    {
        public string Content { get; set; }
        public string TargetLanguage { get; set; }
    }

    [AgentMetadata(
        Name = "TranslatorAgent",
        Specialty = "Translate content from one language to another",
        Keywords = new[] { "translate", "translating", "translation", "language"},
        Goal = "You are a translation agent. Translate the content and specify the target language."
    )]
    public class TranslatorAgent : TypedAgent<TranslationInput, TextOutput>
    {
        public TranslatorAgent(LLM llm) : base(llm)
        {
        }

        protected override AgentConfig ConfigureTools() => new()
        {
            ToolMethods = Array.Empty<Delegate>(),
            UseStructuredOutput = true 
        };

        protected override string ConvertInputToPrompt(AgentInput input)
        {
            if (input is TranslationInput translationInput)
            {
                return $@"Translate {translationInput.Content} into {translationInput.TargetLanguage}. Return only translated content";
            }
            return base.ConvertInputToPrompt(input);
        }

        protected override TextOutput ConvertResultToOutput(string finalLLMAnswer, ExecutionMetadata metadata)
        {
            return new TextOutput
            {
                Metadata = metadata,
                Content = finalLLMAnswer,
                Success = true
            };
        }
    }
}
using MoniaAgent.Configuration;
using MoniaAgent.Core;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;
using System.Text.Json;

namespace MoniaAgentTest.Agents
{
    public class ContentSafetyOutput : AgentOutput
    {
        public bool IsSafe { get; set; }
        public string RiskLevel { get; set; } = "Low";
        public string Summary { get; set; } = string.Empty;
    }

    public class GuardRailAgent : TypedAgent<TextInput, ContentSafetyOutput>
    {
        public GuardRailAgent(LLM llm) : base(llm)
        {
        }

        protected override AgentConfig Configure() => new()
        {
            Name = "GuardRailAgent",
            Specialty = "Analyzes content for security risks and dangerous patterns",
            Keywords = new[] { "safety", "security", "malware", "dangerous", "risk", "threat" },
            ToolMethods = new Delegate[0],
            UseStructuredOutput = true, // Enable structured output
            Goal = @"You are a content safety expert. Analyze the provided content for:
                    1. Security threats (malware patterns, suspicious scripts, executable code)
                    2. Dangerous content (instructions for harmful activities)
                    3. Sensitive data exposure (passwords, API keys, personal info)
                    4. Malicious patterns (phishing attempts, social enginering)"
        };

        protected override string ConvertInputToPrompt(AgentInput input)
        {
            if (input is TextInput textInput)
            {
                return $@"Analyze this content for safety: {textInput.Prompt}

                Respond with ONLY a JSON object in this exact format:
                {{
                  ""isSafe"": true/false,
                  ""riskLevel"": ""Low|Medium|High|Critical"",
                  ""summary"": ""Brief explanation""
                }}";
            }
            return base.ConvertInputToPrompt(input);
        }

        protected override ContentSafetyOutput ConvertStringToOutput(string textResult, ExecutionMetadata metadata)
        {
            var output = new ContentSafetyOutput
            {
                Metadata = metadata,
                IsSafe = false,
                RiskLevel = "Critical",
                Summary = textResult
            };

            try
            {
                // For agents without tools, the JSON response is in the first LLM response from conversation history
                var firstLlmResponse = metadata.ConversationHistory?
                    .FirstOrDefault(step => step.Type == ConversationStepType.LlmResponse);

                string jsonContent = firstLlmResponse?.Content ?? textResult;

                // With UseStructuredOutput=true, the LLM response should be pure JSON
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var parsed = JsonSerializer.Deserialize<ContentSafetyOutput>(jsonContent, options);
                
                if (parsed != null)
                {
                    output.IsSafe = parsed.IsSafe;
                    output.RiskLevel = parsed.RiskLevel;
                    output.Summary = parsed.Summary;
                }
            }
            catch
            {}

            return output;
        }
    }
}
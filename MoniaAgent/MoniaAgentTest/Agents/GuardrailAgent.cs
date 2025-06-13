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
            Goal = @"You are a content safety expert. Analyze the provided content for:
                    1. Security threats (malware patterns, suspicious scripts, executable code)
                    2. Dangerous content (instructions for harmful activities)
                    3. Sensitive data exposure (passwords, API keys, personal info)
                    4. Malicious patterns (phishing attempts, social engineering)

                    Respond with a JSON object containing:
                    {
                      ""isSafe"": true/false,
                      ""riskLevel"": ""Low|Medium|High|Critical"",
                      ""summary"": ""Brief explanation""
                    }"
        };

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
                var jsonStart = textResult.IndexOf('{');
                var jsonEnd = textResult.LastIndexOf('}') + 1;

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonString = textResult.Substring(jsonStart, jsonEnd - jsonStart);
                    var json = JsonSerializer.Deserialize<JsonElement>(jsonString);

                    if (json.TryGetProperty("isSafe", out var isSafe))
                        output.IsSafe = isSafe.GetBoolean();

                    if (json.TryGetProperty("riskLevel", out var riskLevel))
                        output.RiskLevel = riskLevel.GetString() ?? "Low";

                    if (json.TryGetProperty("summary", out var summary))
                        output.Summary = summary.GetString() ?? textResult;
                }
            }
            catch
            {
                // Parsing failed, keep defaults (unsafe state)
            }

            return output;
        }
    }
}
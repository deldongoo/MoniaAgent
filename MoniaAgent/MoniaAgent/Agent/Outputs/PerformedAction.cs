using System;
using System.Collections.Generic;

namespace MoniaAgent.Agent.Outputs
{
    /// <summary>
    /// Represents a step in the conversation history (LLM response, tool call, or tool result)
    /// </summary>
    public class ConversationStep
    {
        public ConversationStepType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ToolName { get; set; }
        public Dictionary<string, object?>? Arguments { get; set; }
        public string? Result { get; set; }
    }

    /// <summary>
    /// Types of conversation steps
    /// </summary>
    public enum ConversationStepType
    {
        LlmResponse,
        ToolCall,
        ToolResult
    }
}
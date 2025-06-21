using System;
using System.Collections.Generic;
using System.Linq;

namespace MoniaAgent.Core.Outputs
{
    /// <summary>
    /// Metadata about agent execution
    /// </summary>
    public class ExecutionMetadata
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public string AgentName { get; set; } = string.Empty;
        public List<ConversationStep> ConversationHistory { get; set; } = new();

        /// <summary>
        /// Finds the most recent tool result for a specific tool name
        /// </summary>
        public string? FindToolResult(string toolName)
        {
            return ConversationHistory
                .Where(step => step.Type == ConversationStepType.ToolResult && step.ToolName == toolName)
                .OrderByDescending(step => step.Timestamp)
                .FirstOrDefault()?.Result;
        }

        /// <summary>
        /// Finds all tool results for a specific tool name
        /// </summary>
        public List<string> FindAllToolResults(string toolName)
        {
            return ConversationHistory
                .Where(step => step.Type == ConversationStepType.ToolResult && step.ToolName == toolName)
                .OrderBy(step => step.Timestamp)
                .Select(step => step.Result ?? string.Empty)
                .ToList();
        }
    }
}
using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.ComponentModel;

namespace MoniaAgent.Tools
{
    public static class TaskCompleteTool
    {
        public const string TOOL_NAME = "TaskComplete";
        
        public static AITool Create()
        {
            return AIFunctionFactory.Create(
                (string? actions) => GenerateTaskSummary(actions),
                TOOL_NAME,
                $"Call this when the task is completed successfully. Use {TOOL_NAME} to indicate completion.");
        }

        public static string GenerateTaskSummary(string? actions)
        {
            return "Completed";
        }
    }
}
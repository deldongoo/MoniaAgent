using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.ComponentModel;

namespace MoniaAgent.Tools
{
    internal static class TaskCompleteTool
    {
        public static AITool Create()
        {
            return AIFunctionFactory.Create(
                (string? actions) => GenerateTaskSummary(actions),
                "TaskComplete",
                "Call this when the task is completed successfully.");
        }

        public static string GenerateTaskSummary(string? actions)
        {
            return "Completed";
        }
    }
}
using Microsoft.Extensions.AI;
using System.Collections.Generic;

namespace MoniaAgent
{
    public static class TaskCompleteTool
    {
        public static AITool Create()
        {
            return AIFunctionFactory.Create(
                (string? actions) => GenerateTaskSummary(actions),
                "task_complete",
                "Call this when the task is completed successfully. Optionally provide a summary of actions performed. This discussion ends here, no more tool calls");
        }

        public static string GenerateTaskSummary(string? actions)
        {
            if (string.IsNullOrEmpty(actions))
            {
                return "Task completed successfully.";
            }

            return $"Task completed successfully. Summary of actions performed:\n{actions}";
        }
    }
}
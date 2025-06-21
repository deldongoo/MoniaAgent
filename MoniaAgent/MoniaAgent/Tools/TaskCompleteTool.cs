using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.ComponentModel;

namespace MoniaAgent.Tools
{
    public static class TaskCompleteTool
    {
        public const string TOOL_NAME = "TaskCompleted";
        
        public static AITool Create()
        {
            return AIFunctionFactory.Create(
                (string? actions) => TaskCompleted(actions),
                TOOL_NAME,
                $"Call this when you have completed the user's task. IMPORTANT: The 'finalAnswer' parameter must contain your complete, final response that will be shown to the user. Include all conclusions, results, explanations, and any other information the user needs. Do not just summarize actions - provide the actual answer to their request.");
        }

        [Description("Indicate completion of the agent task. Provide your complete final response to the user in finalAnswer - this will be the main content shown to them.")]
        public static string TaskCompleted(string? finalAnswer)
        {
            return finalAnswer;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using MoniaAgent.Core.Outputs;

namespace MoniaAgent.Workflows
{
    public class WorkflowExecutionResult
    {
        public string WorkflowName { get; set; } = string.Empty;
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public List<StepExecutionResult> StepResults { get; set; } = new();
        public WorkflowContext? Context { get; set; }
        
        /// <summary>
        /// Get the final result from the last successful step
        /// </summary>
        public AgentOutput? FinalResult => StepResults
            .Where(s => s.Success && s.Result != null)
            .LastOrDefault()?.Result;
        
        /// <summary>
        /// Get typed final result
        /// </summary>
        public T? GetFinalResult<T>() where T : AgentOutput
        {
            return FinalResult as T;
        }
    }
}
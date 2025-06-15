using System;
using MoniaAgent.Core.Outputs;

namespace MoniaAgent.Workflows
{
    public class StepExecutionResult
    {
        public WorkflowStepBase Step { get; set; } = null!;
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public AgentOutput? Result { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public int AttemptNumber { get; set; } = 1;
        
        public string StepName { get; set; } = string.Empty;
        public string AgentName => Step.AgentName;
    }
}
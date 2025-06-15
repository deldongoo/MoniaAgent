using System;
using MoniaAgent.Core.Outputs;

namespace MoniaAgent.Workflows
{
    public class ConditionalStep : WorkflowStepBase
    {
        public Func<AgentOutput, bool>? Condition { get; set; }
    }
}
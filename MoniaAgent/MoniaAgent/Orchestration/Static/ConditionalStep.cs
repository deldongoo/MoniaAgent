using System;
using MoniaAgent.Agent.Outputs;

namespace MoniaAgent.Orchestration.Static
{
    public class ConditionalStep : WorkflowStepBase
    {
        public Func<AgentOutput, bool>? Condition { get; set; }
    }
}
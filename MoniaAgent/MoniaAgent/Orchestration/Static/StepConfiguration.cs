using System;
using System.Collections.Generic;
using MoniaAgent.Agent.Inputs;
using MoniaAgent.Agent.Outputs;

namespace MoniaAgent.Orchestration.Static
{
    public class StepConfiguration
    {
        public Func<AgentOutput?, AgentInput>? InputTransformer { get; set; }
        public int MaxRetries { get; set; } = 1;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
        public bool ContinueOnError { get; set; } = false;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
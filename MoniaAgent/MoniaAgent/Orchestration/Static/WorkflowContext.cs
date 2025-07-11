using System;
using System.Collections.Generic;
using MoniaAgent.Agent.Outputs;

namespace MoniaAgent.Orchestration.Static
{
    public class WorkflowContext
    {
        public string WorkflowId { get; set; } = Guid.NewGuid().ToString();
        public Dictionary<string, object> SharedData { get; set; } = new();
        public List<string> ExecutionPath { get; set; } = new();
        public Dictionary<string, AgentOutput> PreviousResults { get; set; } = new();
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
    }
}
using System;
using System.Collections.Generic;
using MoniaAgent.Core;
using MoniaAgent.Core.Outputs;

namespace MoniaAgent.Workflows
{
    public class WorkflowBuilder
    {
        private readonly List<WorkflowStepBase> steps = new();
        private readonly Dictionary<string, Agent> agents = new();
        private string? name;
        
        public WorkflowBuilder WithName(string workflowName)
        {
            name = workflowName;
            return this;
        }
        
        public WorkflowBuilder RegisterAgent(Agent agent)
        {
            agents[agent.Name] = agent;
            return this;
        }
        
        public WorkflowBuilder AddStep(string agentName, Action<StepConfiguration>? configure = null)
        {
            var config = new StepConfiguration();
            configure?.Invoke(config);
            
            steps.Add(new WorkflowStep
            {
                AgentName = agentName,
                Configuration = config
            });
            
            return this;
        }
        
        public WorkflowBuilder AddConditionalStep(
            string agentName, 
            Func<AgentOutput, bool> condition,
            Action<StepConfiguration>? configure = null)
        {
            var config = new StepConfiguration();
            configure?.Invoke(config);
            
            steps.Add(new ConditionalStep
            {
                AgentName = agentName,
                Condition = condition,
                Configuration = config
            });
            
            return this;
        }
        
        public Workflow Build()
        {
            if (string.IsNullOrEmpty(name))
                throw new InvalidOperationException("Workflow name is required");
                
            return new Workflow(name, steps, agents);
        }
    }
}
namespace MoniaAgent.Orchestration.Static
{
    public abstract class WorkflowStepBase
    {
        public string AgentName { get; set; } = string.Empty;
        public StepConfiguration Configuration { get; set; } = new();
    }
}
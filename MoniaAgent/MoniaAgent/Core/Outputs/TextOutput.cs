namespace MoniaAgent.Core.Outputs
{
    /// <summary>
    /// Simple text output
    /// </summary>
    public class TextOutput : AgentOutput
    {
        public string Content { get; set; } = string.Empty;
    }
}
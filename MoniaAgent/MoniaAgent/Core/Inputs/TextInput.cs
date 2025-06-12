namespace MoniaAgent.Core.Inputs
{
    /// <summary>
    /// Simple text prompt input
    /// </summary>
    public class TextInput : AgentInput
    {
        public string Prompt { get; set; } = string.Empty;
        
        public TextInput() { }
        public TextInput(string prompt) => Prompt = prompt;
        
        // Implicit conversion from string for backward compatibility
        public static implicit operator TextInput(string prompt) => new(prompt);
    }
}
using MoniaAgent.Agent.Inputs;

namespace MoniaAgentTest.Inputs
{
    /// <summary>
    /// Input for file-related operations
    /// </summary>
    public class FileInput : AgentInput
    {
        public string FilePath { get; set; } = string.Empty;
        public string? Content { get; set; }
        public FileOperation Operation { get; set; } = FileOperation.Read;
        
        public FileInput() { }
        public FileInput(string filePath, FileOperation operation = FileOperation.Read)
        {
            FilePath = filePath;
            Operation = operation;
        }
        
        // Implicit conversion from string for file path
        public static implicit operator FileInput(string filePath) => new(filePath);
    }

    /// <summary>
    /// File operations supported by file agents
    /// </summary>
    public enum FileOperation
    {
        Read,
        Write,
        Delete,
        Create,
        Move,
        Copy
    }
}
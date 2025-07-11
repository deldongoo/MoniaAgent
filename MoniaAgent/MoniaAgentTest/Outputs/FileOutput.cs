using MoniaAgent.Agent.Inputs;
using MoniaAgent.Agent.Outputs;
using MoniaAgentTest.Inputs;

namespace MoniaAgentTest.Outputs
{
    /// <summary>
    /// Result from file-related operations
    /// </summary>
    public class FileOutput : AgentOutput
    {
        // Content property is inherited from AgentOutput
        
        /// <summary>
        /// Path of the file that was operated on
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// The operation that was performed
        /// </summary>
        public FileOperation Operation { get; set; } = FileOperation.Read;
        
        /// <summary>
        /// File size in bytes (if applicable)
        /// </summary>
        public long? FileSize { get; set; }
        
        /// <summary>
        /// Last modified timestamp (if applicable)
        /// </summary>
        public DateTime? LastModified { get; set; }
    }
}
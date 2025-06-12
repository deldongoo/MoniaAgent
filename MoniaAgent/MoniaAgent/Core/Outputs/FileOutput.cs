using MoniaAgent.Core.Inputs;

namespace MoniaAgent.Core.Outputs
{
    /// <summary>
    /// Result from file-related operations
    /// </summary>
    public class FileOutput : AgentOutput
    {
        /// <summary>
        /// File content (for read operations) or result message
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
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
using System;
using System.Collections.Generic;

namespace MoniaAgent.Core.Outputs
{
    /// <summary>
    /// Represents a tool action that was performed during agent execution
    /// </summary>
    public class PerformedAction
    {
        public string ToolName { get; set; } = string.Empty;
        public Dictionary<string, object?> Arguments { get; set; } = new();
        public string Result { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
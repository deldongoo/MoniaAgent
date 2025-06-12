using MoniaAgent.Core;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;
using MoniaAgent.Configuration;

namespace MoniaAgentTest
{
    public class McpDesktopCommanderAgent : TypedAgent<TextInput, TextOutput>
    {
        public McpDesktopCommanderAgent(LLM llm) : base(llm)
        {
        }

        protected override AgentConfig Configure() => new()
        {
            Name = "McpDesktopCommanderAgent",
            Specialty = "Tools for coding, file access, terminal commands, DevOps, and automation",
            Keywords = new[] { 
                "read", "write", "edit", "file", "create", "list", "delete", 
                "directories", "folder", "files", "directory", "move", "search", "code", "execute", "launch", "debug"
            },
            McpServers = new[] {
                new McpServer
                {
                    Name = "desktop-commander",
                    Command = "npx",
                    Args = new List<string> { "-y", "@wonderwhy-er/desktop-commander" }
                }
            },
            Goal = @"You are a coding agent with access to file operations. 
                    You can:
                    - List directory contents using available tools
                    - Read and write files
                    - Create and delete directories
                    - Move files and directories
                    - Search for files
                    - Execute commands
                    - Code programs

                    Use the available desktop-commander tools to complete user requests. Call task_complete when finished."
        };


        // Implement abstract method for string to output conversion
        protected override TextOutput ConvertStringToOutput(string textResult, ExecutionMetadata metadata)
        {
            return new TextOutput { Success = !textResult.Contains("Error") && !textResult.Contains("Failed"), Content = textResult, Metadata = metadata };
        }
    }
}
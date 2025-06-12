using MoniaAgent.Core;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;
using MoniaAgent.Configuration;

namespace MoniaAgentTest
{
    public class FileSystemAgent : TypedAgent<TextInput, TextOutput>
    {
        public FileSystemAgent(LLM llm) : base(llm)
        {
        }

        protected override AgentConfig Configure() => new()
        {
            Name = "FileSystemAgent",
            Specialty = "Secure file operations with configurable access controls",
            Keywords = new[] { 
                "read", "write", "edit", "file", "create", "list", "delete", 
                "directories", "folder", "files", "directory", "move", "search"
            },
            McpServers = new[] {
                new McpServer
                {
                    Name = "filesystem",
                    Command = "npx",
                    Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", "C:\\Users\\serva\\source\\repos\\MoniaSandbox" }
                }
            },
            Goal = @"You are a filesystem agent with access to file operations. 
                    You can:
                    - List directory contents using available tools
                    - Read and write files
                    - Create and delete directories
                    - Move files and directories
                    - Search for files
                    - Get file metadata

                    Use the available filesystem tools to complete user requests. Call task_complete when finished."
        };


        // Implement abstract method for string to output conversion
        protected override TextOutput ConvertStringToOutput(string textResult, ExecutionMetadata metadata)
        {
            return new TextOutput { Success = !textResult.Contains("Error") && !textResult.Contains("Failed"), Content = textResult, Metadata = metadata };
        }
    }
}
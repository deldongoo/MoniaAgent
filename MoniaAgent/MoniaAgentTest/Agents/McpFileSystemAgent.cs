using MoniaAgent.Core;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;
using MoniaAgent.Configuration;

namespace MoniaAgentTest.Agents
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

                    Use the available filesystem tools to complete user requests."
        };


        // Implement abstract method for string to output conversion
        protected override TextOutput ConvertResultToOutput(string finalLLMAnswer, ExecutionMetadata metadata)
        {
            return new TextOutput { Success = !finalLLMAnswer.Contains("Error") && !finalLLMAnswer.Contains("Failed"), Content = finalLLMAnswer, Metadata = metadata };
        }
    }
}
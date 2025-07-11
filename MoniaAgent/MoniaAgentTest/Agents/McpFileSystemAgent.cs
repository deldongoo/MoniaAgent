using MoniaAgent.Agent;
using MoniaAgent.Agent.Inputs;
using MoniaAgent.Agent.Outputs;
using MoniaAgent.Configuration;

namespace MoniaAgentTest.Agents
{
    [AgentMetadata(
        Name = "FileSystemAgent",
        Specialty = "Secure file operations with configurable access controls",
        Keywords = new[] { 
            "read", "write", "edit", "file", "create", "list", "delete", 
            "directories", "folder", "files", "directory", "move", "search", "filesystem"
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
    )]
    public class FileSystemAgent : TypedAgent<TextInput, TextOutput>
    {
        public FileSystemAgent(LLM llm) : base(llm)
        {
        }

        protected override AgentConfig ConfigureTools() => new()
        {
            McpServers = new[] {
                new McpServer
                {
                    Name = "filesystem",
                    Command = "npx",
                    Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", "C:\\Users\\serva\\source\\repos\\MoniaSandbox" }
                }
            }
        };


        // Implement abstract method for string to output conversion
        protected override TextOutput ConvertResultToOutput(string finalLLMAnswer, ExecutionMetadata metadata)
        {
            return new TextOutput { Success = !finalLLMAnswer.Contains("Error") && !finalLLMAnswer.Contains("Failed"), Content = finalLLMAnswer, Metadata = metadata };
        }
    }
}
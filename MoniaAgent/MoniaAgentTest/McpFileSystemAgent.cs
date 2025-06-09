using MoniaAgent;

namespace MoniaAgentTest
{
    public class FileSystemAgent : SpecializedAgent
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
    }
}
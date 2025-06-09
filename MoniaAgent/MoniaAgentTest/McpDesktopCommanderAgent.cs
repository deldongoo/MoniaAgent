using MoniaAgent.Agents;
using MoniaAgent.Configuration;

namespace MoniaAgentTest
{
    public class McpDesktopCommanderAgent : SpecializedAgent
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
    }
}
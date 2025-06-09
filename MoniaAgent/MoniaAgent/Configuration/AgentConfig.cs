using System;

namespace MoniaAgent.Configuration
{
    public class AgentConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string[] Keywords { get; set; } = Array.Empty<string>();
        public string Goal { get; set; } = string.Empty;
        
        // Pour méthodes avec attributs Description
        public Delegate[] ToolMethods { get; set; } = Array.Empty<Delegate>();
        
        // Pour serveurs MCP
        public McpServer[] McpServers { get; set; } = Array.Empty<McpServer>();
    }
}
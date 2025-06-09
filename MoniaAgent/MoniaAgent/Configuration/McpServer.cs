using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoniaAgent.Configuration
{
    public class McpServer
    {
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public List<string> Args { get; set; } = new List<string>();
        public string Url { get; set; } = string.Empty;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new ArgumentException("MCP server name cannot be null or empty", nameof(Name));
            
            bool hasCommand = !string.IsNullOrWhiteSpace(Command);
            bool hasUrl = !string.IsNullOrWhiteSpace(Url);
            
            if (!hasCommand && !hasUrl)
                throw new ArgumentException("MCP server must have either Command or Url specified");
            
            if (hasCommand && hasUrl)
                throw new ArgumentException("MCP server cannot have both Command and Url specified");
            
            if (hasUrl && !Uri.TryCreate(Url, UriKind.Absolute, out _))
                throw new ArgumentException("MCP server URL must be a valid URI", nameof(Url));
        }

        public IClientTransport GetTransport()
        {
            IClientTransport? clientTransport = null;
            switch (String.IsNullOrEmpty(Url)) {
                case true:
                    clientTransport = new StdioClientTransport(new StdioClientTransportOptions
                    {
                        Name = Name,
                        Command = Command,
                        Arguments = new List<string>(Args),
                    });
                    break;
                case false:
                    clientTransport = new SseClientTransport(new SseClientTransportOptions
                    {
                        Name = Name,
                        Endpoint = new Uri(Url),
                        TransportMode = HttpTransportMode.AutoDetect,
                    });
                    break;
            }    
            return clientTransport!; 
        }
    }
}

# MoniaAgent Framework

> 🚧 **Work in Progress** - This framework is currently under active development

A powerful .NET 8 multi-agent framework that integrates with Model Context Protocol (MCP) servers and AI language models to create specialized AI agents with typed inputs/outputs and workflow capabilities.

## 🌟 Features

- **Multi-Agent Architecture** - Create specialized agents with specific capabilities
- **MCP Integration** - Seamless integration with Model Context Protocol servers
- **Typed I/O System** - Strongly typed inputs and outputs for type-safe agent communication
- **Agent Orchestration** - Automatic routing of tasks to the most appropriate agent
- **Tool Registry** - Support for both local tools and external MCP server tools
- **Workflow Support** - Chain agents together in complex workflows (planned)
- **Configuration-Driven** - Easy agent configuration via JSON and code

## 🏗️ Architecture

The framework consists of three main projects:

### Core Components

- **MoniaAgent** - Core framework library with agent abstractions
- **MoniaAgentTest** - Test console application with example implementations
- **McpServerTime** - Sample MCP server providing time-related tools

### Key Classes

- **`Agent`** - Base class handling LLM communication and tool execution
- **`TypedAgent<TInput, TOutput>`** - Strongly typed agent base class
- **`AgentOrchestrator`** - Routes tasks to appropriate agents
- **`ToolRegistry`** - Manages local and MCP server tools

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 SDK
- An OpenAI-compatible API key (OpenRouter, OpenAI, etc.)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/yourusername/MoniaAgent.git
cd MoniaAgent
```

2. Set up configuration:
```bash
cp MoniaAgentTest/config.template.json MoniaAgentTest/config.json
```

3. Edit `config.json` with your API credentials:
```json
{
  "LLM": {
    "BaseUrl": "https://openrouter.ai/api/v1",
    "ApiKey": "your-actual-api-key-here",
    "Model": "openai/gpt-4o"
  }
}
```

4. Build and run:
```bash
dotnet build
dotnet run --project MoniaAgentTest
```

## 📖 Usage Examples

### Creating a Simple Agent

```csharp
public class MyAgent : TypedAgent<TextInput, TextOutput>
{
    public MyAgent(LLM llm) : base(llm) { }

    protected override AgentConfig Configure() => new()
    {
        Name = "MyAgent",
        Specialty = "Specialized task handler",
        Keywords = new[] { "keyword1", "keyword2" },
        ToolMethods = new Delegate[] { MyToolMethod },
        Goal = "You are a helpful specialist agent."
    };

    [Description("Description of what this tool does")]
    private static string MyToolMethod(string input)
    {
        return $"Processed: {input}";
    }

    protected override TextOutput ConvertStringToOutput(string textResult, ExecutionMetadata metadata)
    {
        return new TextOutput
        {
            Success = !textResult.Contains("Error"),
            Content = textResult,
            Metadata = metadata
        };
    }
}
```

### Using MCP Servers

```csharp
protected override AgentConfig Configure() => new()
{
    Name = "FileAgent",
    Specialty = "File operations",
    Keywords = new[] { "file", "read", "write" },
    McpServers = new[] {
        new McpServer
        {
            Name = "filesystem",
            Command = "npx",
            Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", "/path/to/workspace" }
        }
    },
    Goal = "You are a file management specialist."
};
```

### Agent Orchestration

```csharp
var orchestrator = new AgentOrchestrator();

var fileAgent = new FileReaderAgent(llm);
await fileAgent.ConnectAsync();
orchestrator.RegisterAgent(fileAgent);

var timeAgent = new TimeAgent(llm);
await timeAgent.ConnectAsync();
orchestrator.RegisterAgent(timeAgent);

// Automatically routes to the appropriate agent
var result = await orchestrator.Execute("What time is it?");
```

## 🛠️ Available Example Agents

- **FileReaderAgent** - Reads and analyzes file contents
- **GuardRailAgent** - Content safety and security analysis
- **TimeAgent** - Time and date operations
- **McpTimeAgent** - Time operations via MCP server
- **FileSystemAgent** - File system operations via MCP
- **McpDesktopCommanderAgent** - System commands and automation

## 🔧 Configuration

### LLM Configuration

```json
{
  "LLM": {
    "BaseUrl": "https://api.openai.com/v1",
    "ApiKey": "your-api-key",
    "Model": "gpt-4"
  }
}
```

### Agent Configuration

```csharp
new AgentConfig
{
    Name = "AgentName",
    Specialty = "What this agent specializes in",
    Keywords = new[] { "routing", "keywords" },
    ToolMethods = new Delegate[] { /* local methods */ },
    McpServers = new McpServer[] { /* MCP servers */ },
    Goal = "System prompt for the agent"
}
```

## 📋 Typed I/O System

The framework supports strongly typed inputs and outputs:

### Input Types
- **TextInput** - Simple text prompts
- **FileInput** - File operations
- **QueryInput** - Structured queries (planned)
- **BatchInput** - Batch processing (planned)

### Output Types
- **TextOutput** - Simple text responses
- **FileOutput** - File operation results
- **ContentSafetyOutput** - Security analysis results
- **ValidationResult** - Data validation results (planned)

## 🗺️ Roadmap

- [ ] **Workflow System** - Chain agents in complex workflows
- [ ] **Advanced Input Types** - Batch processing, structured queries
- [ ] **Result Caching** - Performance optimization
- [ ] **Parallel Execution** - Concurrent agent operations
- [ ] **Monitoring & Telemetry** - Observability features
- [ ] **Plugin System** - Extended agent capabilities
- [ ] **Web API** - REST API for agent interactions

## 🤝 Contributing

This project is in active development. Contributions are welcome!

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📋 Development Commands

```bash
# Build the solution
dotnet build

# Run tests
dotnet run --project MoniaAgentTest

# Run MCP time server
dotnet run --project McpServerTime

# Clean build artifacts
dotnet clean
```

## 📦 Dependencies

- **Microsoft.Extensions.AI.OpenAI** (9.5.0-preview) - AI framework integration
- **ModelContextProtocol** (0.2.0-preview.3) - MCP client/server functionality
- **OpenAI** (2.2.0-beta.4) - OpenAI API client

## 🔒 Security

- Configuration files with API keys are excluded from git
- Use the provided `config.template.json` for setup
- Never commit actual API keys to the repository

## 📄 License

This project is licensed under the MIT License

## 🆘 Support

This framework is under active development. For questions or issues:

1. Check the existing issues
2. Create a new issue with detailed description
3. Include code samples and error messages

---

**Note**: This framework is designed to be easy to implement and extend. The guiding principle is simplicity and modularity - agents should be straightforward to create and deploy outside the framework.

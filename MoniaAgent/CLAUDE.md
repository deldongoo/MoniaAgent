# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MoniaAgent is a .NET 8 multi-agent framework that integrates with Model Context Protocol (MCP) servers and AI language models. The solution consists of three main projects:

1. **MoniaAgent** - Core agent framework library
2. **MoniaAgentTest** - Test console application with example agent implementations
3. **McpServerTime** - Sample MCP server providing time-related tools

## Solution Structure

```
MoniaAgent/
├── Core/
│   ├── Agent.cs              # Base agent implementation
│   ├── IAgent.cs             # Agent interface
│   └── AgentOrchestrator.cs  # Task routing system
├── Agents/
│   └── SpecializedAgent.cs   # Abstract specialized agent base
├── Configuration/
│   └── AgentConfig.cs        # Agent configuration model
├── Tools/
│   ├── ToolRegistry.cs       # Tool management system
│   └── TaskCompleteTool.cs   # Built-in completion tool
└── Extensions/
    └── ServiceCollectionExtensions.cs  # DI setup

MoniaAgentTest/
├── Agents/                   # Example agent implementations
├── config.template.json      # Configuration template
└── Program.cs               # Console application entry

McpServerTime/
└── Program.cs               # MCP server implementation
```

## Architecture

### Core Components

- **Agent (Core/Agent.cs)** - Base class handling LLM communication, tool execution, and MCP client integration
- **IAgent (Core/IAgent.cs)** - Interface defining agent capabilities (Name, Specialty, Execute, CanHandle)
- **SpecializedAgent (Agents/SpecializedAgent.cs)** - Abstract base for creating specialized agents with specific configurations
- **AgentOrchestrator (Core/AgentOrchestrator.cs)** - Routes tasks to the most appropriate agent based on specialties

### Agent Configuration System

Agents are configured via **AgentConfig** which specifies:
- Name and specialty description
- Keywords for task routing
- Tool methods (local functions)
- MCP servers to connect to
- System goal/prompt

### Tool Integration

- **ToolRegistry (Tools/ToolRegistry.cs)** - Manages both local AI tools and MCP server tools
- Built-in **TaskCompleteTool** automatically added to all agents
- Supports both local method-based tools and remote MCP tools

## Configuration Setup

### First-time Setup
1. Copy the configuration template:
   ```bash
   cp MoniaAgentTest/config.template.json MoniaAgentTest/config.json
   ```
2. Edit `MoniaAgentTest/config.json` with your actual LLM credentials:
   ```json
   {
     "LLM": {
       "BaseUrl": "https://openrouter.ai/api/v1",
       "ApiKey": "your-actual-api-key-here",
       "Model": "openai/gpt-4o"
     }
   }
   ```

**Important**: The `config.json` file is excluded from git to protect sensitive API keys. Always use the template file for new setups.

## Common Development Commands

### Build and Run
```bash
# Build entire solution
dotnet build

# Run test console application (requires config.json setup)
dotnet run --project MoniaAgentTest

# Run MCP time server
dotnet run --project McpServerTime

# Build and run specific project
dotnet build MoniaAgent
dotnet run --project MoniaAgentTest --configuration Debug
```

### Development Workflow
```bash
# Restore NuGet packages
dotnet restore

# Clean build artifacts
dotnet clean

# Format code (if configured)
dotnet format

# Run solution-wide build
dotnet build --configuration Release
```

### Testing Commands
```bash
# Run all tests (if test projects exist)
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Build without running
dotnet build --no-run
```

## Key Dependencies

- **Microsoft.Extensions.AI.OpenAI** (9.5.0-preview) - AI framework integration
- **ModelContextProtocol** (0.2.0-preview.3) - MCP client/server functionality
- **OpenAI** (2.2.0-beta.4) - OpenAI API client

## Agent Development Patterns

### Creating Specialized Agents
1. **Inherit from SpecializedAgent**: Create new agent class extending `SpecializedAgent`
2. **Implement Configure()**: Define agent configuration including name, specialty, keywords, and tools
3. **Add Tool Methods**: Register local methods as tools via `AgentConfig.ToolMethods`
4. **Configure MCP Servers**: Add MCP server connections via `AgentConfig.McpServers`
5. **Define Keywords**: Set routing keywords in `AgentConfig.Keywords` for orchestrator

### Tool Development
- **Local Tools**: Create methods and register them in `AgentConfig.ToolMethods`
- **MCP Tools**: Implement MCP server or connect to existing MCP servers
- **Tool Discovery**: Tools are automatically discovered and registered by `ToolRegistry`
- **Tool Execution**: Framework handles both local and remote tool execution transparently

### Task Routing
- **Orchestrator**: `AgentOrchestrator` routes tasks based on agent specialties and keywords
- **CanHandle**: Agents implement `CanHandle()` method for task suitability evaluation
- **Fallback**: Framework provides fallback mechanisms for unhandled tasks

## MCP Server Integration

MCP servers provide external tools to agents. The framework:
- Automatically discovers and registers MCP tools
- Routes tool calls to appropriate MCP servers
- Handles both local and remote tool execution transparently
- Supports multiple MCP servers per agent
- Manages MCP client connections and lifecycle

### MCP Server Development
- **Server Implementation**: Extend MCP server base classes
- **Tool Registration**: Register tools with MCP server
- **Client Connection**: Framework automatically connects to configured MCP servers
- **Error Handling**: Built-in error handling for MCP communication

## Agent Execution Flow

1. **Agent Creation**: Agent is instantiated with LLM configuration and auto-connects during construction
2. **Task Reception**: Agent receives task prompt via `ExecuteAsync()` method
3. **Multi-turn Conversation**: Executes conversation with tool calls and responses
4. **Tool Processing**: Processes both local AI tools and MCP server tools
5. **Completion Detection**: Returns result when task completion is detected or max turns reached
6. **Resource Cleanup**: Properly disposes of resources and connections

### Basic Usage Pattern
```csharp
// Simple usage - connection is automatic
var agent = new FileReaderAgent(llm);
var result = await agent.ExecuteAsync("Read test-file.txt");

// Works with specialized inputs too
var textInput = new TextInput("What time is it?");
var timeResult = await timeAgent.ExecuteAsync(textInput);
```

## Troubleshooting

### Common Issues

**Configuration Problems:**
- Verify `config.json` exists and contains valid API credentials
- Check that `config.template.json` is properly copied and modified
- Ensure API key has proper permissions for the configured model

**Agent Registration:**
- Confirm agents are properly registered in DI container
- Check that `Configure()` method is implemented correctly
- Verify agent keywords don't conflict with other agents

**MCP Connection Issues:**
- Ensure MCP server is running and accessible
- Check MCP server configuration in agent config
- Verify MCP server tools are properly registered

**Tool Execution:**
- Check that tool methods have proper signatures
- Verify tool registration in `AgentConfig.ToolMethods`
- Ensure tool methods are public and accessible

### Debug Tips
- Enable detailed logging in configuration
- Use debugger to step through agent execution
- Check MCP server logs for connection issues
- Verify LLM API responses and error messages

## Guiding Principles

- MoniaAgent should always be programmed in order to be easy to implement outside the framework.

## Coding Guidelines

- please do not create files with multiple class unless i ask for it

## Development Guidance

- **Do not use lambda expressions**
# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MoniaAgent is a .NET 8 multi-agent framework that integrates with Model Context Protocol (MCP) servers and AI language models. The solution consists of three main projects:

1. **MoniaAgent** - Core agent framework library
2. **MoniaAgentTest** - Test console application with example agent implementations
3. **McpServerTime** - Sample MCP server providing time-related tools

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
```

### Project Structure Commands
```bash
# Restore NuGet packages
dotnet restore

# Clean build artifacts
dotnet clean
```

## Key Dependencies

- **Microsoft.Extensions.AI.OpenAI** (9.5.0-preview) - AI framework integration
- **ModelContextProtocol** (0.2.0-preview.3) - MCP client/server functionality
- **OpenAI** (2.2.0-beta.4) - OpenAI API client

## Agent Development Patterns

1. **Creating Specialized Agents**: Inherit from `SpecializedAgent` and implement `Configure()` method
2. **Tool Registration**: Add methods to `AgentConfig.ToolMethods` or MCP servers to `AgentConfig.McpServers`
3. **Task Routing**: Define keywords in `AgentConfig.Keywords` for automatic task routing via orchestrator
4. **MCP Integration**: Use `IMcpClient` for external tool capabilities

## MCP Server Integration

MCP servers provide external tools to agents. The framework:
- Automatically discovers and registers MCP tools
- Routes tool calls to appropriate MCP servers
- Handles both local and remote tool execution transparently

## Agent Execution Flow

1. Agent receives task prompt
2. Connects to LLM with system goal
3. Executes multi-turn conversation with tool calls
4. Processes both local AI tools and MCP server tools
5. Returns result when task completion is detected or max turns reached

## Guiding Principles

- MoniaAgent should always be programmed in order to be easy to implement outside the framework.
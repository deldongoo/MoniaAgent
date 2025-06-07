using MoniaAgent;
using MoniaAgentTest;

var llm = new LLM
{
    BaseUrl = "https://openrouter.ai/api/v1",
    ApiKey = "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",
    Model = "openai/gpt-4o"
};

// Create orchestrator and register agents
var orchestrator = new AgentOrchestrator();

var timeAgent = new TimeAgent(llm);
await timeAgent.ConnectAsync();
orchestrator.RegisterAgent(timeAgent);

var desktopAgent = new DesktopCommanderAgent(llm);
await desktopAgent.ConnectAsync();
orchestrator.RegisterAgent(desktopAgent);

var generalAgent = new Agent(llm, new List<Tool>(), "You are a helpful AI assistant");
await generalAgent.ConnectAsync();
orchestrator.RegisterAgent(generalAgent);

// Test routing
Console.WriteLine("=== Testing Agent Orchestration ===");

var timeResponse = await orchestrator.Execute("What time is it right now?");
Console.WriteLine($"Time query → {timeResponse}");

var desktopResponse = await orchestrator.Execute("Can you take a screenshot of my desktop?");
Console.WriteLine($"Desktop query → {desktopResponse}");

var generalResponse = await orchestrator.Execute("Hello, how are you?");
Console.WriteLine($"General query → {generalResponse}");

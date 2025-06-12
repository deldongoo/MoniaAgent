using MoniaAgent.Agents;
using MoniaAgent.Configuration;
using MoniaAgent.Core;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;
using MoniaAgentTest;
using Microsoft.Extensions.Configuration;

// Load configuration from JSON file
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("config.json", optional: false, reloadOnChange: true)
    .Build();

var llm = new LLM();
configuration.GetSection("LLM").Bind(llm);

var mcpTimeAgent = new McpTimeAgent(llm);
await mcpTimeAgent.ConnectAsync();
var timeResult = await mcpTimeAgent.ExecuteAsync(new TextInput("Quelle heure est il?."));
Console.WriteLine($"Success: {timeResult.Success}");
if (timeResult is TextOutput textOutput)
{
    Console.WriteLine($"Content: {textOutput.Content}");
}
else
{
    Console.WriteLine($"Error: {timeResult.ErrorMessage}");
}

/*Console.WriteLine("\n=== Testing FileReaderAgent ===");
var fileReaderAgent = new FileReaderAgent(llm);
await fileReaderAgent.ConnectAsync();
var fileResult = await fileReaderAgent.ExecuteAsync(new FileInput("test-file.txt"));
Console.WriteLine($"File read success: {fileResult.Success}");
if (fileResult.Success)
{
    Console.WriteLine($"File Path: {fileResult.FilePath}");
    Console.WriteLine($"File Size: {fileResult.FileSize} bytes");
    Console.WriteLine($"Last Modified: {fileResult.LastModified}");
    Console.WriteLine($"Content:\n{fileResult.Content}");
}
else
{
    Console.WriteLine($"Error: {fileResult.ErrorMessage}");
}*/

/*var timeAgent = new TimeAgent(llm);
await timeAgent.ConnectAsync();
var timeResult = await timeAgent.Execute("Quelle heure est il?.");
Console.WriteLine($"{timeResult}"); */

/*var fileSystemAgent = new FileSystemAgent(llm);
await fileSystemAgent.ConnectAsync();
var fileSystemResponse = await fileSystemAgent.Execute("Crée un fichier txt avec le contenu 'coucou' à la racine de ton répertoire autorisé, " +
    "copie le (ne le coupe pas), " +
    "remplace 'coucou' par 'Hello World' dans le nouveau fichier. A la fin il doit y avoir 2 fichiers, 1 avec 'coucou', l'autre avec 'Hello World'");
Console.WriteLine($"McpFileSystemAgent result: {fileSystemResponse}");*/

/*var desktopCommanderAgent = new McpDesktopCommanderAgent(llm);
await desktopCommanderAgent.ConnectAsync();
var desktopCommanderResponse = await desktopCommanderAgent.Execute("Draw a butterfly in svg. Write the code into a html file. Launch it in a web browser.");
Console.WriteLine($"McpDesktopCommanderAgent result: {desktopCommanderResponse}");*/

/*// Create a simple agent that should use multiple turns
var agent = new SimpleAgent(llm,
    "You are a helpful assistant. Always think step by step and use the task_complete tool when you have finished the task completely.");

await agent.ConnectAsync();

Console.WriteLine("=== Testing Conversational Loop ===");

// Test 1: Simple task
Console.WriteLine("\n--- Test 1: Simple greeting ---");
var result1 = await agent.Execute("Say hello");
Console.WriteLine($"Result: {result1}");

// Test 2: Multi-step task
Console.WriteLine("\n--- Test 2: Multi-step calculation ---");
var result2 = await agent.Execute("Calculate 25 * 8, then add 17 to the result");
Console.WriteLine($"Result: {result2}");

// Test 3: Complex reasoning task
Console.WriteLine("\n--- Test 3: Complex task ---");
var result3 = await agent.Execute("Tell me 3 facts about France, then explain why Paris is the capital");
Console.WriteLine($"Result: {result3}");

// Test 4: Task without explicit completion request
Console.WriteLine("\n--- Test 4: Auto-completion ---");
var result4 = await agent.Execute("What is the largest planet in our solar system?");
Console.WriteLine($"Result: {result4}");*/

/*// Create orchestrator and register agents
var orchestrator = new AgentOrchestrator();

var timeAgent = new TimeAgent(llm);
await timeAgent.ConnectAsync();
orchestrator.RegisterAgent(timeAgent);

var generalAgent = new SimpleAgent(llm, "You are a helpful AI assistant. Always think step by step and use the task_complete tool when you have finished the task completely.");
await generalAgent.ConnectAsync();
orchestrator.RegisterAgent(generalAgent);

// Test routing
Console.WriteLine("=== Testing Agent Orchestration ===");

var timeResponse = await orchestrator.Execute("What time is it right now?");
Console.WriteLine($"Time query → {timeResponse}");


var generalResponse = await orchestrator.Execute("Hello, how are you?");
Console.WriteLine($"General query → {generalResponse}");*/


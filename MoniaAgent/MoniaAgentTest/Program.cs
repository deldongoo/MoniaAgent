using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using ModelContextProtocol.Protocol;
using MoniaAgentTest.Agents;
using MoniaAgentTest.Inputs;
using MoniaAgentTest.Outputs;
using MoniaAgent.Configuration;
using MoniaAgent.Agent;
using MoniaAgent.Agent.Inputs;
using MoniaAgent.Agent.Outputs;
using MoniaAgent.Orchestration.Dynamic;
using MoniaAgent.Orchestration.Static;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Xml.Linq;

namespace MoniaAgentTest
{

    public class GitLabConfig
    {
        public string PersonalAccessToken { get; set; } = string.Empty;
        public string ApiUrl { get; set; } = "https://gitlab.com";

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(PersonalAccessToken))
                throw new ArgumentException("GitLab Personal Access Token cannot be null or empty", nameof(PersonalAccessToken));
            if (string.IsNullOrWhiteSpace(ApiUrl))
                throw new ArgumentException("GitLab API URL cannot be null or empty", nameof(ApiUrl));
            if (!Uri.TryCreate(ApiUrl, UriKind.Absolute, out _))
                throw new ArgumentException("GitLab API URL must be a valid URI", nameof(ApiUrl));
        }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Configure UTF-8 encoding for proper emoji display on Windows
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            // Configure centralized logging
            SetupLogging(args);
            
            Console.WriteLine("=== MoniaAgent Test Application ===\n");
            
            // Setup configuration
            var (llm, gitlabConfig) = SetupConfiguration();

            // Run tests
            //await TestWorkflowSystem(llm);
            //await TestMcpDesktopCommander(llm);
            await TestOrchestratorAgent(llm);
            //await TestTimeAgent(llm);
            //await TestFileReaderAgent(llm);
            //await TestTranslatorAgent(llm);

            Console.WriteLine("\n=== All tests completed ===");
        }

        static void SetupLogging(string[] args)
        {
            // Determine log level from command line args
            var logLevel = Microsoft.Extensions.Logging.LogLevel.Information; // Default
            
            if (args.Contains("--verbose") || args.Contains("-v"))
                logLevel = Microsoft.Extensions.Logging.LogLevel.Debug;
            else if (args.Contains("--quiet") || args.Contains("-q"))
                logLevel = Microsoft.Extensions.Logging.LogLevel.Warning;
            else if (args.Contains("--silent"))
                logLevel = Microsoft.Extensions.Logging.LogLevel.Error;

            //logLevel = Microsoft.Extensions.Logging.LogLevel.Error;
            // Configure MoniaLogging using the built-in helper
            MoniaLogging.ConfigureDefault(logLevel);
            
            var logger = MoniaLogging.CreateLogger<Program>();
            Console.WriteLine($"Logging configured with level: {logLevel}");
        }

        static (LLM llm, GitLabConfig gitLabConfig) SetupConfiguration()
        {
            // Load configuration from JSON file
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .Build();

            var llm = new LLM();
            configuration.GetSection("LLM").Bind(llm);

            var gitLabConfig = new GitLabConfig();
            configuration.GetSection("GitLab").Bind(gitLabConfig);

            return (llm, gitLabConfig);
        }

        static async Task TestWorkflowSystem(LLM llm)
        {
            Console.WriteLine("=== Testing Basic Workflow ===");

            try
            {
                // Create agents
                var fileReaderAgent = new FileReaderAgent(llm);
                var guardRailAgent = new GuardRailAgent(llm);
                
                // Create workflow with agents
                var workflow = new WorkflowBuilder()
                    .WithName("FileProcessingWorkflow")
                    .RegisterAgent(fileReaderAgent)
                    .RegisterAgent(guardRailAgent)
                    .AddStep("FileReaderAgent", config =>
                    {
                        config.InputTransformer = _ => new FileInput("test-file.txt");
                    })
                    .AddStep("GuardRailAgent", config =>
                    {
                        config.InputTransformer = prev =>
                        {
                            if (prev is FileOutput fileOutput)
                            {
                                return new TextInput($"Is the following content safe? Content: {fileOutput.Content}");
                            }
                            return new TextInput("Analyze this content for safety");
                        };
                    })
                    .Build();

                var workflowResult = await workflow.ExecuteAsync("Process the test file");
                
                DisplayWorkflowResult(workflowResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Workflow test failed: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        static async Task TestMcpDesktopCommander(LLM llm)
        {
            Console.WriteLine("=== Testing MCP Desktop Commander ===");
            
            try
            {
                var desktopCommanderAgent = new McpDesktopCommanderAgent(llm);
                /*var result = await desktopCommanderAgent.ExecuteAsync(@"Generate an SVG of a pelican riding a bicycle.
                    Write the code into a html file in C:\Users\serva\source\repos\MoniaSandbox.
                    Launch it in a web browser.");*/

                var result = await desktopCommanderAgent.ExecuteAsync(@"Add 'C:\Users\serva\source\repos\MoniaAgent\MoniaAgent\MoniaAgent\Core' to your allowed directories. Analyse the repository's source files and extract all classes, interfaces, enums, and their relationships to generate a comprehensive UML class diagram. Extract the following elements from each source file and store it in a analysis.json file:
- **Classes/Interfaces/Enums**: Name, type, namespace/package, access modifiers
- **Attributes/Properties**: Name, type (including generics), visibility, static/instance
- **Methods**: Name, parameters (with types), return type, visibility, static/instance
- **Relationships**: Inheritance, implementation, composition, aggregation, dependencies
Then Convert the JSON to a valid PlantUML class diagram strored in a class_diagram.puml file.");
                /*
                var workflow = new WorkflowBuilder()
                    .WithName("FileProcessingWorkflow")
                    .RegisterAgent(desktopCommanderAgent)
                    .AddStep("McpDesktopCommanderAgent", config =>
                    {
                        config.InputTransformer = _ => new TextInput("\"Generate an SVG of a pelican riding a bicycle. Write the code into a html file in C:\\Users\\serva\\source\\repos\\MoniaSandbox.Launch it in a web browser.");
                    })
                    .Build();

                var workflowResult = await workflow.ExecuteAsync("Load SVG butterfly in the browser");*/
                //DisplayWorkflowResult(workflowResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Desktop commander test failed: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        static async Task TestOrchestratorAgent(LLM llm)
        {
            Console.WriteLine("=== Testing Orchestrator Agent ===");

            try
            {
                var orchestrator = new OrchestratorAgent(llm);
                
                // Register available agents
                orchestrator.RegisterAgentType<TimeAgent>();
                orchestrator.RegisterAgentType<FileReaderAgent>();
                orchestrator.RegisterAgentType<GuardRailAgent>();
                orchestrator.RegisterAgentType<TranslatorAgent>();
                orchestrator.RegisterAgentType<McpDesktopCommanderAgent>();

                // Test single agent task
                /*Console.WriteLine("\n--- Test 1: Single Agent Task ---");
                var timeResult = await orchestrator.ExecuteAsync("What time is it now?");
                Console.WriteLine($"Time query - Success: {timeResult.Success}");
                Console.WriteLine($"Time query - Content: {timeResult.Content}");
                
                Console.WriteLine("\n--- Test 2: File Reading Task ---");
                var fileResult = await orchestrator.ExecuteAsync("Read the file test-file.txt");
                Console.WriteLine($"File query - Success: {fileResult.Success}");
                Console.WriteLine($"File query - Content: {fileResult.Content}");

                // Test complex multi-agent task
                Console.WriteLine("\n--- Test 3: Multi-Agent Task ---");
                var complexResult = await orchestrator.ExecuteAsync("Read test-file.txt and tell me what time it is");
                Console.WriteLine($"Complex query - Success: {complexResult.Success}");
                Console.WriteLine($"Complex query - Content: {complexResult.Content}");*/
                
                // Test with a task that requires sequential steps
                Console.WriteLine("\n--- Test 4: Sequential Multi-Agent Task ---");
                var safetyCheckResult = await orchestrator.ExecuteAsync("Read safe-test-file.txt, check if the content is safe, if it is translate it to english and write translation in test-file-translated.txt");
                Console.WriteLine($"Safety check - Success: {safetyCheckResult.Success}");
                Console.WriteLine($"Safety check - Content: {safetyCheckResult.Content}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Orchestrator test failed: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        static async Task TestTimeAgent(LLM llm)
        {
            Console.WriteLine("=== Testing Time Agent===");

            try
            {
                var timeAgent = new TimeAgent(llm);
                var timeAgentResponse = await timeAgent.ExecuteAsync("What local time is it?");

                Console.WriteLine($"Success: {timeAgentResponse.Success}");
                if (timeAgentResponse.Success && timeAgentResponse is TimeOutput timeOutput)
                {
                    Console.WriteLine($"Time: {timeOutput.CurrentTime}");
                    Console.WriteLine($"Text: {timeOutput.Content}");
                }
                else
                {
                    Console.WriteLine($"Error: {timeAgentResponse.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Time Agent test failed: {ex.Message}");
            }

            Console.WriteLine();
        }

        static async Task TestFileReaderAgent(LLM llm)
        {
            Console.WriteLine("=== Testing FileReaderAgent ===");
            
            try
            {
                var fileReaderAgent = new FileReaderAgent(llm);
                var fileResult = await fileReaderAgent.ExecuteAsync(new FileInput("test-file.txt"));
                Console.WriteLine("=== Results FileReaderAgent ===");
                Console.WriteLine($"File read success: {fileResult.Success}");
                if (fileResult.Success && fileResult is FileOutput fileOutput)
                {
                    Console.WriteLine($"File Path: {fileOutput.FilePath}");
                    Console.WriteLine($"File Size: {fileOutput.FileSize} bytes");
                    Console.WriteLine($"Last Modified: {fileOutput.LastModified}");
                    Console.WriteLine($"Content:\n{fileOutput.Content}");
                }
                else
                {
                    Console.WriteLine($"Error: {fileResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"File reader test failed: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        static async Task TestTranslatorAgent(LLM llm)
        {
            Console.WriteLine("=== Testing TranslatorAgent ===");
            
            try
            {
                var translatorAgent = new TranslatorAgent(llm);
                var result = await translatorAgent.ExecuteAsync(new TranslationInput
                {
                    Content = "Hello, how are you?",
                    TargetLanguage = "French"
                });
                if (result.Success)
                {
                    Console.WriteLine($"Translation success: {result.Success}");
                    Console.WriteLine($"Text: {result.Content}");
                }
                else
                {
                    Console.WriteLine($"Error: {result.ErrorMessage}");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Translator test failed: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        static void DisplayWorkflowResult(WorkflowExecutionResult result)
        {
            Console.WriteLine("\n" + new string('=', 70));
            Console.WriteLine($"WORKFLOW: {result.WorkflowName}");
            Console.WriteLine($"Status: {(result.Success ? "✅ SUCCESS" : "❌ FAILED")} | Duration: {result.TotalDuration.TotalSeconds:F2}s | Steps: {result.StepResults.Count}");
            
            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"Error: {result.ErrorMessage}");
            }
            Console.WriteLine(new string('=', 70));

            // Display each step
            for (int i = 0; i < result.StepResults.Count; i++)
            {
                var step = result.StepResults[i];
                
                Console.WriteLine($"\nSTEP {i + 1}: {step.AgentName}");
                Console.WriteLine(new string('-', 40));
                Console.WriteLine($"Status: {(step.Success ? "✅ SUCCESS" : "❌ FAILED")} | Duration: {step.Duration.TotalSeconds:F2}s | Attempts: {step.AttemptNumber}");
                
                if (!step.Success && !string.IsNullOrEmpty(step.ErrorMessage))
                {
                    Console.WriteLine($"Error: {step.ErrorMessage}");
                }

                // Display conversation history
                if (step.Result?.Metadata?.ConversationHistory?.Any() == true)
                {
                    Console.WriteLine("Conversation history:");
                    foreach (var historyStep in step.Result.Metadata.ConversationHistory)
                    {
                        var timestamp = historyStep.Timestamp.ToString("HH:mm:ss");
                        switch (historyStep.Type)
                        {
                            case ConversationStepType.LlmResponse:
                                var responseText = historyStep.Content.Length > 100 
                                    ? historyStep.Content.Substring(0, 100) + "..." 
                                    : historyStep.Content;
                                Console.WriteLine($"  [{timestamp}] LLM: {responseText}");
                                break;
                            case ConversationStepType.ToolCall:
                                var args = historyStep.Arguments?.Any() == true 
                                    ? $"({string.Join(", ", historyStep.Arguments.Select(kv => $"{kv.Key}={kv.Value}"))})" 
                                    : "";
                                Console.WriteLine($"  [{timestamp}] TOOL CALL: {historyStep.ToolName}{args}");
                                break;
                            case ConversationStepType.ToolResult:
                                var resultText = historyStep.Result?.Length > 100 
                                    ? historyStep.Result.Substring(0, 100) + "..." 
                                    : historyStep.Result ?? "";
                                Console.WriteLine($"  [{timestamp}] TOOL RESULT: {historyStep.ToolName} -> {resultText}");
                                break;
                        }
                    }
                }

                // Display step result
                if (step.Result != null)
                {
                    Console.WriteLine("Step result:");
                    Console.WriteLine($"  Type: {step.Result.GetType().Name}");
                    
                    // Generic display using reflection
                    var properties = step.Result.GetType().GetProperties()
                        .Where(p => p.CanRead && p.PropertyType.IsPublic && p.Name != "Metadata")
                        .Take(10); // Limit to first 10 properties
                    
                    foreach (var prop in properties)
                    {
                        try
                        {
                            var value = prop.GetValue(step.Result);
                            if (value != null)
                            {
                                var displayValue = FormatPropertyValue(value);
                                Console.WriteLine($"  {prop.Name}: {displayValue}");
                            }
                        }
                        catch { /* Skip problematic properties */ }
                    }
                }
            }

            // Display final result
            Console.WriteLine($"\n" + new string('=', 70));
            Console.WriteLine("🎯 FINAL WORKFLOW RESULT:");
            if (result.FinalResult != null)
            {
                Console.WriteLine($"Type: {result.FinalResult.GetType().Name}");
                Console.WriteLine($"Success: {result.FinalResult.Success}");
                
                // Show final result content - generic approach
                var finalProps = result.FinalResult.GetType().GetProperties()
                    .Where(p => p.CanRead && p.PropertyType.IsPublic && p.Name != "Metadata");
                
                foreach (var prop in finalProps)
                {
                    try
                    {
                        var value = prop.GetValue(result.FinalResult);
                        if (value != null)
                        {
                            var displayValue = FormatPropertyValue(value);
                            Console.WriteLine($"{prop.Name}: {displayValue}");
                        }
                    }
                    catch { /* Skip problematic properties */ }
                }
            }
            else
            {
                Console.WriteLine("No final result available");
            }
            Console.WriteLine(new string('=', 70));
        }

        static string FormatPropertyValue(object value)
        {
            if (value == null) return "null";
            
            var stringValue = value.ToString();
            if (string.IsNullOrEmpty(stringValue)) return "";
            
            // Clean line breaks for consistent display
            var cleanValue = stringValue.Replace("\n", " ").Replace("\r", "");
            
            // Truncate long values
            if (cleanValue.Length > 300)
            {
                return cleanValue.Substring(0, 300) + "...";
            }
            
            return cleanValue;
        }
    }
}
using MoniaAgent.Agents;
using MoniaAgent.Configuration;
using MoniaAgent.Core;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;
using MoniaAgent.Workflows;
using MoniaAgentTest.Agents;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace MoniaAgentTest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Configure UTF-8 encoding for proper emoji display on Windows
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            Console.WriteLine("=== MoniaAgent Test Application ===\n");
            
            // Setup configuration
            var llm = SetupConfiguration();
            
            // Run tests
            await TestWorkflowSystem(llm);
           // await TestMcpDesktopCommander(llm);
            
            Console.WriteLine("\n=== All tests completed ===");
        }

        static LLM SetupConfiguration()
        {
            // Load configuration from JSON file
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .Build();

            var llm = new LLM();
            configuration.GetSection("LLM").Bind(llm);
            
            return llm;
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
                var desktopCommanderResponse = await desktopCommanderAgent.ExecuteAsync(
                    "Draw a butterfly in svg. Write the code into a html file in C:\\Users\\serva\\source\\repos\\MoniaSandbox. Launch it in a web browser.");
                
                Console.WriteLine($"Success: {desktopCommanderResponse.Success}");
                if (desktopCommanderResponse.Success)
                {
                    Console.WriteLine($"Result: Operation completed successfully");
                }
                else
                {
                    Console.WriteLine($"Error: {desktopCommanderResponse.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Desktop commander test failed: {ex.Message}");
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

                // Display performed actions
                if (step.Result?.Metadata?.PerformedActions?.Any() == true)
                {
                    Console.WriteLine("Actions performed:");
                    foreach (var action in step.Result.Metadata.PerformedActions)
                    {
                        Console.WriteLine($"  • {action.ToolName}");
                        if (action.Arguments?.Any() == true)
                        {
                            var args = string.Join(", ", action.Arguments.Select(kv => 
                            {
                                var value = kv.Value?.ToString()?.Replace("\n", " ").Replace("\r", "") ?? "";
                                return $"{kv.Key}={value}";
                            }));
                            Console.WriteLine($"    Args: {args}");
                        }
                        if (!string.IsNullOrEmpty(action.Result))
                        {
                            var cleanResult = action.Result.Replace("\n", " ").Replace("\r", "");
                            var result_text = cleanResult.Length > 100 
                                ? cleanResult.Substring(0, 100) + "..." 
                                : cleanResult;
                            Console.WriteLine($"    Result: {result_text}");
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
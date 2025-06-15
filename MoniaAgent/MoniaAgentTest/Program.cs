using MoniaAgent.Agents;
using MoniaAgent.Configuration;
using MoniaAgent.Core;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;
using MoniaAgent.Workflows;
using MoniaAgentTest.Agents;
using Microsoft.Extensions.Configuration;

namespace MoniaAgentTest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
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
                
                Console.WriteLine($"Workflow Success: {workflowResult.Success}");
                Console.WriteLine($"Total Duration: {workflowResult.TotalDuration}");
                Console.WriteLine($"Steps Executed: {workflowResult.StepResults.Count}");

                foreach (var step in workflowResult.StepResults)
                {
                    Console.WriteLine($"  {step.StepName} ({step.AgentName}): {step.Success} in {step.Duration}");
                }

                if (workflowResult.FinalResult != null)
                {
                    Console.WriteLine($"Final Result Type: {workflowResult.FinalResult.GetType().Name}");
                }
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
    }
}
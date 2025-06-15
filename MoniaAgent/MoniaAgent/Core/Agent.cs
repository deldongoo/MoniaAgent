using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using ModelContextProtocol.Client;
using MoniaAgent.Configuration;
using MoniaAgent.Tools;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;

namespace MoniaAgent.Core
{

    public class LoggingHandler : DelegatingHandler

    {
        public LoggingHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();

            sb.AppendLine("===== HTTP REQUEST =====");
            sb.AppendLine($"{request.Method} {request.RequestUri}");

            foreach (var header in request.Headers)
            {
                sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            if (request.Content != null)

            {
                var content = await request.Content.ReadAsStringAsync();

                sb.AppendLine("Body:");

                sb.AppendLine(content);
            }
            sb.AppendLine("=========================");

            string log = sb.ToString();
            Console.WriteLine(log);
            System.Diagnostics.Debug.WriteLine(log); // pour Visual Studio "Sortie" > "Debug"
            return await base.SendAsync(request, cancellationToken);
        }
    }

    public class Agent : IAgent
    {
        private static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        private static readonly ILogger logger = loggerFactory.CreateLogger<Agent>();
        
        protected readonly LLM? llm;
        protected readonly IList<AITool> tools;
        private IChatClient? chatClient;
        protected IMcpClient? mcpClient;
        
        protected string Goal { get; private set; } = string.Empty;

        // IAgent implementation
        public virtual string Name => "Agent";
        public virtual string Specialty => "General purpose assistant";
        public virtual Type[] SupportedInputTypes => new[] { typeof(AgentInput) };
        public virtual Type ExpectedOutputType => typeof(AgentOutput);
        public virtual bool CanHandle(string task) => true;

        protected Agent(LLM llm, IList<AITool> tools, string goal, IMcpClient? mcpClient = null)
        {
            llm?.Validate();
            
            if (string.IsNullOrWhiteSpace(goal))
                throw new ArgumentException("Goal cannot be null or empty", nameof(goal));
                
            this.llm = llm;
            this.tools = new List<AITool>(tools ?? new List<AITool>());
            
            // Add built-in framework tools
            this.tools.Add(TaskCompleteTool.Create());
            
            this.Goal = goal;
            this.mcpClient = mcpClient;
            
            // Initialize connection
            InitializeConnection();
        }

        protected Agent(LLM llm, IMcpClient? mcpClient = null)
        {
            llm?.Validate();
            this.llm = llm;
            this.tools = new List<AITool>();
            this.Goal = string.Empty; // Will be set by Initialize
            this.mcpClient = mcpClient;
            
            // Initialize connection
            InitializeConnection();
        }

        protected void Initialize(IList<AITool> tools, string goal)
        {
            if (string.IsNullOrWhiteSpace(goal))
                throw new ArgumentException("Goal cannot be null or empty", nameof(goal));

            this.tools.Clear();
            if (tools != null)
            {
                foreach (var tool in tools)
                {
                    this.tools.Add(tool);
                }
            }
            
            // Add built-in framework tools
            this.tools.Add(TaskCompleteTool.Create());
            
            this.Goal = goal;
        }

        private void InitializeConnection()
        {
            if (chatClient != null)
                return; // Already connected
                
            if (llm == null)
                throw new InvalidOperationException("LLM configuration is null");

            var handler = new LoggingHandler(new HttpClientHandler());
            var httpClient = new HttpClient(handler);

            var options = new OpenAI.OpenAIClientOptions
            {
                Endpoint = new Uri(llm.BaseUrl),
                //Transport = new HttpClientPipelineTransport(httpClient),
            };

            var openAIClient = new OpenAI.OpenAIClient(new System.ClientModel.ApiKeyCredential(llm.ApiKey), options);
            var openAIChatClient = openAIClient.GetChatClient(llm.Model);

           /* chatClient = new ChatClientBuilder(openAIChatClient.AsIChatClient())
                .UseFunctionInvocation()
                .Build();*/

            // Use base client without function invocation to avoid serialization issues
            chatClient = openAIChatClient.AsIChatClient();
        }


        protected async Task<string> ExecuteInternal(string prompt, ExecutionMetadata executionMetadata)
        {
            if (chatClient == null)
                throw new InvalidOperationException("Chat client not initialized. This should not happen as connection is initialized in constructor.");
                
            try
            {
                var messages = new List<ChatMessage>
                {
                    new ChatMessage(ChatRole.System, Goal),
                    new ChatMessage(ChatRole.User, prompt)
                };

                var requestOptions = new ChatOptions
                {
                    Tools = tools,
                };

                const int maxTurns = 10;
                int consecutiveNonToolMessages = 0;
                string lastTextResponse = "";

                for (int turn = 0; turn < maxTurns; turn++)
                {
                    var completion = await chatClient!.GetResponseAsync(messages, requestOptions);
                    
                    // Check if response contains tool calls  
                    if (completion.FinishReason == ChatFinishReason.ToolCalls)
                    {
                        // Add all response messages
                        messages.AddRange(completion.Messages);
                        
                        // Log any text content that accompanies tool calls
                        foreach (var message in completion.Messages)
                        {
                            var textContent = message.Contents.OfType<TextContent>().FirstOrDefault()?.Text;
                            if (!string.IsNullOrEmpty(textContent))
                            {
                                Console.WriteLine($"[TEXT] {textContent}");
                            }
                        }
                        
                        // Get tool calls from the response
                        var toolCalls = completion.Messages
                            .SelectMany(m => m.Contents.OfType<FunctionCallContent>())
                            .ToList();
                        
                        foreach (var toolCall in toolCalls)
                        {
                            // Execute tool manually
                            var toolResult = await ExecuteToolManually(toolCall);

                            // Track action in metadata
                            var performedAction = new PerformedAction
                            {
                                ToolName = toolCall.Name,
                                Arguments = toolCall.Arguments?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new(),
                                Result = FormatActionOutput(toolResult),
                                Timestamp = DateTime.UtcNow
                            };
                            executionMetadata.PerformedActions.Add(performedAction);
                            
                            // Real-time display
                            var actionDescription = FormatActionForDisplay(performedAction);
                            Console.WriteLine($"[TOOLCALL] {actionDescription}");
                            
                            // If task_complete was called, return its result immediately
                            if (toolCall.Name == "TaskComplete")
                            {
                                var actionSummaries = executionMetadata.PerformedActions.Select(FormatActionForDisplay);
                                var finalSummary = string.IsNullOrEmpty(lastTextResponse)
                                    ? $"Actions performed:\n{string.Join("\n", actionSummaries)}"
                                    : $"{lastTextResponse}\n\nActions performed:\n{string.Join("\n", actionSummaries)}";
                                return finalSummary;
                            }
                            
                            // Add tool result as message
                            var toolResultContent = new FunctionResultContent(toolCall.CallId, toolResult);
                            messages.Add(new ChatMessage(ChatRole.Tool, [toolResultContent]));
                        }
                        
                        consecutiveNonToolMessages = 0;
                    }
                    else
                    {
                        // Pure text response
                        var lastMessage = completion.Messages.LastOrDefault();
                        lastTextResponse = lastMessage?.Text ?? "";
                        
                        // Log text responses
                        if (!string.IsNullOrEmpty(lastTextResponse))
                        {
                            Console.WriteLine($"[TEXT] {lastTextResponse}");
                        }
                        
                        if (lastMessage != null)
                            messages.Add(lastMessage);
                        consecutiveNonToolMessages++;
                        
                        // Stop if agent gave 2 consecutive text responses (finished thinking)
                        if (consecutiveNonToolMessages >= 2)
                        {
                            break;
                        }
                    }
                }

                // Return summary if actions were performed, otherwise last text response
                if (executionMetadata.PerformedActions.Count > 0)
                {
                    var actionSummaries = executionMetadata.PerformedActions.Select(FormatActionForDisplay);
                    var finalSummary = string.IsNullOrEmpty(lastTextResponse) 
                        ? $"Actions performed:\n{string.Join("\n", actionSummaries)}"
                        : $"{lastTextResponse}\n\nActions performed:\n{string.Join("\n", actionSummaries)}";
                    return finalSummary;
                }
                
                return lastTextResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error communicating with API");
                throw new Exception($"Error communicating with API: {ex.Message}", ex);
            }
        }

        public virtual async Task<AgentOutput> ExecuteAsync(AgentInput input, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var executionMetadata = new ExecutionMetadata
            {
                StartTime = DateTime.UtcNow,
                AgentName = Name
            };
            
            try
            {
                // Convert typed input to prompt
                string prompt = ConvertInputToPrompt(input);
                
                // Use existing Execute logic
                string textResult = await ExecuteInternal(prompt, executionMetadata);
                
                // Convert string result to typed result
                var result = ConvertToTypedResult(textResult, executionMetadata);
                
                result.Metadata = executionMetadata;
                result.Metadata.EndTime = DateTime.UtcNow;
                
                return result;
            }
            catch (Exception ex)
            {
                return CreateErrorResult(ex, executionMetadata);
            }
        }
        
        // Virtual method that specialized agents can override
        protected virtual string ConvertInputToPrompt(AgentInput input)
        {
            return input switch
            {
                TextInput textInput => textInput.Prompt,
                _ => System.Text.Json.JsonSerializer.Serialize(input)
            };
        }
        
        // Virtual method for converting results
        protected virtual AgentOutput ConvertToTypedResult(string textResult, ExecutionMetadata metadata)
        {
            // Default implementation returns TextOutput
            return new TextOutput
            {
                Success = !textResult.Contains("Error") && !textResult.Contains("Failed"),
                Content = textResult,
                Metadata = metadata
            };
        }
        
        protected virtual AgentOutput CreateErrorResult(Exception ex, ExecutionMetadata metadata)
        {
            return new TextOutput
            {
                Success = false,
                ErrorMessage = ex.Message,
                Content = string.Empty,
                Metadata = metadata
            };
        }

        private async Task<string> ExecuteToolManually(FunctionCallContent toolCall)
        {
            try
            {
                // Convert arguments to Dictionary<string, object?>
                var argumentsDict = new Dictionary<string, object?>();
                
                if (toolCall.Arguments != null)
                {
                    foreach (var kvp in toolCall.Arguments)
                    {
                        argumentsDict[kvp.Key] = kvp.Value;
                    }
                }

                // Check if tool exists in MCP client
                if (mcpClient != null)
                {
                    var mcpTools = await mcpClient.ListToolsAsync();
                    if (mcpTools.Any(t => t.Name == toolCall.Name))
                    {
                        // Execute via MCP
                        var result = await mcpClient.CallToolAsync(
                            toolCall.Name, 
                            argumentsDict, 
                            cancellationToken: CancellationToken.None);
                        
                        // Extract content from CallToolResponse
                        if (result?.Content != null)
                        {
                            var textContents = result.Content
                                .Where(c => c.Type == "text")
                                .Select(c => c.Text)
                                .Where(text => !string.IsNullOrEmpty(text));
                            return string.Join("\n", textContents);
                        }
                        
                        return "";
                    }
                }

                // Execute locally via framework tools
                return await ExecuteLocalTool(toolCall, argumentsDict);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing tool {ToolName}", toolCall.Name);
                return $"Error executing tool: {ex.Message}";
            }
        }

        private async Task<string> ExecuteLocalTool(FunctionCallContent toolCall, Dictionary<string, object?> argumentsDict)
        {
            // Find the AITool in our tools list
            var aiTool = tools.FirstOrDefault(t => t.Name == toolCall.Name);
            if (aiTool == null)
            {
                return $"Error: Tool '{toolCall.Name}' not found in local tools list";
            }

            // Cast to AIFunction for manual invocation
            if (aiTool is AIFunction aiFunction)
            {
                try
                {
                    AIFunctionArguments? functionArgs = null;

                    if (argumentsDict.Count > 0)
                    {
                        functionArgs = new AIFunctionArguments(argumentsDict);
                    }
                    
                    var result = await aiFunction.InvokeAsync(functionArgs, CancellationToken.None);
                    return result?.ToString() ?? "";
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error invoking AIFunction {ToolName}", toolCall.Name);
                    return $"Error invoking local tool: {ex.Message}";
                }
            }

            return $"Error: Tool '{toolCall.Name}' is not an AIFunction";
        }

        private static string FormatActionOutput(string toolResult)
        {
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(toolResult);
                if (json.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
                {
                    var textContent = content.EnumerateArray()
                        .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "text")
                        .Select(item => item.GetProperty("text").GetString())
                        .FirstOrDefault();
                    return textContent ?? toolResult;
                }
            }
            catch
            {
                // Fallback to original if parsing fails
            }
            return toolResult;
        }

        private static string FormatActionForDisplay(PerformedAction action)
        {
            var argsString = action.Arguments.Count > 0
                ? $"({string.Join(", ", action.Arguments.Select(kvp => $"{kvp.Key}={kvp.Value}"))})"
                : "";
            return $"- {action.ToolName}{argsString} --> {action.Result}";
        }

    }
}

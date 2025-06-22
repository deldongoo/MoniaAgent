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
            var logger = MoniaLogging.CreateLogger("HttpRequest");
            logger.LogDebug("{HttpRequestLog}", log);
            return await base.SendAsync(request, cancellationToken);
        }
    }

    public class Agent : IAgent
    {
        private readonly ILogger logger;
        
        protected readonly LLM? llm;
        protected readonly IList<AITool> tools;
        private IChatClient? chatClient;
        protected IMcpClient? mcpClient;
        
        protected string Goal { get; private set; } = string.Empty;

        // IAgent implementation
        public virtual string Name => "Agent";
        public virtual string Specialty => "General purpose assistant";


        protected Agent(LLM llm, IMcpClient? mcpClient = null)
        {
            llm?.Validate();
            this.llm = llm;
            this.tools = new List<AITool>();
            this.Goal = string.Empty; // Will be set by Initialize
            this.mcpClient = mcpClient;
            this.logger = MoniaLogging.CreateLogger<Agent>();
            
            // Initialize connection
            InitializeConnection();
        }

        protected AgentConfig? agentConfig;

        protected void Initialize(IList<AITool> tools, string goal)
        {
            var config = new AgentConfig { Goal = goal };
            Initialize(tools, config);
        }

        protected void Initialize(IList<AITool> tools, AgentConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.Goal))
                throw new ArgumentException("Goal cannot be null or empty", nameof(config.Goal));

            this.agentConfig = config;
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
            
            this.Goal = config.Goal + $"\n\nIMPORTANT: Always call the {Tools.TaskCompleteTool.TOOL_NAME} tool when you have finished your work to indicate completion.";
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

                // Add structured output if requested
                if (agentConfig?.UseStructuredOutput == true)
                {
                    try
                    {
                        requestOptions.ResponseFormat = ChatResponseFormat.Json;
                        logger.LogDebug("Structured output enabled for agent {AgentName}", agentConfig.Name);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Failed to enable structured output: {Error}. Falling back to normal mode.", ex.Message);
                    }
                }

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
                        
                        // Log and record any text content that accompanies tool calls
                        foreach (var message in completion.Messages)
                        {
                            var textContent = message.Contents.OfType<TextContent>().FirstOrDefault()?.Text;
                            if (!string.IsNullOrEmpty(textContent))
                            {
                                logger.LogInformation("[TEXT] {TextContent}", textContent);
                                
                                // Record LLM response in conversation history
                                executionMetadata.ConversationHistory.Add(new ConversationStep
                                {
                                    Type = ConversationStepType.LlmResponse,
                                    Timestamp = DateTime.UtcNow,
                                    Content = textContent
                                });
                            }
                        }
                        
                        // Get tool calls from the response
                        var toolCalls = completion.Messages
                            .SelectMany(m => m.Contents.OfType<FunctionCallContent>())
                            .ToList();
                        
                        foreach (var toolCall in toolCalls)
                        {
                            // Record tool call in conversation history
                            var argumentsDict = toolCall.Arguments?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new();
                            executionMetadata.ConversationHistory.Add(new ConversationStep
                            {
                                Type = ConversationStepType.ToolCall,
                                Timestamp = DateTime.UtcNow,
                                Content = $"Calling {toolCall.Name}",
                                ToolName = toolCall.Name,
                                Arguments = argumentsDict
                            });
                            
                            // Execute tool manually
                            var toolResult = await ExecuteToolManually(toolCall);
                            
                            // Record tool result in conversation history
                            executionMetadata.ConversationHistory.Add(new ConversationStep
                            {
                                Type = ConversationStepType.ToolResult,
                                Timestamp = DateTime.UtcNow,
                                Content = $"Result from {toolCall.Name}",
                                ToolName = toolCall.Name,
                                Result = FormatActionOutput(toolResult)
                            });

                            // Real-time display
                            var argsString = string.Join(", ", argumentsDict.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                            logger.LogInformation("[TOOLCALL] {ToolName}({Arguments}) --> {Result}", toolCall.Name, argsString, FormatActionOutput(toolResult));
                            
                            // If task_complete was called, return its finalAnswer immediately
                            if (toolCall.Name == TaskCompleteTool.TOOL_NAME)
                            {
                                return toolResult ?? "Task completed";
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
                        
                        // Log and record text responses
                        if (!string.IsNullOrEmpty(lastTextResponse))
                        {
                            logger.LogInformation("[TEXT] {TextContent}", lastTextResponse);
                            
                            // Record LLM response in conversation history
                            executionMetadata.ConversationHistory.Add(new ConversationStep
                            {
                                Type = ConversationStepType.LlmResponse,
                                Timestamp = DateTime.UtcNow,
                                Content = lastTextResponse
                            });
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
                
                // Framework ensures metadata is always correctly set
                if (result.Metadata == null || result.Metadata != executionMetadata)
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


    }
}

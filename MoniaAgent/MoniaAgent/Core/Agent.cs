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
        }

        protected Agent(LLM llm, IMcpClient? mcpClient = null)
        {
            llm?.Validate();
            this.llm = llm;
            this.tools = new List<AITool>();
            this.Goal = string.Empty; // Will be set by Initialize
            this.mcpClient = mcpClient;
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

        public Task ConnectAsync()
        {
            if (chatClient != null)
                return Task.CompletedTask; // Déjà connecté
                
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


            return Task.CompletedTask;
        }


        public async Task<string> Execute(string prompt)
        {
            if (chatClient == null)
                await ConnectAsync();
                
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
                var actionsSummary = new List<string>();

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
                                Console.WriteLine($"[RESPONSE] {textContent}");
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

                            // Track action for summary
                            var argsString = "";
                            /*argsString = toolCall.Arguments != null && toolCall.Arguments.Any() 
                                ? $"({string.Join(", ", toolCall.Arguments.Select(kvp => $"{kvp.Key}={kvp.Value}"))})" 
                                : "";*/
                            var formattedResult = FormatActionOutput(toolResult);
                            var actionDescription = $"- {toolCall.Name}{argsString} --> {formattedResult}";
                            actionsSummary.Add(actionDescription);
                            
                            // Real-time display
                            Console.WriteLine($"[ACTION] {actionDescription}");
                            
                            // If task_complete was called, return its result immediately
                            if (toolCall.Name == "task_complete")
                            {
                                return "Exit";
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
                            Console.WriteLine($"[RESPONSE] {lastTextResponse}");
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

                /*// Return summary if actions were performed, otherwise last text response
                if (actionsSummary.Count > 0)
                {
                    var finalSummary = string.IsNullOrEmpty(lastTextResponse) 
                        ? $"Actions performed:\n{string.Join("\n", actionsSummary)}"
                        : $"{lastTextResponse}\n\nActions performed:\n{string.Join("\n", actionsSummary)}";
                    return finalSummary;
                }*/
                
                return lastTextResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error communicating with API");
                throw new Exception($"Error communicating with API: {ex.Message}", ex);
            }
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

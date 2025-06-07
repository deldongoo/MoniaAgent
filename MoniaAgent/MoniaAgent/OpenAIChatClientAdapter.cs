using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ClientModel;

namespace MoniaAgent
{
    internal class OpenAIChatClientAdapter : IChatClient
    {
        private Microsoft.Extensions.AI.IChatClient innerClient;
        
        public OpenAIChatClientAdapter(LLM llm)
        {
            var options = new OpenAIClientOptions
            {
                Endpoint = new Uri(llm.BaseUrl)
            };

            var openAIClient = new OpenAIClient(new ApiKeyCredential(llm.ApiKey), options);
            var openAIChatClient = openAIClient.GetChatClient(llm.Model);
            var chatClient = openAIChatClient.AsIChatClient();

            innerClient = new ChatClientBuilder(chatClient)
                .UseFunctionInvocation()
                .Build();
        }
        
        public async Task<string> GetResponseAsync(IList<ChatMessage> messages, ChatOptions options)
        {
            var msftMessages = messages.Select(m => new Microsoft.Extensions.AI.ChatMessage(
                MapRole(m.Role), m.Content)).ToList();
                
            var msftOptions = new Microsoft.Extensions.AI.ChatOptions();
            
            // Convert tools using the new unified Tool.ToMicrosoftAI() method
            if (options.Tools?.Any() == true)
            {
                var mappedTools = new List<Microsoft.Extensions.AI.AITool>();
                foreach (var tool in options.Tools)
                {
                    try
                    {
                        var mappedTool = tool.ToMicrosoftAI();
                        if (mappedTool != null)
                            mappedTools.Add(mappedTool);
                    }
                    catch (Exception ex)
                    {
                        // Log and skip problematic tools instead of failing
                        System.Diagnostics.Debug.WriteLine($"Skipping tool {tool.Name}: {ex.Message}");
                        Console.WriteLine($"DEBUG: Skipping tool {tool.Name}: {ex.Message}");
                    }
                }
                
                if (mappedTools.Any())
                {
                    msftOptions.Tools = mappedTools;
                    msftOptions.AllowMultipleToolCalls = options.AllowMultipleToolCalls;
                }
            }
            
            var completion = await innerClient.GetResponseAsync(msftMessages, msftOptions);
            return completion.Text ?? "";
        }
        
        private Microsoft.Extensions.AI.ChatRole MapRole(string role)
        {
            return role switch
            {
                ChatRole.System => Microsoft.Extensions.AI.ChatRole.System,
                ChatRole.User => Microsoft.Extensions.AI.ChatRole.User,
                ChatRole.Assistant => Microsoft.Extensions.AI.ChatRole.Assistant,
                _ => Microsoft.Extensions.AI.ChatRole.User
            };
        }
        
    }
}
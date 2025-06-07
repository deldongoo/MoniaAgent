using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoniaAgent
{
    public class Agent : IAgent
    {
        private static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        private static readonly ILogger logger = loggerFactory.CreateLogger<Agent>();
        
        private readonly LLM? llm;
        private readonly IList<Tool> tools;
        private readonly string goal;
        private IChatClient? chatClient;

        // IAgent implementation
        public virtual string Name => "Agent";
        public virtual string Specialty => "General purpose assistant";
        public virtual bool CanHandle(string task) => true;

        public Agent(LLM llm, IList<Tool> tools, string goal)
        {
            llm?.Validate();
            
            if (string.IsNullOrWhiteSpace(goal))
                throw new ArgumentException("Goal cannot be null or empty", nameof(goal));
                
            this.llm = llm;
            this.tools = tools ?? new List<Tool>();
            this.goal = goal;
        }

        public Task ConnectAsync()
        {
            if (chatClient != null)
                return Task.CompletedTask; // Déjà connecté
                
            if (llm == null)
                throw new InvalidOperationException("LLM configuration is null");
                
            chatClient = new OpenAIChatClientAdapter(llm);
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
                    new ChatMessage(ChatRole.System, goal),
                    new ChatMessage(ChatRole.User, prompt)
                };

                var requestOptions = new ChatOptions
                {
                    Tools = tools,
                    AllowMultipleToolCalls = true,
                };

                return await chatClient.GetResponseAsync(messages, requestOptions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error communicating with API");
                throw new Exception($"Error communicating with API: {ex.Message}", ex);
            }
        }
    }
}

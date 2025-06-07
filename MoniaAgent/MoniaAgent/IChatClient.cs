using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoniaAgent
{
    public interface IChatClient
    {
        Task<string> GetResponseAsync(IList<ChatMessage> messages, ChatOptions options);
    }
    
    public class ChatMessage
    {
        public string Role { get; }
        public string Content { get; }
        
        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }
    
    public static class ChatRole
    {
        public const string System = "system";
        public const string User = "user";
        public const string Assistant = "assistant";
    }
    
    public class ChatOptions
    {
        public IList<Tool> Tools { get; set; } = new List<Tool>();
        public bool AllowMultipleToolCalls { get; set; } = false;
    }
    
}
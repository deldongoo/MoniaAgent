using Microsoft.Extensions.AI;
using MoniaAgent;

namespace MoniaAgentTest
{
    public class ConversationalTest
    {
        public static async Task TestConversationalLoop()
        {
            var llm = new LLM
            {
                BaseUrl = "https://openrouter.ai/api/v1",
                ApiKey = "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",
                Model = "openai/gpt-4o"
            };

            // Create a simple agent that should use multiple turns
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
            Console.WriteLine($"Result: {result4}");
        }
    }
}
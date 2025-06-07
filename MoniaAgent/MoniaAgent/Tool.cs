using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoniaAgent
{
    public abstract class Tool
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        
        /// <summary>
        /// Executes the tool asynchronously with the provided parameters
        /// </summary>
        public virtual async Task<object> ExecuteAsync(Dictionary<string, object> parameters)
        {
            return await Task.FromResult(Execute(parameters));
        }
        
        /// <summary>
        /// Executes the tool synchronously with the provided parameters
        /// Override this method for simple tools that don't need async execution
        /// </summary>
        public virtual object Execute(Dictionary<string, object> parameters)
        {
            throw new NotImplementedException($"Tool {Name} must implement either Execute or ExecuteAsync");
        }
        
        /// <summary>
        /// Internal method to convert this tool to Microsoft.Extensions.AI format
        /// </summary>
        internal virtual Microsoft.Extensions.AI.AITool ToMicrosoftAI()
        {
            return Microsoft.Extensions.AI.AIFunctionFactory.Create(
                () => Execute(new Dictionary<string, object>()),
                name: Name,
                description: Description
            );
        }
        
        /// <summary>
        /// Helper method to get a typed parameter value with default fallback
        /// </summary>
        protected T GetParameter<T>(Dictionary<string, object> parameters, string key, T defaultValue = default(T))
        {
            if (parameters?.TryGetValue(key, out var value) == true)
            {
                try
                {
                    if (value is T directValue)
                        return directValue;
                    
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            
            return defaultValue;
        }
    }
}
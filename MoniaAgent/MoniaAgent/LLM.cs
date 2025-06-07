using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoniaAgent
{
    public class LLM
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new ArgumentException("API key cannot be null or empty", nameof(ApiKey));
            
            if (string.IsNullOrWhiteSpace(BaseUrl))
                throw new ArgumentException("Base URL cannot be null or empty", nameof(BaseUrl));
            
            if (string.IsNullOrWhiteSpace(Model))
                throw new ArgumentException("Model cannot be null or empty", nameof(Model));
            
            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
                throw new ArgumentException("Base URL must be a valid URI", nameof(BaseUrl));
        }
    }
}

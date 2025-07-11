using Microsoft.Extensions.Logging;

namespace MoniaAgent.Agent
{
    /// <summary>
    /// Centralized logging configuration for the MoniaAgent framework
    /// </summary>
    public static class MoniaLogging
    {
        private static ILoggerFactory? _loggerFactory;
        
        /// <summary>
        /// Gets or sets the logger factory used throughout the framework
        /// </summary>
        public static ILoggerFactory LoggerFactory
        {
            get => _loggerFactory ?? Microsoft.Extensions.Logging.LoggerFactory.Create(_ => { });
            set => _loggerFactory = value;
        }
        
        /// <summary>
        /// Creates a logger for the specified type
        /// </summary>
        public static ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
        
        /// <summary>
        /// Creates a logger with the specified category name
        /// </summary>
        public static ILogger CreateLogger(string categoryName) => LoggerFactory.CreateLogger(categoryName);
        
        /// <summary>
        /// Configures logging levels for common scenarios
        /// </summary>
        public static void ConfigureDefault(LogLevel minimumLevel = LogLevel.Information)
        {
            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(minimumLevel)
                    .AddConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                    });
            });
        }
    }
}
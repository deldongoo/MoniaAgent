using Microsoft.Extensions.AI;
using MoniaAgent;
using System;
using System.Collections.Generic;

namespace MoniaAgentTest
{
    public static class CurrentTimeTool
    {
        public static AITool Create()
        {
            return AIFunctionFactory.Create(
            (Dictionary<string, object?> parameters) => 
            {
                var timezone = GetParameter(parameters, "timezone", "local");
                var format = GetParameter(parameters, "format", "yyyy-MM-dd HH:mm:ss zzz");
                
                DateTime dateTime;
                
                // Handle timezone parameter
                if (string.IsNullOrEmpty(timezone) || timezone.ToLower() == "local")
                {
                    dateTime = DateTime.Now;
                }
                else if (timezone.ToLower() == "utc")
                {
                    dateTime = DateTime.UtcNow;
                }
                else
                {
                    // Try to parse timezone as TimeZoneInfo
                    try
                    {
                        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                        dateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);
                    }
                    catch
                    {
                        // Fallback to local time if timezone parsing fails
                        dateTime = DateTime.Now;
                    }
                }
                
                // Handle format parameter
                try
                {
                    return dateTime.ToString(format);
                }
                catch
                {
                    // Fallback to default format if custom format is invalid
                    return dateTime.ToString("yyyy-MM-dd HH:mm:ss zzz");
                }
            },
            "get_current_time",
            "Gets the current date and time. Parameters: timezone (optional: 'utc', 'local', or timezone ID), format (optional: .NET datetime format string)");
        }
        
        private static T GetParameter<T>(Dictionary<string, object?> parameters, string key, T defaultValue)
        {
            if (parameters?.TryGetValue(key, out var value) == true)
            {
                try
                {
                    if (value is T directValue)
                        return directValue;
                    
                    return (T?)Convert.ChangeType(value, typeof(T)) ?? defaultValue;
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
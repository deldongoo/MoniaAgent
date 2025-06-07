using MoniaAgent;
using System;
using System.Collections.Generic;

namespace MoniaAgentTest
{
    public class CurrentTimeTool : Tool
    {
        public override string Name => "get_current_time";
        public override string Description => "Gets the current date and time. Parameters: timezone (optional: 'utc', 'local', or timezone ID), format (optional: .NET datetime format string)";
        
        public override object Execute(Dictionary<string, object> parameters)
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
        }
    }
}
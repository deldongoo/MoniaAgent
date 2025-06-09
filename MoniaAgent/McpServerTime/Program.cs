using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();

[McpServerToolType]
public static class FileSystem
{
    [McpServerTool, Description("Retourne l'heure actuelle dans le fuseau horaire du système")]
    public static string RetourneHeureActuelle([Description("Zone temporelle optionnelle (ex: 'Europe/Paris', 'UTC')")] string? zoneTempo = null)
    {
        var maintenant = DateTime.Now;
        
        if (!string.IsNullOrEmpty(zoneTempo))
        {
            try
            {
                var infoZoneTempo = TimeZoneInfo.FindSystemTimeZoneById(zoneTempo);
                var heureZone = TimeZoneInfo.ConvertTime(maintenant, infoZoneTempo);
                return $"Il est actuellement {heureZone:HH:mm:ss} le {heureZone:dd/MM/yyyy} (fuseau {zoneTempo})";
            }
            catch (TimeZoneNotFoundException)
            {
                return $"Fuseau horaire '{zoneTempo}' introuvable. Heure locale : {maintenant:HH:mm:ss} le {maintenant:dd/MM/yyyy}";
            }
        }
        
        return $"Il est actuellement {maintenant:HH:mm:ss} le {maintenant:dd/MM/yyyy}";
    }
}
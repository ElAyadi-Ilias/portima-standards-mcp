using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PortimaStandardsMcp;

var builder = Host.CreateApplicationBuilder(args);

// Charger la configuration depuis appsettings.json
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(PortimaDevOpsTools).Assembly);

builder.Services.AddSingleton<PortimaDevOpsService>();

await builder.Build().RunAsync();

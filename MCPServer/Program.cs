using MCPServer.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Net.Http.Headers;

var builder = Host.CreateApplicationBuilder(args);

// Build configuration and read from appsettings.json
var config = BuildConfiguration();
string serverName = config["MCP:ServerName"];  // Read MCP server name from config

// Register services
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<APITools>();

// Add OpenTelemetry with logging, tracing, and metrics
ResourceBuilder resource = ResourceBuilder.CreateDefault().AddService(serverName);
builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.AddSource("*")
                       .AddHttpClientInstrumentation()
                       .SetResourceBuilder(resource))
    .WithMetrics(b => b.AddMeter("*")
                       .AddHttpClientInstrumentation()
                       .SetResourceBuilder(resource))
    .WithLogging(b => b.SetResourceBuilder(resource))
    .UseOtlpExporter();

// Add logging to console with trace-level logging
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Build and run the application
await builder.Build().RunAsync();

// Method to build configuration from appsettings.json
static IConfiguration BuildConfiguration()
{
    return new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory()) // Ensure base path is set
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();
}

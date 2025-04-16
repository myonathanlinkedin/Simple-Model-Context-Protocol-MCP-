using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using OpenAI;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.ClientModel;

class Program
{
    static async Task Main(string[] args)
    {
        // Build configuration from appsettings.json
        var config = BuildConfiguration();
        string serverName = config["MCP:ServerName"];  // Read server name from config
        string apiEndpoint = config["API:Endpoint"];  // Read API endpoint from config
        string apiKey = config["API:ApiKey"];  // Read API key from config

        using var tracerProvider = SetupTracing();
        using var metricsProvider = SetupMetrics();
        using var loggerFactory = SetupLogging();

        // Connect to an MCP server
        Console.WriteLine($"Connecting client to MCP '{serverName}' server");

        var openAIClient = CreateOpenAIClient(apiEndpoint, apiKey);

        // Create a sampling client
        using var samplingClient = CreateSamplingClient(openAIClient, loggerFactory);

        // Get path to the MCPServer project
        string serverCsprojPath = GetSiblingProjectFilePath("MCPServer", "MCPServer.csproj");

        // Start the MCP client with sampling support
        var mcpClient = await CreateMcpClientAsync(serverCsprojPath, samplingClient, serverName, loggerFactory);

        // Get all available tools
        Console.WriteLine("\nTools available:");
        var tools = await mcpClient.ListToolsAsync();
        foreach (var tool in tools)
            Console.WriteLine($"  {tool}");

        Console.WriteLine();

        // Create a chat client with function invocation
        using var chatClient = CreateChatClient(openAIClient, loggerFactory);

        // Start interactive chat loop
        await RunChatLoopAsync(chatClient, tools);
    }

    static TracerProvider SetupTracing() =>
        Sdk.CreateTracerProviderBuilder()
           .AddHttpClientInstrumentation()
           .AddSource("*")
           .AddOtlpExporter()
           .Build();

    static MeterProvider SetupMetrics() =>
        Sdk.CreateMeterProviderBuilder()
           .AddHttpClientInstrumentation()
           .AddMeter("*")
           .AddOtlpExporter()
           .Build();

    static ILoggerFactory SetupLogging() =>
        LoggerFactory.Create(builder =>
            builder.AddOpenTelemetry(opt => opt.AddOtlpExporter()));

    static OpenAIClient CreateOpenAIClient(string apiEndpoint, string apiKey) =>
        new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(apiEndpoint),  // Use the endpoint from config
            });

    static IChatClient CreateSamplingClient(OpenAIClient client, ILoggerFactory loggerFactory) =>
        client.GetChatClient("gpt-4o-mini")
              .AsIChatClient()
              .AsBuilder()
              .UseOpenTelemetry(loggerFactory: loggerFactory, configure: o => o.EnableSensitiveData = true)
              .Build();

    static async Task<IMcpClient> CreateMcpClientAsync(string csprojPath, IChatClient samplingClient, string serverName, ILoggerFactory loggerFactory) =>
        await McpClientFactory.CreateAsync(
            new StdioClientTransport(new()
            {
                Command = "dotnet",
                Arguments = ["run", "--project", csprojPath, "--build"],
                Name = serverName, // Use server name from config
            }),
            clientOptions: new()
            {
                Capabilities = new()
                {
                    Sampling = new() { SamplingHandler = samplingClient.CreateSamplingHandler() }
                }
            },
            loggerFactory: loggerFactory);

    static IChatClient CreateChatClient(OpenAIClient client, ILoggerFactory loggerFactory) =>
        client.GetChatClient("gpt-4o-mini")
              .AsIChatClient()
              .AsBuilder()
              .UseFunctionInvocation()
              .UseOpenTelemetry(loggerFactory: loggerFactory, configure: o => o.EnableSensitiveData = false)
              .Build();

    static async Task RunChatLoopAsync(IChatClient chatClient, IEnumerable<McpClientTool> tools)
    {
        List<ChatMessage> messages = [];

        while (true)
        {
            Console.Write("Q: ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;

            messages.Add(new(ChatRole.User, input));
            List<ChatResponseUpdate> updates = [];

            var results = chatClient.GetStreamingResponseAsync(messages, new() { Tools = [.. tools] });

            await foreach (var update in results)
            {
                Console.Write(update);
                updates.Add(update);
            }

            Console.WriteLine("\n");
            messages.AddMessages(updates);
        }
    }

    static string GetSiblingProjectFilePath(string siblingFolderName, string projectFileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        // Go up until we find the root containing the sibling folder
        while (dir != null)
        {
            var siblingDir = Path.Combine(dir.FullName, siblingFolderName);
            var projectPath = Path.Combine(siblingDir, projectFileName);

            if (File.Exists(projectPath))
                return projectPath;

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not find '{projectFileName}' inside sibling folder '{siblingFolderName}'.");
    }

    static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // Ensure base path is set
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }
}

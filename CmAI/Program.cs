using CmAI.GenAiProcessor;
using CmAI.CmAIExecutor;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using CommandLine;
using CmAI.CliOptions;
using CmAI.CommandExecutor;

string assemblyDirectory = AppContext.BaseDirectory;
// 1. Set up Configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(assemblyDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

string logFilePath = Path.Combine(assemblyDirectory, "logs", "cm-log.txt");

// 2. Configure Serilog from the Configuration file
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day) // Overrides the path from appsettings.json
    .CreateLogger();

using var cts = new CancellationTokenSource();
CancellationToken cancellationToken = cts.Token;

// Optional: Hook into Ctrl+C event for graceful shutdown
Console.CancelKeyPress += (sender, eventArgs) =>
{
    Log.Warning("Cancellation signal (Ctrl+C) received. Shutting down gracefully...");
    eventArgs.Cancel = true; // Prevents the application from exiting immediately
    cts.Cancel(); // Signal all consuming tasks to cancel
};

try
{
    var host = CreateHostBuilder(args).Build();
    var logger = host.Services.GetService<ILogger<Program>>();

    if (args.Length > 2)
    {
        logger.LogError("Invalid command line arguments");
        logger.LogInformation("Please pass the query as a string, enclosed by \"\"");
        await Task.CompletedTask;
        Log.CloseAndFlush();
        Environment.Exit(1);
    }

    var parser = new Parser(with =>
    {
        with.CaseInsensitiveEnumValues = true;
        with.HelpWriter = Console.Out;
    });
    var parserResult = parser.ParseArguments<CliOptions>(args);
    await parserResult.WithParsedAsync(async options =>
    {
        await RunOperationAsync(host.Services, options, cancellationToken);
    });

    //handle parsing error
    await parserResult.WithNotParsedAsync(async errors =>
    {
        if (!errors.IsHelp() && !errors.IsVersion())
        {
            logger.LogError(
                "Parsing Error: Did you forget to wrap your query in quotes? Example: -q \"Your Query Here\"");
            logger.LogDebug("Detailed parsing failures: {Erroros}", errors.Select(e => e.ToString()));
            Environment.ExitCode = 1;
        }

        await Task.CompletedTask;
    });
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    // Ensure all buffered logs are written to the file before exiting
    Log.CloseAndFlush();
}

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureLogging(logging => { logging.ClearProviders(); })
        .UseSerilog() // Use Serilog instead of the default logger
        .ConfigureServices((hostContext, services) =>
        {
            // Register your application's services here
            services.Configure<GeminiAISettings>(hostContext.Configuration.GetSection("GeminiAI"));
            services.AddScoped<CmAiExecutor>();
            services.AddScoped<AiService>();
            services.AddScoped<CommandExecutorService>();
        });

async Task RunOperationAsync(IServiceProvider serviceProvider, CliOptions options, CancellationToken cancellationToken)
{
    var logger = serviceProvider.GetService<ILogger<Program>>();
    var cmAiExecutor = serviceProvider.GetService<CmAiExecutor>();
    logger.LogDebug("[RunOperationAsync]");
    await cmAiExecutor.ExecuteAsync(options, cancellationToken);
    logger.LogInformation("Completed operation.");
    Environment.ExitCode = 0;
}
using CmAI.AiService;
using CmAI.CmAIExecutor;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

// 1. Set up Configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

// 2. Configure Serilog from the Configuration file
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console() // Still useful for real-time viewing
    .CreateLogger();


try
{
    // 3. Create the Generic Host
    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog() // Use Serilog instead of the default logger
        .ConfigureServices((hostContext, services) =>
        {
            // Register your application's services here
            services.AddHostedService<CmAiExecutor>();
            services.AddScoped<GenAiProcessor>();
            services.Configure<GeminiAISettings>(hostContext.Configuration.GetSection("GeminiAI"));
        })
        .Build();

    // 4. Start the application
    Log.Information("Application starting up...");
    await host.RunAsync();
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
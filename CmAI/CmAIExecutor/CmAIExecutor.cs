using Google.Apis.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CmAI.AiService;

namespace CmAI.CmAIExecutor;

public class CmAiExecutor : BackgroundService
{
    private readonly ILogger<CmAiExecutor> _logger;
    private readonly GenAiProcessor _aiService;

    private readonly List<string> _exitCommands =
    [
        "quit",
        "exit",
        "close",
        "terminate",
        "stop",
        "end",
        "log off",
        "log out",
        "shut down",
        "abort",
        "escape",
        "leave",
        "withdraw"
    ];

    public CmAiExecutor(ILogger<CmAiExecutor> logger, GenAiProcessor aiService)
    {
        _logger = logger;
        _aiService = aiService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CmAI executor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine($"Insert task: ");
                string? userInput = Console.ReadLine()?.Trim();

                if (_exitCommands.Contains(userInput.ToLower()))
                {
                    _logger.LogInformation("Shutdown requested by user.");
                    break;
                }

                if (string.IsNullOrEmpty(userInput))
                {
                    Console.WriteLine("Empty user input");
                    continue;
                }

                _logger.LogInformation($"Executing command: {userInput}");

                _aiService.GetCommand(userInput, GetCurrentOS());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                _logger.LogError(e.Message, e);
            }
        }

        await StopHostGracefully(stoppingToken);
    }


    /// <summary>
    /// Helper method to signal the host to stop
    /// </summary>
    /// <param name="stoppingToken"></param>
    private async Task StopHostGracefully(CancellationToken stoppingToken)
    {
        if (stoppingToken.CanBeCanceled)
        {
            // Inject IHostApplicationLifetime in the constructor if you want to use this pattern
            // For a simple console app, just exiting the loop often suffices, 
            // but this is the formal way to stop the host.
        }

        _logger.LogInformation("CmAI executor stopped");
        await Task.CompletedTask;
    }

    private string GetCurrentOS() =>
        System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows)
            ? "Windows"
            : "Linux/macOS";
}
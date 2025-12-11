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
        _logger.LogDebug("Entering ExecuteAsync");
        _logger.LogInformation("CmAI executor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine($"Insert task: ");
                string? userInput = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(userInput))
                {
                    _logger.LogInformation("Empty user input");
                    continue;
                }

                if (_exitCommands.Contains(userInput.ToLower()))
                {
                    _logger.LogInformation("Shutdown requested by user.");
                    Console.WriteLine("Shutdown requested by user.");
                    break;
                }


                _logger.LogInformation("Processing user request: {userInput}", userInput);

                var commandOutput = await _aiService.GetCommand(userInput, GetCurrentOs());

                _logger.LogInformation("AI response: \n Com: {Command} \n Con: {conclusion} \n isSen: {isSensitive}",
                    commandOutput.command,
                    commandOutput.conclusion, commandOutput.isSensitive);

                // Console.WriteLine($"Command: {commandOutput.command}");
                // Console.WriteLine($"Explanation: {commandOutput.conclusion}");

                if (GetConfirmation(commandOutput.isSensitive))
                {
                    _logger.LogInformation("This command is now executing.");
                }
                else
                {
                    _logger.LogInformation("Shutdown requested by user.");
                    break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error occured: {e}", e);
                throw;
            }
        }

        await StopHostGracefully(stoppingToken);
    }

    /// <summary>
    /// Get confirmation from user.
    /// </summary>
    /// <param name="isSensitive"></param>
    /// <returns></returns>
    private bool GetConfirmation(bool isSensitive)
    {
        _logger.LogDebug("Entering GetConfirmation");
        if (isSensitive)
            Console.Write($"It is a sensitive command, please make sure that you know what you are doing.");


        _logger.LogInformation("Do you want to proceed? Press: y/n");

        string? userInput = Console.ReadLine()?.Trim();

        _logger.LogInformation("User response: {userInput}", userInput);

        return userInput == "y" || userInput == "Y";
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

    private string GetCurrentOs() =>
        System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows)
            ? "Windows"
            : "Linux/macOS";
}
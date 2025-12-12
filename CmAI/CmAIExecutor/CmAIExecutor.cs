using Microsoft.Extensions.Logging;
using CmAI.GenAiProcessor;
using CmAI.CommandExecutor;


namespace CmAI.CmAIExecutor;

public class CmAiExecutor : ICliOperation
{
    private readonly ILogger<CmAiExecutor> _logger;
    private readonly AiService _aiService;
    private readonly CommandExecutorService _commandExecutorService;

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

    public CmAiExecutor(ILogger<CmAiExecutor> logger, AiService aiService,
        CommandExecutorService commandExecutorService)
    {
        _logger = logger;
        _aiService = aiService;
        _commandExecutorService = commandExecutorService;
    }

    public async Task ExecuteAsync(CliOptions.CliOptions options, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Entering ExecuteAsync");
        _logger.LogInformation("CmAI executor started");

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var osType = GetCurrentOs();
            string userInput = options.query;

            if (string.IsNullOrEmpty(userInput))
            {
                _logger.LogInformation("Empty user input");
                return;
            }

            if (_exitCommands.Contains(userInput.ToLower()))
            {
                _logger.LogInformation("Shutdown requested by user.");
                Console.WriteLine("Shutdown requested by user.");
            }


            _logger.LogInformation("Processing user request: {userInput}", userInput);

            var commandOutput = await _aiService.GetCommand(userInput, osType, cancellationToken);

            _logger.LogInformation("AI response: \n Com: {Command} \n Con: {conclusion}",
                commandOutput.command,
                commandOutput.conclusion);

            _logger.LogDebug("isSen: {isSensitive}", commandOutput.isSensitive);

            if (GetConfirmation(commandOutput.isSensitive))
            {
                _logger.LogInformation("This command is now executing.");
                ////
                var isDone = await _commandExecutorService.ExecuteCommand(commandOutput.command, osType, commandOutput.isSensitive, cancellationToken);
                ////
            }
            else
            {
                _logger.LogInformation("Shutdown requested by user.");
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error occured: {e}", e);
            throw;
        }


        await StopHostGracefully(cancellationToken);
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

        _logger.LogDebug("User response: {userInput}", userInput);

        return userInput == "y" || userInput == "Y";
    }

    /// <summary>
    /// Helper method to signal the host to stop
    /// </summary>
    /// <param name="cancellationToken"></param>
    private async Task StopHostGracefully(CancellationToken cancellationToken)
    {
        if (cancellationToken.CanBeCanceled)
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
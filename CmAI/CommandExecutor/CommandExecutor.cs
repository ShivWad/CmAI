using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CmAI.CommandExecutor;

public class CommandExecutorService
{
    private readonly ILogger _logger;

    public CommandExecutorService(ILogger<CommandExecutorService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ExecuteCommand(string commandFromAi, string osType, bool isSensitive,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await RunShellCommand(commandFromAi, osType, isSensitive);
    }

    private async Task<bool> RunShellCommand(string command, string osType, bool isSensitive)
    {
        string shell;
        string arguments;

        // Determine the host shell based on the OS the model targeted
        if (osType.Equals("Windows", StringComparison.OrdinalIgnoreCase))
        {
            // For Windows, use cmd.exe and the /C flag to execute and close
            shell = "cmd.exe";
            // The command itself must be wrapped in quotes for cmd.exe
            arguments = $"/C \"{command}\"";
        }
        else // Assumes Linux/macOS (Bash) for any other input
        {
            // For Linux/macOS, use bash and the -c flag
            shell = "/bin/bash";
            // The command itself must be wrapped in quotes for bash
            arguments = $"-c \"{command}\"";
        }

        _logger.LogDebug("Starting process: {Shell} {Args}", shell, arguments);

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = shell,
                    Arguments = arguments,
                    RedirectStandardOutput = true, // Capture output for logging
                    RedirectStandardError = true, // Capture errors
                    UseShellExecute = false, // Required for redirection
                    CreateNoWindow = true, // Don't pop up a console window
                }
            };

            //Give a last warning to the user
            if (isSensitive)
            {
                _logger.LogInformation("Please confirm again, press: y/n");

                var userInput = Console.ReadLine()?.Trim();

                _logger.LogDebug("User response: {userInput}", userInput);

                if (userInput != "y" && userInput != "Y")
                {
                    _logger.LogInformation("Sensitive command execution aborted by user.");
                    return false;
                }
            }

            process.Start();

            // Capture output and errors asynchronously
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            // Wait for the process to exit
            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Command executed successfully. Output:\n{Output}", output.Trim());
                return true;
            }

            _logger.LogError("Command failed with exit code {Code}. Error:\n{Error}", process.ExitCode,
                error.Trim());

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start shell process. Check if the shell executable is accessible.");
            return false;
        }
    }
}
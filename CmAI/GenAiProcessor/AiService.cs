using Microsoft.Extensions.Logging;
using Google.GenAI;
using Google.GenAI.Types;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Google;
using Google.Apis.Util;
using Type = Google.GenAI.Types.Type; // Used to deserialize the final JSON string


namespace CmAI.GenAiProcessor;

public class AiService
{
    private readonly GeminiAISettings _settings;
    private readonly ILogger<AiService> _logger;

    public AiService(ILogger<AiService> logger, IOptions<GeminiAISettings> settings)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// calls GenAI api and gets the response
    /// </summary>
    /// <param name="userQuery"></param>
    /// <param name="osType"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<CommandOutput> GetCommand(string userQuery, string osType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await GetStructuredCommand(userQuery, osType, cancellationToken);
    }


    /// <summary>
    /// Core method which calls GenAI api and gets the response
    /// </summary>
    /// <param name="userInput"></param>
    /// <param name="osType"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<CommandOutput> GetStructuredCommand(string userInput, string osType,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Entering GetStructuredCommand ");

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var client = new Client(apiKey: _settings.ApiKey);

            var systemPrompt = $"""

                                You are an expert command-line executor for a {osType} system.
                                Your only function is to analyze the user's intent and populate the REQUIRED JSON schema.

                                RULES:
                                1. 'command': Must be a single, executable {osType} terminnal command (CMD/PowerShell/bash). Do not include quotes or surrounding text.
                                2. 'isSensitive': MUST be true if the command involves deletion, modification of system files, or irreversible actions.
                                3. 'conclusion': Must be a concise, one-sentence summary of the action and include a clear warning if 'isSensitive' is true.

                                """;

            var config = new GenerateContentConfig
            {
                SystemInstruction = new Content
                {
                    Parts = new List<Part>
                    {
                        new() { Text = systemPrompt }
                    }
                },
                CandidateCount = 1,
                Temperature = 0.1f,
                ResponseMimeType = "application/json",
                ResponseSchema = new Schema
                {
                    Type = Type.OBJECT,
                    Properties = new Dictionary<string, Schema>
                    {
                        { "command", new Schema { Type = Type.STRING } },
                        { "isSensitive", new Schema { Type = Type.BOOLEAN } },
                        { "conclusion", new Schema { Type = Type.STRING } }
                    },
                }
            };

            var apiTask = client.Models.GenerateContentAsync(
                model: "gemini-2.5-flash",
                contents:
                [
                    new Content
                    {
                        Parts = new List<Part>
                        {
                            new() { Text = userInput }
                        }
                    }
                ],
                config: config
            );


            // This task will complete ONLY when the linked cancellationToken is signaled.
            var cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);

            var completedTask = await Task.WhenAny(apiTask, cancellationTask);

            // --- STEP 4: Check which task won the race ---
            if (completedTask == cancellationTask)
            {
                // The user pressed Ctrl+C.
                _logger.LogWarning("AI generation cancelled by CancellationToken signal (Task.WhenAny timeout).");

                // Abort the entire operation by throwing the required exception.
                throw new OperationCanceledException(cancellationToken);
            }

            var response = await apiTask;
            response.ThrowIfNull("Null response received from AI");

            // Deserialize the JSON string into C# object
            var output = JsonSerializer.Deserialize<CommandOutput>(response.Candidates[0].Content.Parts[0].Text);

            return output;
        }
        catch (OperationCanceledException e)
        {
            _logger.LogWarning("Operation cancelled by cancellation signal.");
            _logger.LogDebug(e, "AI generation cancelled by CancellationToken signal.");
            throw;
        }
        catch (GoogleApiException e)
        {
            _logger.LogError("Error [GetStructuredCommand] - GoogleApiException: {e}", e);
            throw;
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("Error-: {e}", e.Message);
            _logger.LogDebug("Error [GetStructuredCommand] - HttpRequestException: {e}", e);
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError("Error [GetStructuredCommand] - Exception: {e}", e);
            throw;
        }
    }
}
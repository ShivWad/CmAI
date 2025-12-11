using Microsoft.Extensions.Logging;
using Google.GenAI;
using Google.GenAI.Types;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Google;
using Google.Apis.Util;
using Google.Apis.Responses; // Need this for ApiException
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http; // For Struct
using Type = Google.GenAI.Types.Type; // Used to deserialize the final JSON string


namespace CmAI.AiService;



public class GenAiProcessor
{
    private readonly GeminiAISettings _settings;
    private readonly ILogger<GenAiProcessor> _logger;

    public GenAiProcessor(ILogger<GenAiProcessor> logger, IOptions<GeminiAISettings> settings)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<CommandOutput> GetCommand(string userQuery, string osType)
    {
        return await GetStructuredCommand(userQuery, osType);
    }


    private async Task<CommandOutput> GetStructuredCommand(string userInput, string osType)
    {
        _logger.LogDebug("Entering GetStructuredCommand ");
        try
        {
            var client = new Client(apiKey: _settings.ApiKey);

            string systemPrompt = $@"
You are an expert command-line executor for a {osType} system.
Your only function is to analyze the user's intent and populate the REQUIRED JSON schema.

RULES:
1. 'command': Must be a single, executable {osType} terminnal command (CMD/PowerShell/bash). Do not include quotes or surrounding text.
2. 'isSensitive': MUST be true if the command involves deletion, modification of system files, or irreversible actions.
3. 'conclusion': Must be a concise, one-sentence summary of the action and include a clear warning if 'isSensitive' is true.
";

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


            var response = await client.Models.GenerateContentAsync(
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

            response.ThrowIfNull("Null response received from AI");

            // Deserialize the guaranteed valid JSON string into your C# object
            var output = JsonSerializer.Deserialize<CommandOutput>(response.Candidates[0].Content.Parts[0].Text);

            return output;
        }
        catch (GoogleApiException e)
        {
            _logger.LogError("Error [GetStructuredCommand] - GoogleApiException: {e}", e);
            throw;
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("Error [GetStructuredCommand] - HttpRequestException: {e}", e);
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError("Error [GetStructuredCommand] - Exception: {e}", e);
            throw;
        }
    }
}
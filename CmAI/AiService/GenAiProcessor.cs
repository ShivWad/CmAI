using Microsoft.Extensions.Logging;
using Google.GenAI;
using Google.GenAI.Types;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Google.Protobuf.WellKnownTypes; // For Struct
using Type = Google.GenAI.Types.Type; // Used to deserialize the final JSON string


namespace CmAI.AiService;

public class GeminiAISettings
{
    // This property name must match the key in appsettings.json
    public string ApiKey { get; set; }
}

public class CommandOutput
{
    public string command { get; set; }

    public bool isSensitive { get; set; }

    public string conclusion { get; set; }
}

public class GenAiProcessor
{
    private readonly GeminiAISettings _settings;
    private readonly ILogger<GenAiProcessor> _logger;

    public GenAiProcessor(ILogger<GenAiProcessor> logger, IOptions<GeminiAISettings> settings)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> GetCommand(string userQuery, string osType)
    {
        try
        {
            var result = await GetStructuredCommand(userQuery, osType);
            Console.WriteLine(result.command);
            Console.WriteLine(result.isSensitive);
            Console.WriteLine(result.conclusion);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return "";
    }


    public async Task<CommandOutput> GetStructuredCommand(string userInput, string osType)
    {
        // Use the strict system prompt defined above
        // The System Prompt you pass to GenerateContentConfig

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

        // Deserialize the guaranteed valid JSON string into your C# object
        var output = JsonSerializer.Deserialize<CommandOutput>(response.Candidates[0].Content.Parts[0].Text);
        return output;
    }
}
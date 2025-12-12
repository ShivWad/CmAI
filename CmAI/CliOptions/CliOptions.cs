using CommandLine;
using CommandLine.Text;

namespace CmAI.CliOptions;

public class CliOptions
{
    [Option('q', "query", Required = true, HelpText = "The query to execute")]
    public string query { get; set; }
}
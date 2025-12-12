namespace CmAI;

public interface ICliOperation
{
    
    Task ExecuteAsync(CliOptions.CliOptions options, CancellationToken cancellationToken);
}
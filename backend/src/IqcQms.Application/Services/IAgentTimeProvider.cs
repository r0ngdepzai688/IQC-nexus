namespace IqcQms.Application.Services;

public interface IAgentTimeProvider
{
    DateTime UtcNow { get; }
}

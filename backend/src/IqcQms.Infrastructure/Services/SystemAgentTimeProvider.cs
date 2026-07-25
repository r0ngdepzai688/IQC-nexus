using IqcQms.Application.Services;

namespace IqcQms.Infrastructure.Services;

public class SystemAgentTimeProvider : IAgentTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

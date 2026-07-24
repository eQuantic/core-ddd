namespace eQuantic.Core.Application.Services;

public interface IDateTimeProviderService : IApplicationService
{
    DateTimeOffset GetLocalNow();
    DateTimeOffset GetUtcNow();
    long GetTimestamp();
}